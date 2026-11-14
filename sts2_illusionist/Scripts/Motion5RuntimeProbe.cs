using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts;

/// <summary>Opt-in visual smoke test through the real mod loader. Never starts a run.</summary>
public static class Motion5RuntimeProbe
{
    public static void TryRun()
    {
        string[] args = OS.GetCmdlineUserArgs();
        int index = Array.IndexOf(args, "--illusionist-motion-probe");
        if (index < 0 || index + 1 >= args.Length) return;
        string destination = args[index + 1];
        var tree = (SceneTree)Engine.GetMainLoop();
        Callable.From(() => { _ = Run(tree, destination); }).CallDeferred();
    }

    private static async Task Run(SceneTree tree, string destination)
    {
        try
        {
            Directory.CreateDirectory(destination);
            await CheckFireMaterial(tree,destination);
            await CheckCombatScale(tree, destination);
            await CheckMultiActorDepth(tree, destination);
            var view = new SubViewport { Size = new Vector2I(1000, 800), RenderTargetUpdateMode = SubViewport.UpdateMode.Always, ProcessMode = Node.ProcessModeEnum.Always };
            tree.Root.AddChild(view);
            view.AddChild(new ColorRect { Color = new Color("292832"), Size = new Vector2(1000, 800) });
            var body = SpineBody.CreateSprite(SpineBody.CombatSkel, 1, "Motion5ProbeBody") ?? throw new Exception("Combat skeleton failed to load");
            body.Position = new Vector2(285, 750); body.Scale = Vector2.One * .58f;
            view.AddChild(body);
            SpineBody.Play(body, SpineBody.CombatSkel);
            IllusionistOrbitFx.Attach(body);
            var fx = body.GetNode<IllusionistOrbitFx>("IllusionistOrbitFx");
            GodotObject state = body.Call("get_animation_state").AsGodotObject();
            GD.Print("MOTION5 REAL MOD: controller attached");
            await CheckOcclusion(tree, view, body, fx, destination);
            await CheckDirectedBehavior(tree, fx, destination);
            var target = new Creature((Player)null!, 100, 100);
            // Record at the real combat scale, after the close-up regression checks.
            body.Scale = Vector2.One * .30f;
            body.Position = new Vector2(285, 600);
            view.Size = new Vector2I(1000, 650);
            var animator = ModelDb.Character<Characters.Illusionist>().GenerateAnimator(new MegaSprite(body), target);
            var targetBody = ClassDB.Instantiate("SpineSprite").As<Node2D>();
            targetBody.Call("set_skeleton_data_res", ResourceLoader.Load("res://animations/characters/ironclad/ironclad_skel_data.tres"));
            targetBody.Position = new Vector2(790, 600); targetBody.Scale = new Vector2(.27f, .27f);
            view.AddChild(targetBody);
            targetBody.Call("get_animation_state").AsGodotObject().Call("set_animation", "idle_loop", true, 0);
            var hp = new Label { Text = "HP 100 / 100", Position = new Vector2(730, 240) };
            view.AddChild(hp);
            var phase = new Label { Text = "Combat scale 0.30 / native animator", Position = new Vector2(35, 35) };
            view.AddChild(phase);
            target.CurrentHpChanged += (_, _) => hp.Text = $"HP {target.CurrentHp} / 100";
            string frames = Path.Combine(destination, "teleport-attack-frames");
            Directory.CreateDirectory(frames);
            int corridorPurplePixelsMax = 0;
            int contactVisiblePixels = 0, attackFrames = 0;
            for (int frame = 0; frame < 420; frame++)
            {
                if (frame is 120 or 180) { animator.SetTrigger("Attack"); fx.PrepareAttack(); }
                if (frame is 124 or 184) fx.BeginDirectedAttack(new[] { new Vector2(790, 440) });
                if (frame is 134 or 194)
                {
                    var previous = IllusionistDamageFxScope.Current.Value;
                    IllusionistDamageFxScope.Current.Value = new(_ => fx.StrikeAt(new Vector2(790, 440)));
                    try
                    {
                        target.LoseHpInternal(6m, ValueProp.Move);
                        targetBody.Call("get_animation_state").AsGodotObject().Call("set_animation", "hurt", false, 0);
                    }
                    finally { IllusionistDamageFxScope.Current.Value = previous; }
                }
                if (frame is 165 or 220) targetBody.Call("get_animation_state").AsGodotObject().Call("set_animation", "idle_loop", true, 0);
                if (frame == 255) { animator.SetTrigger("IllusionistDefend"); fx.ShowGuard(); }
                if (frame == 300) fx.PowerCast();
                if (frame == 375) { animator.SetTrigger("Hit"); fx.Hurt(); }
                if (frame == 405) animator.SetTrigger("Dead");
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                if (frame >= 120 && frame < 180)
                {
                    string current = state.Call("get_current", 0).AsGodotObject().Call("get_animation").AsGodotObject().Call("get_name").AsString();
                    if (current == "attack") attackFrames++;
                    float age = (frame - 120) / 30f;
                    phase.Text = $"Sequential attack  {age:0.00} s   |   3 flames / 0.20 s stagger / approach - hit - rebound";
                }
                if (frame >= 180 && frame < 240) phase.Text = "Sequential strike / 0.20 s intervals / rebound";
                if (frame >= 250 && frame < 295) phase.Text = "Block gained / thicker flame trails";
                if (frame >= 300 && frame < 375) phase.Text = $"Power card / orbit speed {fx.DebugState()["orbit_speed"].AsDouble():0.0}x / gradual recovery";
                if (frame >= 375) phase.Text = "Hurt / body and flames recoil";
                if (frame >= 90 && frame <= 400)
                {
                    RenderingServer.ForceDraw(false);
                    Image capture = view.GetTexture().GetImage();
                    capture.SavePng(Path.Combine(frames, $"frame-{frame - 90:D3}.png"));
                    int purplePixels = 0;
                    for (int y = 250; y < 550; y += 3) for (int x = 450; x < 650; x += 3)
                    {
                        Color color = capture.GetPixel(x, y);
                        if (color.B > .30f && color.B > color.R * 1.1f && color.R > color.G * 1.12f) purplePixels++;
                    }
                    corridorPurplePixelsMax = Math.Max(corridorPurplePixelsMax, purplePixels);
                    if (frame == 145)
                    {
                        foreach (Vector2 point in fx.FlameWorldPositions())
                        {
                            int visible = 0;
                            for (int y = (int)point.Y - 10; y <= (int)point.Y + 10; y++)
                                for (int x = (int)point.X - 10; x <= (int)point.X + 10; x++)
                                {
                                    Color color = capture.GetPixel(x, y);
                                    if (color.B > .35f && color.B > color.R * 1.05f && color.R > color.G * 1.1f) visible++;
                                }
                            contactVisiblePixels += visible;
                        }
                    }
                }
                if (new[] { 30, 90, 148, 172, 225, 315 }.Contains(frame))
                {
                    RenderingServer.ForceDraw(false);
                    view.GetTexture().GetImage().SavePng(Path.Combine(destination, $"live-game-{frame}.png"));
                    GD.Print($"MOTION5 REAL MOD {frame}: {Json.Stringify(fx.DebugState())}");
                }
            }
            var result = fx.DebugState();
            result["corridor_purple_pixels"] = corridorPurplePixelsMax;
            result["contact_visible_pixels"] = contactVisiblePixels;
            result["native_animator_attack_seconds"] = attackFrames / 30f;
            if (result["clock"].AsDouble() < 1 || result["draws"].AsInt32() < 10) throw new Exception("FX callbacks did not run");
            if (result["max_flame_trail_source_px"].AsDouble() > 140.01) throw new Exception("Flame trail exceeded spatial length limit");
            if (corridorPurplePixelsMax != 0) throw new Exception("Purple attack pixels appeared in the teleport corridor");
            if (attackFrames < 41 || attackFrames > 46) throw new Exception($"Native animator cut attack short: {attackFrames} frames");
            File.WriteAllText(Path.Combine(destination, "live-game-check.json"), Json.Stringify(result, "  "));
            GD.Print("MOTION5 REAL MOD: PASS");
            tree.Quit();
        }
        catch (Exception ex)
        {
            GD.PushError("MOTION5 REAL MOD: " + ex);
            tree.Quit(1);
        }
    }

    private static async Task CheckDirectedBehavior(SceneTree tree, IllusionistOrbitFx fx, string destination)
    {
        var checks = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        void Check(string name, bool passed) => checks.Add(new() { ["name"] = name, ["passed"] = passed });
        Vector2 aim = new(850, 430);
        fx.BeginDirectedAttack(new[] { aim });
        float approachDistance = 0;
        for (int frame = 0; frame < 44; frame++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            var positions = fx.FlameWorldPositions(); var opacity = fx.FlameOpacities();
            if (frame == 5) {
                approachDistance = positions[0].DistanceTo(aim);
                Check("first approaches from above left while third stays home", positions[0].X < aim.X && positions[0].Y < aim.Y && positions[2].X < 520);
            }
            if (frame == 9) {
                Check("first converges on target", positions[0].DistanceTo(aim) < approachDistance);
                var previous = IllusionistDamageFxScope.Current.Value;
                var dummy = new Creature((Player)null!,100,100);
                IllusionistDamageFxScope.Current.Value = new(_ => fx.StrikeAt(aim));
                dummy.LoseHpInternal(6m, ValueProp.Move);
                Check("native HP and impact share frame", dummy.CurrentHp == 94 && fx.DebugState()["impact_frame"].AsInt64() == (long)Engine.GetProcessFrames());
                IllusionistDamageFxScope.Current.Value = previous;
            }
            if (frame == 15) Check("first rebounds after contact", positions[0].Y < aim.Y && positions[0].X > aim.X);
            if (frame == 20) Check("first vanishes while third attacks", opacity[0] < .01f && positions[2].X > 520 && opacity[2] > .5f);
        }
        Check("all three return to local orbit", fx.FlameWorldPositions().All(p => p.X < 520) && fx.FlameOpacities().All(a => a > .99f));
        fx.PowerCast(); float fast = (float)fx.DebugState()["orbit_speed"].AsDouble();
        for (int f = 0; f < 45; f++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        float slowing = (float)fx.DebugState()["orbit_speed"].AsDouble();
        Check("power accelerates immediately then decays smoothly", fast > 3.7 && slowing > 1 && slowing < fast);
        fx.ShowGuard();
        for (int f = 0; f < 10; f++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Check("guard trail grows to over four times idle width", fx.DebugState()["guard_tail_width"].AsDouble() > 48);
        fx.Hurt();
        Check("procedural flame renderer active", fx.DebugState()["procedural_fire"].AsBool());
        var command = new AttackCommand(6m).WithHitFx("vfx/vfx_attack_slash", "preserved");
        Patches.IllusionistOrbitAttackPatch.Configure(command, fx);
        Check("generic slash replaced without removing sound", command.HitVfx == null && command.HitSfx == "preserved");
        var report = new Godot.Collections.Dictionary { ["passed"] = checks.All(c=>c["passed"].AsBool()), ["checks"] = checks };
        File.WriteAllText(Path.Combine(destination,"directed-check.json"),Json.Stringify(report,"  "));
        GD.Print("MOTION5 DIRECTED: " + Json.Stringify(report));
        if (!report["passed"].AsBool()) throw new Exception("Directed flame choreography failed");
    }

    private static async Task CheckFireMaterial(SceneTree tree,string destination)
    {
        var view=new SubViewport{Size=new Vector2I(400,400),RenderTargetUpdateMode=SubViewport.UpdateMode.Always,ProcessMode=Node.ProcessModeEnum.Always};
        tree.Root.AddChild(view);
        var bg=new ColorRect{Size=new Vector2(400,400),Color=new Color(.05f,.05f,.05f)};view.AddChild(bg);
        var material=new ShaderMaterial{Shader=ResourceLoader.Load<Shader>("res://illusionist/art/motion5/ghost_fire.gdshader")};material.SetShaderParameter("fx_time",1.2f);
        var white=Image.CreateEmpty(1,1,false,Image.Format.Rgba8);white.Fill(Colors.White);
        var flame=new TextureRect{Texture=ImageTexture.CreateFromImage(white),Position=new Vector2(100,80),Size=new Vector2(200,264),Material=material};view.AddChild(flame);
        for(int f=0;f<3;f++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw(false);var dark=view.GetTexture().GetImage();dark.SavePng(Path.Combine(destination,"ghost-fire-dark.png"));
        bg.Color=new Color(.85f,.85f,.85f);
        for(int f=0;f<3;f++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw(false);var light=view.GetTexture().GetImage();light.SavePng(Path.Combine(destination,"ghost-fire-light.png"));
        Color a=dark.GetPixel(200,280),b=light.GetPixel(200,280);
        float transmission=((b.R-a.R)+(b.G-a.G)+(b.B-a.B))/(3*.8f);
        bool passed=transmission>.10f&&transmission<.80f&&a.B>a.G*1.2f;
        var report=new Godot.Collections.Dictionary{["passed"]=passed,["root_transmission"]=transmission};
        File.WriteAllText(Path.Combine(destination,"ghost-fire-material-check.json"),Json.Stringify(report,"  "));view.Free();
        if(!passed)throw new Exception("Ghost fire shader or transparency failed: "+Json.Stringify(report));
    }

    private static async Task CheckMultiActorDepth(SceneTree tree, string destination)
    {
        var view = new SubViewport { Size = new Vector2I(700,650), RenderTargetUpdateMode = SubViewport.UpdateMode.Always, ProcessMode = Node.ProcessModeEnum.Always };
        tree.Root.AddChild(view);
        view.AddChild(new ColorRect { Size = new Vector2(700,650), Color = new Color("292832") });
        var bodies = new System.Collections.Generic.List<Node2D>();
        foreach (var point in new[] { new Vector2(350,600), new Vector2(290,540), new Vector2(410,570) })
        {
            var body = SpineBody.CreateSprite(SpineBody.CombatSkel,1,"DepthActor")!;
            body.Position=point; body.Scale=Vector2.One*.36f; view.AddChild(body);
            SpineBody.Play(body,SpineBody.CombatSkel); IllusionistOrbitFx.Attach(body); bodies.Add(body);
        }
        Patches.IllusionistMirrorRingPatch.SortByGroundDepth(view,bodies);
        bool passed = bodies[1].GetIndex()<bodies[2].GetIndex() && bodies[2].GetIndex()<bodies[0].GetIndex();
        for(int f=0;f<30;f++) await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw(false);view.GetTexture().GetImage().SavePng(Path.Combine(destination,"multi-actor-depth.png"));
        File.WriteAllText(Path.Combine(destination,"multi-actor-depth.json"),"{\"passed\":"+(passed?"true":"false")+"}");
        view.Free();if(!passed)throw new Exception("Multi-actor depth sorting failed");
    }

    private static async Task CheckImpactTiming(SceneTree tree, Node2D body, IllusionistOrbitFx fx, string destination)
    {
        var checks = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        var target = new Creature((Player)null!, 100, 100);
        var previous = IllusionistDamageFxScope.Current.Value;
        var state = fx.DebugState();
        int strikes = state["strikes"].AsInt32();
        void Check(string name, bool passed) => checks.Add(new() { ["name"] = name, ["passed"] = passed });
        IllusionistDamageFxScope Scope() => new(_ => fx.StrikeAt(new Vector2(850, 430)));
        void NativeHit(decimal amount)
        {
            IllusionistDamageFxScope.Current.Value = Scope();
            int hp = target.CurrentHp;
            ulong frame = Engine.GetProcessFrames();
            target.LoseHpInternal(amount, ValueProp.Move);
            state = fx.DebugState();
            Check("native HP and impact in the same frame", target.CurrentHp == hp - (int)amount
                && state["strikes"].AsInt32() == ++strikes
                && state["impact_frame"].AsInt64() == (long)frame);
            IllusionistDamageFxScope.Current.Value = previous;
        }
        try
        {
            foreach (string placeholder in new[] { "vfx/vfx_attack_slash", "vfx/vfx_attack_blunt" })
            {
                var command = new AttackCommand(6m).WithHitFx(placeholder, "preserved-sound", "preserved-debug-sound");
                Patches.IllusionistOrbitAttackPatch.Configure(command, fx);
                Check("replace overlapping " + placeholder, command.HitVfx == null
                    && command.HitSfx == "preserved-sound" && command.TmpHitSfx == "preserved-debug-sound");
            }
            var special = new AttackCommand(6m).WithHitFx("vfx/custom_card_effect");
            Patches.IllusionistOrbitAttackPatch.Configure(special, fx);
            Check("preserve specific card effects", special.HitVfx == "vfx/custom_card_effect");
            var aim = new Vector2(850, 430);
            Vector2[] origin = fx.FlameWorldPositions();
            fx.BeginTeleportOut(.18f);
            for (int frame = 0; frame < 2; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Vector2[] moving = fx.FlameWorldPositions();
            Check("flames fade at departure without travelling", target.CurrentHp == 100
                && moving.Where((point, i) => point.DistanceTo(origin[i]) < .01f).Count() == 3
                && fx.FlameOpacities().All(alpha => alpha > 0 && alpha < 1));
            for (int frame = 0; frame < 4; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Check("fully invisible before teleport", target.CurrentHp == 100 && fx.FlameOpacities().All(alpha => alpha < .001f));
            NativeHit(6m);
            Check("target reappearance shares the HP frame", fx.FlameWorldPositions().All(point => point.DistanceTo(aim) < 115)
                && fx.FlameOpacities().All(alpha => alpha >= .79f));
            bool corridorEmpty = true;
            for (int frame = 0; frame < 38; frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                Vector2[] positions = fx.FlameWorldPositions();
                float[] opacity = fx.FlameOpacities();
                for (int i = 0; i < 3; i++)
                    if (opacity[i] > .001f && positions[i].X > 525 && positions[i].X < 750) corridorEmpty = false;
                if (frame == 12) Check("all three flames remain fully visible at contact for half a second",
                    opacity.All(alpha => alpha > .99f) && positions.All(point => point.X > 750));
            }
            Check("no visible flame between actor and target", corridorEmpty);
            Check("all flames reappear on the local orbit", fx.FlameWorldPositions().All(point => point.X < 520)
                && fx.FlameOpacities().All(alpha => alpha > .99f));
            fx.BeginTeleportOut(0);
            NativeHit(1m);
            fx.BeginTeleportOut(0);
            NativeHit(1m);
            Check("same-frame Instant attacks cannot leave flames invisible", fx.FlameOpacities().All(alpha => alpha >= .49f));
            target.GainBlockInternal(10m);
            decimal blocked = target.DamageBlockInternal(6m, ValueProp.Move);
            NativeHit(6m - blocked);
            for (int i = 0; i < 3; i++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                NativeHit(2m);
            }
            Check("rapid hits are never queued", fx.DebugState()["strikes"].AsInt32() == strikes);
            IllusionistDamageFxScope.Current.Value = Scope();
            for (int i = 0; i < 5; i++) new Creature((Player)null!, 20, 20).LoseHpInternal(1m, ValueProp.Move);
            strikes += 5;
            Check("AOE includes more than three receivers", fx.DebugState()["strikes"].AsInt32() == strikes);
            target.LoseHpInternal(1m, ValueProp.Unpowered);
            Check("unpowered damage does not create an attack", fx.DebugState()["strikes"].AsInt32() == strikes);
            // Match the native async method's prefix/call/postfix boundary: the
            // continuation keeps its scope after the caller has restored its own.
            IllusionistDamageFxScope.Current.Value = Scope();
            Task delayed = DelayedNativeHit();
            IllusionistDamageFxScope.Current.Value = previous;
            await delayed;
            Check("scope survives await without leaking to caller", fx.DebugState()["strikes"].AsInt32() == ++strikes
                && ReferenceEquals(IllusionistDamageFxScope.Current.Value, previous));
            target.LoseHpInternal(1m, ValueProp.Move);
            Check("unrelated HP changes do not inherit the scope", fx.DebugState()["strikes"].AsInt32() == strikes);
            IllusionistDamageFxScope.Current.Value = Scope();
            var outer = IllusionistDamageFxScope.Current.Value;
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), (System.Collections.Generic.IEnumerable<DamageTargetData>?)null,
                1m, ValueProp.Move, null, null, null);
            Check("patched native damage restores outer context", ReferenceEquals(IllusionistDamageFxScope.Current.Value, outer));
            IllusionistDamageFxScope.Current.Value = previous;
            foreach (Variant animation in body.Call("get_skeleton_data_res").AsGodotObject().Call("get_animations").AsGodotArray())
            {
                var clip = animation.AsGodotObject();
                if (clip.Call("get_name").AsString() == "attack")
                    Check("installed attack clip is 1.5 seconds", Math.Abs(clip.Call("get_duration").AsDouble() - 1.5) < .001);
            }
            Check("normal visual sequence totals 1.50 seconds", Math.Abs(.15 + .18 + fx.DebugState()["last_return_seconds"].AsDouble() - 1.50) < .001);
            Check("flame heads are 2.8 times original size", Math.Abs(fx.DebugState()["flame_size_multiplier"].AsDouble() - 2.8) < .001);
        }
        finally { IllusionistDamageFxScope.Current.Value = previous; }
        bool passed = checks.All(c => c["passed"].AsBool());
        var report = new Godot.Collections.Dictionary { ["passed"] = passed, ["checks"] = checks };
        File.WriteAllText(Path.Combine(destination, "impact-timing-check.json"), Json.Stringify(report, "  "));
        GD.Print("MOTION5 IMPACT TIMING: " + Json.Stringify(report));
        if (!passed) throw new Exception("Native HP/impact timing failed");

        async Task DelayedNativeHit()
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            target.LoseHpInternal(1m, ValueProp.Move);
        }
    }

    private static async Task CheckCombatScale(SceneTree tree, string destination)
    {
        // Exercise the borrowed scene and Ready swap used in combat, not a standalone sprite.
        var view = new SubViewport { Size = new Vector2I(1100, 700), RenderTargetUpdateMode = SubViewport.UpdateMode.Always, ProcessMode = Node.ProcessModeEnum.Always };
        tree.Root.AddChild(view);
        view.AddChild(new ColorRect { Color = new Color("292832"), Size = new Vector2(1100, 700) });
        view.AddChild(new ColorRect { Color = new Color("55505f"), Position = new Vector2(40, 600), Size = new Vector2(1020, 2) });
        PackedScene scene = ResourceLoader.Load<PackedScene>(SceneHelper.GetScenePath("creature_visuals/necrobinder"));
        var result = new Godot.Collections.Dictionary();
        bool passed = true;
        foreach (var sample in new[] { ("Native Necrobinder", 190f, 1f, false), ("Illusionist", 550f, 1f, true), ("Parent 0.8 + flipped", 900f, .8f, true) })
        {
            var visuals = scene.Instantiate<NCreatureVisuals>();
            visuals.Position = new Vector2(sample.Item2, 600);
            visuals.Scale = Vector2.One * sample.Item3;
            Node2D body = visuals.GetNode<Node2D>("%Visuals");
            Vector2 borrowedScale = body.Scale;
            if (sample.Item3 < 1f) body.Scale = new Vector2(-body.Scale.X, body.Scale.Y);
            if (sample.Item4 && !SpineBody.SwapOnReady(visuals, 1f)) throw new Exception("Combat scene swap failed");
            view.AddChild(visuals);
            float sourceHeightAtWorldScale = 1085f * body.GlobalTransform.Y.Length();
            if (sample.Item4)
            {
                passed &= body.HasMeta("illusionist_swapped") && body.HasNode("IllusionistOrbitFx")
                    && Mathf.IsEqualApprox(body.Scale.Y, .30f)
                    && Mathf.IsEqualApprox(body.Scale.X, sample.Item3 < 1f ? -.30f : .30f)
                    && Mathf.IsEqualApprox(visuals.Scale.Y, sample.Item3)
                    && Mathf.Abs(sourceHeightAtWorldScale - 325.5f * sample.Item3) < .1f;
            }
            result[sample.Item1] = new Godot.Collections.Dictionary
            {
                ["borrowed_scale"] = borrowedScale.ToString(), ["body_scale"] = body.Scale.ToString(),
                ["parent_scale"] = visuals.Scale.ToString(), ["global_scale"] = body.GlobalScale.ToString(),
                ["body_position"] = body.Position.ToString(),
                ["source_height_at_world_scale"] = sample.Item4 ? sourceHeightAtWorldScale : 0f,
                ["native_bounds_height"] = visuals.Bounds.Size.Y
            };
            view.AddChild(new Label { Text = sample.Item1, Position = new Vector2(sample.Item2 - 110, 635) });
        }
        for (int frame = 0; frame < 12; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw(false);
        view.GetTexture().GetImage().SavePng(Path.Combine(destination, "combat-scale-comparison.png"));
        result["passed"] = passed;
        File.WriteAllText(Path.Combine(destination, "combat-scale-check.json"), Json.Stringify(result, "  "));
        GD.Print("MOTION5 COMBAT SCALE: " + Json.Stringify(result));
        view.Free();
        if (!passed) throw new Exception("Borrowed combat scene scale or facing did not pass");
    }

    private static async Task CheckOcclusion(SceneTree tree, SubViewport view, Node2D body, IllusionistOrbitFx fx, string destination)
    {
        Variant updateMode = body.Call("get_update_mode");
        GodotObject track = body.Call("get_animation_state").AsGodotObject().Call("get_current", 0).AsGodotObject();
        track.Call("set_time_scale", 0f);
        foreach (string name in ClassDB.ClassGetIntegerConstantList("SpineConstant"))
            if (name.Contains("MANUAL")) body.Call("set_update_mode", ClassDB.ClassGetIntegerConstant("SpineConstant", name));
        body.SetProcess(false); body.SetPhysicsProcess(false); fx.SetProcess(false);
        body.Call("update_skeleton", 0f);
        var back = body.GetNode<Node2D>("IllusionistOrbitBack");
        FieldInfo clock = typeof(IllusionistOrbitFx).GetField("_clock", BindingFlags.NonPublic | BindingFlags.Instance)!;
        fx.Visible = false; back.Visible = false;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw(false);
        Image baseline = view.GetTexture().GetImage();
        baseline.SavePng(Path.Combine(destination, "depth-game-baseline.png"));
        var result = new Godot.Collections.Dictionary();
        foreach (var probe in new[] { ("back", 2.77718308f, 575.7432f), ("front", .77718308f, 661.4464f) })
        {
            clock.SetValue(fx, probe.Item2);
            typeof(IllusionistOrbitFx).GetField("_orbitTime", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(fx, probe.Item2);
            fx.Visible = true; back.Visible = true; fx._Process(0);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            RenderingServer.ForceDraw(false);
            Image actual = view.GetTexture().GetImage();
            actual.SavePng(Path.Combine(destination, $"depth-game-{probe.Item1}.png"));
            int cx = (int)(285 + (375 - 360) * .58f), cy = (int)(750 + (probe.Item3 - 1185) * .58f);
            float largest = 0;
            for (int y = cy - 10; y <= cy + 10; y++) for (int x = cx - 14; x <= cx + 14; x++)
            {
                Color a = actual.GetPixel(x, y), b = baseline.GetPixel(x, y);
                largest = Mathf.Max(largest, Mathf.Max(Mathf.Abs(a.R - b.R), Mathf.Max(Mathf.Abs(a.G - b.G), Mathf.Abs(a.B - b.B))));
            }
            result[probe.Item1 + "_max_difference"] = largest;
        }
        result["passed"] = result["back_max_difference"].AsDouble() < .01 && result["front_max_difference"].AsDouble() > .1;
        File.WriteAllText(Path.Combine(destination, "depth-game-check.json"), Json.Stringify(result, "  "));
        GD.Print("MOTION5 DEPTH: " + Json.Stringify(result));
        if (!result["passed"].AsBool()) throw new Exception("Actual game front/back occlusion did not pass");
        clock.SetValue(fx, 0f);typeof(IllusionistOrbitFx).GetField("_orbitTime", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(fx,0f);fx.SetProcess(true);track.Call("set_time_scale",1f);body.Call("set_update_mode", updateMode);body.SetProcess(true);
    }
}
