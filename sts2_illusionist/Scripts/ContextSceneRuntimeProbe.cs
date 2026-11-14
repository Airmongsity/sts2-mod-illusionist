using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;

namespace Illusionist.Scripts;

/// <summary>Explicit opt-in probe of installed campsite resources and the real event animation hook.</summary>
public static class ContextSceneRuntimeProbe
{
    public static void TryRun()
    {
        string[] args=OS.GetCmdlineUserArgs();
        int index=Array.IndexOf(args,"--illusionist-context-probe");
        if(index<0||index+1>=args.Length)return;
        var tree=(SceneTree)Engine.GetMainLoop();
        Callable.From(()=>{_=Run(tree,args[index+1]);}).CallDeferred();
    }

    private static async Task Run(SceneTree tree,string destination)
    {
        try
        {
            Directory.CreateDirectory(destination);
            var checks=new Godot.Collections.Dictionary();
            var view=new SubViewport{Size=new Vector2I(1920,1080),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};
            tree.Root.AddChild(view);
            var background=new Sprite2D{Centered=false};view.AddChild(background);
            var root=new Node2D{Position=new Vector2(623.775f,662.775f),Scale=Vector2.One*.380003f};
            view.AddChild(root);
            var pose=IllusionistRestPose.Create(new Vector2(6.5789f,-10.5262f),Mathf.Abs(root.GlobalScale.Y))
                ??throw new Exception("Installed rest texture or shader could not load");
            root.AddChild(pose);
            using(var image=pose.Texture.GetImage())
            {
                checks["alpha_corner"]=image.GetPixel(0,0).A;
                if(image.GetPixel(0,0).A>.001f)throw new Exception("Rest sprite background is opaque");
            }
            checks["rest_display_height"]=pose.Texture.GetHeight()*Mathf.Abs(pose.GlobalScale.Y);
            foreach(string scene in new[]{"overgrowth","hive","glory"})
            {
                background.Texture=ImageTexture.CreateFromImage(Image.LoadFromFile(Path.Combine(destination,scene+"-native.png")));
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                RenderingServer.ForceDraw(false);
                view.GetTexture().GetImage().SavePng(Path.Combine(destination,scene+"-installed.png"));
            }
            Vector2 seat=pose.ToGlobal(IllusionistRestPose.SeatPixel+pose.Offset);
            checks["seat_error"]=seat.DistanceTo(root.ToGlobal(pose.Position));
            root.Scale=new Vector2(-.380003f,.380003f);
            checks["mirrored_height"]=pose.Texture.GetHeight()*Mathf.Abs(pose.GlobalScale.Y);
            root.Free();
            background.Texture=ImageTexture.CreateFromImage(Image.LoadFromFile(Path.Combine(destination,"fake_merchant-native.png")));
            var eventParent=new Node2D{Position=new Vector2(562,816),Scale=Vector2.One*1.75f};view.AddChild(eventParent);
            var visuals=ModelDb.Character<Characters.Illusionist>().CreateVisuals();
            eventParent.AddChild(visuals);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            // Invoke the patched original method, so registration and character scoping are tested too.
            var merchant=new NFakeMerchant();
            typeof(NFakeMerchant).GetMethod("StartCharacterAnimation",BindingFlags.NonPublic|BindingFlags.Instance)!
                .Invoke(merchant,new object[]{visuals});
            var body=visuals.GetNode<Node2D>("%Visuals");
            checks["event_body_scale"]=Mathf.Abs(body.Scale.Y);
            checks["event_front_hidden"]=!body.GetNode<Node2D>("IllusionistOrbitFx").Visible;
            checks["event_back_hidden"]=!body.GetNode<Node2D>("IllusionistOrbitBack").Visible;
            string clip=body.Call("get_animation_state").AsGodotObject().Call("get_current",0).AsGodotObject()
                .Call("get_animation").AsGodotObject().Call("get_name").AsString();
            checks["event_clip"]=clip;
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);RenderingServer.ForceDraw(false);
            view.GetTexture().GetImage().SavePng(Path.Combine(destination,"fake_merchant-installed.png"));
            bool passed=Mathf.Abs(checks["rest_display_height"].AsSingle()-280)<.01f
                &&checks["seat_error"].AsSingle()<.01f&&Mathf.Abs(body.Scale.Y-.25f)<.001f
                &&checks["event_front_hidden"].AsBool()&&checks["event_back_hidden"].AsBool()&&clip=="relaxed_loop";
            checks["passed"]=passed;
            File.WriteAllText(Path.Combine(destination,"installed-context-check.json"),Json.Stringify(checks,"  "));
            merchant.Free();view.QueueFree();
            GD.Print("ILLUSIONIST CONTEXT PROBE: "+Json.Stringify(checks));
            tree.Quit(passed?0:1);
        }
        catch(Exception ex)
        {
            File.WriteAllText(Path.Combine(destination,"context-probe-error.txt"),ex.ToString());
            GD.PrintErr("ILLUSIONIST CONTEXT PROBE: "+ex);tree.Quit(1);
        }
    }
}
