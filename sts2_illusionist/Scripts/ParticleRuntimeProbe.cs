using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts;

/// <summary>Opt-in capture of the installed native renderer, never a browser or mocked material.</summary>
public static class ParticleRuntimeProbe
{
    public static void TryRun()
    {
        var args=OS.GetCmdlineUserArgs();int k=Array.IndexOf(args,"--illusionist-particle-probe");
        if(k<0||k+1>=args.Length)return;
        var tree=(SceneTree)Engine.GetMainLoop();Callable.From(()=>{_=Run(tree,args[k+1]);}).CallDeferred();
    }
    private static async Task Run(SceneTree tree,string destination)
    {
        try
        {
            Directory.CreateDirectory(destination);
            var view=new SubViewport{Size=new Vector2I(1200,720),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};tree.Root.AddChild(view);
            view.AddChild(new ColorRect{Size=new Vector2(1200,720),Color=new Color(.145f,.14f,.17f)});
            var visuals=ModelDb.Character<Characters.Illusionist>().CreateVisuals();view.AddChild(visuals);
            var body=visuals.GetNode<Node2D>("%Visuals");body.Position=new Vector2(305,637.6f);body.Scale=Vector2.One*(365f/1085);
            var fx=body.GetNode<IllusionistOrbitFx>("IllusionistOrbitFx");fx.SetProcess(false);
            var clock=typeof(IllusionistOrbitFx).GetField("_clock",BindingFlags.NonPublic|BindingFlags.Instance)!;
            var anim=body.Call("get_animation_state").AsGodotObject();
            var track=anim.Call("get_current",0).AsGodotObject();track.Call("set_time_scale",0);
            async Task<Image> Capture(float time,string? name=null)
            {
                clock.SetValue(fx,time);track.Call("set_track_time",Mathf.PosMod(time,8));fx._Process(0);
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);RenderingServer.ForceDraw(false);
                var image=view.GetTexture().GetImage();if(name!=null)image.SavePng(Path.Combine(destination,name+".png"));return image;
            }
            var checks=new Godot.Collections.Dictionary();
            var first=await Capture(0,"idle-first");var last=await Capture(16,"idle-last");
            checks["renderer"]=fx.DebugState()["renderer"];
            checks["exact_idle_loop"]=first.GetData().SequenceEqual(last.GetData());
            await Capture(2,"idle-native");checks["particle_count"]=fx.DebugState()["persistent_particles"];
            await Capture(3);fx.ShowGuard();
            var counts=new Godot.Collections.Array<int>();bool causal=true;
            foreach(float age in new[]{.04f,.12f,.20f,.28f,.42f})
            {
                await Capture(3+age,$"guard-{age:0.00}");var state=fx.DebugState();counts.Add(state["guard_history_copies"].AsInt32());
                causal&=state["oldest_guard_time"].AsSingle()>=state["guard_started"].AsSingle();
            }
            checks["guard_counts"]=counts;checks["guard_is_causal"]=causal;
            await Capture(6);fx.PowerCast();await Capture(6.6f,"power-native");checks["power_peak_speed"]=fx.DebugState()["orbit_speed"];
            await Capture(13,"power-recovered");checks["power_recovered_speed"]=fx.DebugState()["orbit_speed"];
            await Capture(14);fx.Hurt();await Capture(14.08f,"hurt-native");
            await Capture(16);fx.BeginDirectedAttack(new[]{new Vector2(905,480)});
            var frames=Path.Combine(destination,"attack-frames");Directory.CreateDirectory(frames);
            for(int f=0;f<=50;f++)
            {
                float t=16+f/30f;
                if(f==11){clock.SetValue(fx,t);fx.StrikeAt(new Vector2(905,480));}
                await Capture(t,"attack-frames/frame-"+f.ToString("D3"));
            }
            checks["attack_stagger"]=fx.DebugState()["attack_stagger"];
            checks["three_contacts"]=fx.DebugState()["strikes"].AsInt32()>0;
            // Same actual factory/renderer, three whole actors in the scene's normal sibling order.
            body.Position=new Vector2(560,638);
            for(int i=0;i<2;i++)
            {
                var clone=ModelDb.Character<Characters.Illusionist>().CreateVisuals();view.AddChild(clone);view.MoveChild(clone,1);
                var cb=clone.GetNode<Node2D>("%Visuals");cb.Position=new Vector2(475+i*40,580+i*25);cb.Scale=body.Scale;cb.Modulate=new Color(1,1,1,.7f+i*.15f);
                var cf=cb.GetNode<IllusionistOrbitFx>("IllusionistOrbitFx");cf.SetProcess(false);clock.SetValue(cf,2f);cf._Process(0);
            }
            await Capture(19,"multiple-actors-native");
            bool passed=checks["renderer"].AsString()=="html-particle-density-v1"&&causal
                &&checks["exact_idle_loop"].AsBool()
                &&counts.SequenceEqual(new[]{0,1,2,3,4})&&checks["particle_count"].AsInt32()>90
                &&checks["power_peak_speed"].AsSingle()>2&&Mathf.Abs(checks["power_recovered_speed"].AsSingle()-1)<.01f;
            checks["passed"]=passed;
            File.WriteAllText(Path.Combine(destination,"installed-particle-check.json"),Json.Stringify(checks,"  "));
            GD.Print("ILLUSIONIST PARTICLE PROBE: "+Json.Stringify(checks));
            view.Free();tree.Quit(passed?0:1);
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(destination,"particle-probe-error.txt"),ex.ToString());GD.PrintErr(ex);tree.Quit(1);}
    }
}
