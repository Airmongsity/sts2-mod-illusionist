using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace Illusionist.Scripts;

/// <summary>Three persistent flames. The Spine body and FX share source coordinates,
/// while target positions are resolved from the current combat, not baked into the rig.</summary>
public partial class IllusionistOrbitFx : Node2D
{
    private const string Art = "res://illusionist/art/motion5/";
    // Separate silhouettes around the contact point, rather than three overlapping heads.
    private static readonly Vector2[] ImpactOffsets = { new(0, -155), new(-145, 95), new(145, 95) };
    private static readonly Vector2 Origin = new(360, 1185);
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private Node2D _body = null!;
    private IllusionistOrbitLayer _back = null!;
    private IllusionistTeleportLayer? _contactLayer;
    private readonly List<Vector3> _batchTargets = new();
    private readonly List<(Vector3 Position, float Time)> _impacts = new();
    private readonly Vector3[] _release = new Vector3[3];
    private readonly Vector3[] _targets = new Vector3[3];
    private readonly float[] _releaseOpacity = new float[3];
    private readonly float[] _releaseAngle = new float[3];
    private readonly bool[] _releaseRemote = new bool[3];
    private float _clock;
    private float _attackStart = -100;
    private float _vanishStart = -100;
    private float _vanishDuration;
    private float _trailStart = -100;
    private float _maxTrailLengthDrawn = 0;
    private float _prepareStart = -100;
    private ulong _lastImpactFrame = ulong.MaxValue;
    private int _strikeCount;
    private float _guardUntil = -100;
    private float _guard;
    private float _fade = 1;
    private float _quiet = 1;
    private string _animation = "idle_loop";
    private int _drawCount;
    private bool _directed;
    private float _orbitTime, _powerStart = -100, _hurtStart = -100;
    private readonly bool[] _contactPlayed = new bool[3];
    private Node? _hitContainer;
    private int _aimCount;
    private float _guardStarted = -100;
    private IllusionistParticleFire _particleFire = null!;
    private bool _periodicFire;
    private static readonly float[] PowerCdf = BuildPowerCdf();
    private float OrbitSpeed => 1 + (PowerPhase(_clock+.001f)-PowerPhase(_clock-.001f))/.002f/(Mathf.Tau*.25f);
    public void PowerCast() => _powerStart = _clock;
    public void Hurt() => _hurtStart = _clock;
    private Vector3 Shake(float time) { float t = time - _hurtStart; return t < 0 || t > .45f ? Vector3.Zero
        : new Vector3(Mathf.Sin(t * 38) * 6, Mathf.Sin(t * 32) * 2, 0) * Mathf.Exp(-t * 9); }

    public Godot.Collections.Dictionary DebugState() => new()
    {
        ["clock"] = _clock, ["fade"] = _fade, ["draws"] = _drawCount,
        ["processing"] = IsProcessing(), ["visible"] = IsVisibleInTree(),
        ["position"] = GlobalPosition, ["scale"] = GlobalScale,
        ["texture_size"] = new Vector2(40,37),
        ["children"] = _body.GetChildCount(), ["guard"] = _guard,
        ["back_index"] = _back.GetIndex(), ["front_index"] = GetIndex(),
        ["strikes"] = _strikeCount, ["impact_frame"] = (long)_lastImpactFrame,
        ["attack_age"] = _clock - _attackStart, ["impact_count"] = _impacts.Count,
        ["last_return_seconds"] = HomeFull(2),
        ["max_flame_trail_source_px"] = _maxTrailLengthDrawn,
        ["flame_size_multiplier"] = IllusionistParticleFire.SourceScale,
        ["vanish_age"] = _clock - _vanishStart,
        ["contact_layer_z"] = IsInstanceValid(_contactLayer) ? _contactLayer!.ZIndex : -1,
        ["orbit_speed"] = OrbitSpeed, ["guard_tail_width"] = 12 + _guard * 44,
        ["attack_stagger"] = .20f, ["procedural_fire"] = true,
        ["persistent_particles"] = _particleFire.LiveParticles, ["upright_fire"] = true,
        ["renderer"] = "html-particle-density-v1", ["particle_lifetime_ratio"] = 2f/3,
        ["guard_history_copies"] = _particleFire.GuardCopies, ["guard_started"] = _guardStarted,
        ["oldest_guard_time"] = _particleFire.OldestGuardTime,
    };

    public static void Attach(Node2D body)
    {
        if (body.HasNode("IllusionistOrbitFx")) return;
        new IllusionistOrbitFx().Initialize(body);
    }

    public void Initialize(Node2D body)
    {
        if (!Textures.ContainsKey("fx_impact")) Textures["fx_impact"] = ResourceLoader.Load<Texture2D>(Art+"fx_impact.png");
        // Stay in the creature's Z plane: negative global Z can hide rear flames
        // behind the room background. Draw behind the parent, not behind the room.
        Name = "IllusionistOrbitFx";
        _body = body;
        _back = new IllusionistOrbitLayer { Name = "IllusionistOrbitBack", ShowBehindParent = true, OwnerFx = this };
        body.AddChild(_back);
        body.AddChild(this);
        body.MoveChild(_back, 0);
        _particleFire=new IllusionistParticleFire(body,_back,this,
            (time,i)=>(SamplePosition(time,i),FlameOpacity(time,i),IsRemoteFlame(time,i)),
            (time,i)=>(RawOrbit(time,i),1f,false),GuardAt);
    }

    public static IllusionistOrbitFx? For(Creature? creature) =>
        NCombatRoom.Instance?.GetCreatureNode(creature)?.Body?.GetNodeOrNull<IllusionistOrbitFx>("IllusionistOrbitFx");

    public void PrepareAttack() => _prepareStart = _clock;

    internal Vector2[] FlameWorldPositions()
    {
        var positions = new Vector2[3];
        for (int i = 0; i < 3; i++)
        {
            Vector3 point = SamplePosition(_clock, i);
            positions[i] = _body.ToGlobal(new Vector2(point.X, point.Y) - Origin);
        }
        return positions;
    }

    internal float[] FlameOpacities() => new[] { FlameOpacity(_clock, 0), FlameOpacity(_clock, 1), FlameOpacity(_clock, 2) };

    // The native builder supplies already-resolved targets, including random hits.
    // Disappear BEFORE native damage. Reappearance/contact belongs to the HP hook.
    public async Task TeleportAttack(IReadOnlyList<Creature> targets)
    {
        var points = new List<Vector2>();
        foreach (Creature target in targets)
        {
            var node = NCombatRoom.Instance?.GetCreatureNode(target);
            if (node != null && IsInstanceValid(node) && target.IsAlive) points.Add(node.VfxSpawnPosition);
        }
        if (points.Count == 0 || _animation == "die") return;
        BeginDirectedAttack(points, targets[0].GetVfxContainer());
        await Cmd.CustomScaledWait(.34f, .34f);
    }

    public void BeginDirectedAttack(IReadOnlyList<Vector2> points, Node? hitContainer = null)
    {
        if (points.Count == 0 || _animation == "die") return;
        EnsureContactLayer(hitContainer);
        _directed = true; _vanishStart = -100; _attackStart = _trailStart = _clock;
        _aimCount = points.Count; _batchTargets.Clear();
        for (int i = 0; i < 3; i++)
        {
            Vector2 p = _body.ToLocal(points[i % points.Count]) + Origin;
            _targets[i] = new Vector3(p.X, p.Y, 1); _contactPlayed[i] = false;
        }
    }

    private void EnsureContactLayer(Node? container)
    {
        _hitContainer = container ?? _body.GetParent();
        if (!IsInstanceValid(_contactLayer))
        {
            _contactLayer = new IllusionistTeleportLayer { Name = "IllusionistTeleportFlames", OwnerFx = this, ZIndex = 2 };
            _hitContainer.AddChild(_contactLayer);
            _particleFire.SetRemoteParent(_contactLayer);
        }
        _contactLayer!.GlobalTransform = _body.GlobalTransform;
    }

    public void BeginTeleportOut(float duration)
    {
        if (_animation == "die") return;
        for (int i = 0; i < 3; i++)
        {
            _release[i] = SamplePosition(_clock, i);
            _releaseOpacity[i] = FlameOpacity(_clock, i);
            _releaseRemote[i] = IsRemoteFlame(_clock, i);
            _releaseAngle[i] = FlameAngle(_clock, i);
        }
        _attackStart = -100;
        _vanishStart = _trailStart = _clock;
        _vanishDuration = duration;
    }

    // Called immediately after the native HP operation, including blocked hits.
    // The receiver has already been resolved by the game's damage/redirection hooks.
    public void StrikeCreature(Creature target)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(target);
        if (node == null || !IsInstanceValid(node) || _animation == "die") return;
        StrikeAt(node.VfxSpawnPosition, target.GetVfxContainer());
    }

    public void StrikeAt(Vector2 world, Node? hitContainer = null)
    {
        if (_animation == "die") return;
        ulong frame = Engine.GetProcessFrames();
        if ((frame != _lastImpactFrame || IsVanishing) && !(_directed && _clock - _attackStart < .8f))
        {
            _batchTargets.Clear();
            BeginDirectedAttack(new[] { world }, hitContainer);
            _attackStart = _clock - .34f;
            _vanishStart = -100;
        }
        _lastImpactFrame = frame;
        _strikeCount++;
        Vector2 local = _body.ToLocal(world) + Origin;
        Vector3 point = new(local.X, local.Y, 1);
        _batchTargets.Add(point);
        for (int i = 0; i < 3; i++) if (i % _aimCount == (_batchTargets.Count - 1) % _aimCount) _targets[i] = point;
        // Instant mode can skip the presentation wait; contact must still occur on HP loss.
        if (_clock - _attackStart < .34f) _attackStart = _clock - .34f;
        _impacts.Add((point, _clock));
        if (_impacts.Count > 32) _impacts.RemoveAt(0);
        // Native hit effects live above creature sprites. An impact drawn on the
        // attacker's own front layer can still disappear behind the enemy body.
        EnsureContactLayer(hitContainer);
        SpawnContact(world);
        _contactPlayed[0] = true;
        QueueRedraw();
        _back.QueueRedraw();
        _contactLayer!.QueueRedraw();
    }

    private void SpawnContact(Vector2 world)
    {
        var burst = new IllusionistImpactBurst { Texture = Textures["fx_impact"], ZIndex = 1 };
        (_hitContainer ?? _body.GetParent()).AddChild(burst);
        burst.GlobalPosition = world;
        burst.GlobalScale = Vector2.One * _body.GlobalTransform.X.Length();
    }

    public void ShowGuard()
    {
        if (_animation == "die") return;
        if (_clock > _guardUntil + .65f) _guardStarted = _clock;
        _guardUntil = _clock + 1.5f;
        if (_animation is "idle_loop" or "low_health_loop")
        {
            for (Node? parent = _body.GetParent(); parent != null; parent = parent.GetParent())
                if (parent is MegaCrit.Sts2.Core.Nodes.Combat.NCreature creature)
                {
                    creature.SetAnimationTrigger("IllusionistDefend");
                    break;
                }
        }
    }

    public override void _Process(double delta)
    {
        _clock += (float)delta;
        _orbitTime += (float)delta * OrbitSpeed;
        var state = _body.Call("get_animation_state").AsGodotObject();
        var track = state?.Call("get_current", 0).AsGodotObject();
        if (track != null)
        {
            string nextAnimation = track.Call("get_animation").AsGodotObject()?.Call("get_name").AsString() ?? "idle_loop";
            if (nextAnimation == "hurt" && _animation != "hurt") Hurt();
            _animation = nextAnimation;
            float time = (float)track.Call("get_track_time").AsDouble();
            _fade = _animation == "die" ? 1 - Smooth(.25f, 1.2f, time) : 1;
            _quiet = _animation == "relaxed_loop" ? .6f : _animation == "low_health_loop" ? .42f : 1;
        }
        for (int i = _impacts.Count - 1; i >= 0; i--)
            if (_clock - _impacts[i].Time >= .16f) _impacts.RemoveAt(i);
        _guard = Mathf.MoveToward(_guard, _clock < _guardUntil ? 1 : 0, (float)delta * 4f);
        if (_directed) for (int i = 1; i < 3; i++)
            if (!_contactPlayed[i] && _clock - _attackStart >= .34f + i * .20f)
            {
                _contactPlayed[i] = true;
                SpawnContact(_body.ToGlobal(new Vector2(_targets[i].X, _targets[i].Y) - Origin));
            }
        // SpineMesh2D children ALSO draw behind their parent. The rear layer
        // must precede every mesh, not merely use ShowBehindParent itself.
        if (_back.GetIndex() != 0) _body.MoveChild(_back, 0);
        if (GetIndex() != _body.GetChildCount() - 1) _body.MoveChild(this, -1);
        QueueRedraw();
        _back.QueueRedraw();
        if (IsInstanceValid(_contactLayer))
        {
            _contactLayer!.GlobalTransform = _body.GlobalTransform;
            _contactLayer.QueueRedraw();
        }
        _periodicFire=!IsVanishing&&_clock-_powerStart>=6&&_clock-_hurtStart>.95f&&_clock-_attackStart>2;
        _particleFire.Update(_clock,_guardStarted,_fade*_quiet,IsInstanceValid(_contactLayer),_periodicFire);
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_contactLayer) && !_contactLayer!.IsQueuedForDeletion()) _contactLayer.QueueFree();
    }

    private static float Smooth(float a, float b, float value)
    {
        float x = Mathf.Clamp((value - a) / (b - a), 0, 1);
        return x * x * (3 - 2 * x);
    }

    private Vector3 Orbit(float time,int i)=>RawOrbit(time,i,PowerPhase(time),PowerSeparation(time))+Shake(time);
    private static Vector3 RawOrbit(float time,int i,float extraAngle=0,float separation=0)
    {
        float tt=Mathf.PosMod(time,16),s=IllusionistParticleFire.SourceScale;
        float angle=tt*Mathf.Tau/4+extraAngle+i*Mathf.Tau/3;
        float radius=(95+(i-1)*12*separation)*s;
        return new Vector3(375+(i-1)*12*separation*s+radius*Mathf.Cos(angle),
            1185-(637.6f-420)*s+(i-1)*30*separation*s+26*s*Mathf.Sin(angle)+IllusionistParticleFire.Bob(tt,i)*s,
            Mathf.Sin(angle));
    }
    private static float[] BuildPowerCdf()
    {
        var values=new float[601];
        for(int k=1;k<=600;k++){float u=(k-.5f)/100;values[k]=values[k-1]+Mathf.Exp(-u/1.45f)*Smooth(0,.14f,u)*(1-Smooth(4.5f,6,u))*.01f;}
        return values;
    }
    private float PowerPhase(float time)
    {
        float u=time-_powerStart;if(u<=0)return 0;if(u>=6)return Mathf.Tau;
        float q=u*100;int k=(int)q;return Mathf.Tau*Mathf.Lerp(PowerCdf[k],PowerCdf[k+1],q-k)/PowerCdf[600];
    }
    private float PowerSeparation(float time)=>Smooth(0,.20f,time-_powerStart)*(1-Smooth(3.5f,6,time-_powerStart));

    // Normal mode: 0.15 s preparation + 0.18 s departure + 1.17 s contact/return = 1.50 s.
    // The flames stay readable at contact for half a second before teleporting home.
    private static float RemoteFade(int i) => .50f + .035f * i;
    private static float RemoteGone(int i) => .72f + .035f * i;
    private static float HomeStart(int i) => .72f + .20f * i;
    private static float HomeFull(int i) => .94f + .20f * i;
    private bool IsVanishing => _vanishStart > _attackStart;

    private bool IsRemoteFlame(float time, int i) => IsVanishing ? _releaseRemote[i]
        : _directed && time - _attackStart >= .12f + i * .20f && time - _attackStart < HomeStart(i);

    private float FlameOpacity(float time, int i)
    {
        if (IsVanishing) return _vanishDuration <= 0 ? 0
            : _releaseOpacity[i] * (1 - Smooth(0, _vanishDuration * .8f, time - _vanishStart));
        float age = time - _attackStart;
        if (_directed)
        {
            float t = age - i * .20f;
            if (t < 0) return 1;
            if (t < .12f) return 1 - Smooth(0, .10f, t);
            if (t < .34f) return Smooth(.12f, .18f, t);
            if (t < .72f) return 1 - Smooth(.48f, .64f, t);
            return Smooth(.72f, .94f, t);
        }
        if (age < 0) return 1;
        if (age < HomeStart(i)) return (.8f + .2f * Smooth(0, .10f, age)) * (1 - Smooth(RemoteFade(i), RemoteGone(i), age));
        return Smooth(HomeStart(i), HomeFull(i), age);
    }

    private float FlameAngle(float time, int i)
    {
        if (IsVanishing) return _releaseAngle[i];
        if (IsRemoteFlame(time, i))
        {
            Vector3 v = SamplePosition(time + .002f, i) - SamplePosition(time - .002f, i);
            return Mathf.Atan2(v.Y, v.X);
        }
        Vector3 velocity = Orbit(time + .003f, i) - Orbit(time - .003f, i);
        return Mathf.Atan2(velocity.Y, velocity.X);
    }

    private Vector3 SamplePosition(float time, int i)
    {
        if (IsVanishing) return _release[i];
        if (!IsRemoteFlame(time, i)) return Orbit(time, i);
        float t = time - _attackStart - i * .20f;
        Vector3 direction = i switch { 0 => new(-250, -230, 0), 1 => new(250, -150, 0), _ => new(-240, 110, 0) };
        if (t < .34f) return _targets[i] + direction * (1 - Mathf.Pow(Mathf.Clamp((t-.20f)/.14f,0,1),3));
        float rebound = 1 - Mathf.Pow(1 - Mathf.Clamp((t-.34f)/.22f,0,1),2);
        return _targets[i] + new Vector3(-direction.X * .35f, -150, 0) * rebound;
    }

    public override void _Draw() { _drawCount++; DrawLayer(this, true); }

    internal void DrawLayer(Node2D canvas, bool front)
    {
        if (_fade < .001f) return;
        DrawAmbientSparks(canvas,front);
    }

    internal void DrawRemoteFlames(Node2D canvas) { }

    private float GuardAt(float time) => Smooth(_guardStarted,_guardStarted+.35f,time)
        *(1-Smooth(_guardUntil,_guardUntil+.65f,time));

    private void DrawAmbientSparks(Node2D canvas,bool front)
    {
        float scale=_body.GlobalTransform.X.Length()*IllusionistParticleFire.SourceScale;
        float time=_periodicFire?Mathf.PosMod(_clock,16):_clock;
        canvas.DrawSetTransformMatrix(canvas.GlobalTransform.AffineInverse());
        for(int i=0;i<3;i++)for(int n=(int)Math.Ceiling((time-.9)/.25);n<=(int)Math.Floor(time/.25);n++)
        {
            float born=n*.25f+i*.071f,age=time-born;
            if(age<0||age>.75f)continue;Vector3 p=_periodicFire?RawOrbit(born,i):SamplePosition(born,i);
            if((p.Z>=0)!=front||(!_periodicFire&&IsRemoteFlame(born,i)))continue;
            int id=((n%64)+64)%64;float alpha=(_periodicFire?1:FlameOpacity(born,i))*.55f*Mathf.Sin(Mathf.Pi*age/.75f)*_fade*_quiet;
            if(alpha<.01f)continue;
            Vector2 velocity=new(Mathf.Sin(id*7.13f+i)*12,-22-(Mathf.Sin(id*2+i)+1)*10);
            Vector2 at=_body.ToGlobal(new Vector2(p.X,p.Y)-Origin)+velocity*age*scale;
            canvas.DrawLine(at-Vector2.Up*1.75f*scale,at+Vector2.Up*1.75f*scale,new Color(.85f,.62f,1,alpha),1.8f*scale,true);
        }
        canvas.DrawSetTransformMatrix(Transform2D.Identity);
    }

}

public partial class IllusionistOrbitLayer : Node2D
{
    internal IllusionistOrbitFx OwnerFx = null!;
    public override void _Draw() => OwnerFx.DrawLayer(this, false);
}

public partial class IllusionistTeleportLayer : Node2D
{
    internal IllusionistOrbitFx OwnerFx = null!;
    public override void _Draw()
    {
        if (IsInstanceValid(OwnerFx) && OwnerFx.IsInsideTree()) OwnerFx.DrawRemoteFlames(this);
    }
}

/// <summary>Short, bright contact burst in the game's foreground hit-VFX container.</summary>
public partial class IllusionistImpactBurst : Node2D
{
    internal Texture2D Texture = null!;
    private static ImageTexture? _sparkWhite;
    private float _age;
    public override void _Ready()
    {
        if(_sparkWhite==null){var image=Image.CreateEmpty(1,1,false,Image.Format.Rgba8);image.Fill(Colors.White);_sparkWhite=ImageTexture.CreateFromImage(image);}
        Material=new ShaderMaterial{Shader=ResourceLoader.Load<Shader>("res://illusionist/art/motion5/particle_ember.gdshader")};
    }
    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= .65f) { QueueFree(); return; }
        QueueRedraw();
    }
    public override void _Draw()
    {
        float scale=IllusionistParticleFire.SourceScale;
        for(int n=0;n<24;n++)
        {
            float angle=n*2.39996f,v=145+135*(Mathf.Sin(n*6.1f)*.5f+.5f),life=.42f+(n%4)*.075f;
            if(_age>life)continue;
            Vector2 velocity=new(Mathf.Cos(angle)*v,Mathf.Sin(angle)*v-30);
            float travel=(1-Mathf.Exp(-_age*3.2f))/3.2f;
            Vector2 at=(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*5+velocity*travel+new Vector2(0,_age*_age*95))*scale;
            float fade=1-IllusionistParticleFire.Smooth((_age/life-.35f)/.65f);
            Vector2 direction=(velocity*Mathf.Exp(-_age*3.2f)+new Vector2(0,190*_age)).Normalized();
            float length=(10+v*.025f)*scale;
            float width=(3.6f+n%2*1.1f)*scale;
            DrawSetTransform(at,direction.Angle()-Mathf.Pi*.5f);
            DrawTextureRect(_sparkWhite!,new Rect2(-width*1.2f,-length*.85f,width*2.4f,length*1.7f),false,new Color(1,1,1,.18f*fade));
            DrawTextureRect(_sparkWhite!,new Rect2(-width*.5f,-length*.5f,width,length),false,new Color(1,1,1,.98f*fade));
        }
        DrawSetTransform(Vector2.Zero);
    }
}
