using System;
using System.Collections.Generic;
using Godot;

namespace Illusionist.Scripts;

/// <summary>The browser's emitted-puff density model, rendered in native world space.</summary>
internal sealed class IllusionistParticleFire
{
    internal const float SourceScale = 1085f / 365f;
    private readonly Node2D _body;
    private readonly Func<float,int,(Vector3 Position,float Alpha,bool Remote)> _sample;
    private readonly Func<float,int,(Vector3 Position,float Alpha,bool Remote)> _idleSample;
    private readonly Func<float,float> _guard;
    private readonly IllusionistParticleField[,,] _fields = new IllusionistParticleField[5,3,3];
    private readonly List<Puff> _puffs = new(64);
    internal int GuardCopies { get; private set; }
    internal int LiveParticles { get; private set; }
    internal float OldestGuardTime { get; private set; }
    internal readonly record struct Puff(Vector2 Position,Vector2 Size,float Alpha,float Heat,int Plane);

    internal IllusionistParticleFire(Node2D body,Node2D back,Node2D front,
        Func<float,int,(Vector3,float,bool)> sample,Func<float,int,(Vector3,float,bool)> idleSample,Func<float,float> guard)
    {
        _body=body;_sample=sample;_idleSample=idleSample;_guard=guard;
        var shader=ResourceLoader.Load<Shader>("res://illusionist/art/motion5/particle_fire.gdshader")
            ??throw new InvalidOperationException("Particle density shader missing from PCK");
        for(int copy=4;copy>=0;copy--)for(int i=0;i<3;i++)for(int plane=0;plane<3;plane++)
        {
            var field=new IllusionistParticleField(shader){Name=$"ParticleFire_{copy}_{i}_{plane}",Visible=false};
            _fields[copy,i,plane]=field;
            (plane==0?back:front).AddChild(field);
        }
    }

    internal void SetRemoteParent(Node2D parent)
    {
        for(int copy=0;copy<5;copy++)for(int i=0;i<3;i++)
        {
            var field=_fields[copy,i,2];
            if(field.GetParent()!=parent)field.Reparent(parent);
        }
    }

    internal void Update(float time,float guardStarted,float opacity,bool remoteAvailable,bool periodic)
    {
        GuardCopies=LiveParticles=0;OldestGuardTime=time;
        float scale=_body.GlobalTransform.X.Length()*SourceScale;
        for(int copy=4;copy>=0;copy--)
        {
            float lag=copy*.08f,past=time-lag;
            float alpha=copy==0?1:_guard(time)*.55f*Mathf.Pow(1-copy/5f,.7f)*Smooth((time-guardStarted-lag)/.08f);
            if(copy>0&&(past<guardStarted||alpha<.00001f))alpha=0;
            if(copy>0&&alpha>0){GuardCopies++;OldestGuardTime=Mathf.Min(OldestGuardTime,past);}
            for(int i=0;i<3;i++)
            {
                _puffs.Clear();if(alpha>0)Build(periodic?Mathf.PosMod(past,16):past,i,scale,periodic?_idleSample:_sample);
                if(copy==0)LiveParticles+=_puffs.Count;
                for(int plane=0;plane<3;plane++)
                    _fields[copy,i,plane].UpdateField(_puffs,plane,opacity*alpha,plane!=2||remoteAvailable);
            }
        }
    }

    private void Build(float time,int i,float scale,Func<float,int,(Vector3 Position,float Alpha,bool Remote)> sample)
    {
        var head=sample(time,i);
        if(head.Alpha>.001f)
            Add(head.Position,Vector2.Zero,new Vector2(40,37),head.Alpha*.8f*.72f,.9f,head.Remote,scale);
        for(int n=(int)Math.Ceiling((time-1.02)/.0125);n<=(int)Math.Floor(time/.0125)+1;n++)
        {
            int id=((n%1280)+1280)%1280,seed=id+i*1337;
            float born=(float)(n*.0125+(Random(seed,3)-.5)*.008),age=time-born;
            if(age<0)continue;
            var source=sample(born,i);
            float life=((source.Remote?.31f:.60f)+Random(seed,7)*(source.Remote?.18f:.34f))*2/3;
            if(age>life||source.Alpha<.10f)continue;
            float u=age/life,angle=Random(seed,11)*Mathf.Tau,r=Mathf.Sqrt(Random(seed,13));
            float dx=Mathf.Cos(angle)*r*10,dy=Mathf.Sin(angle)*r*7;
            float vx=(Random(seed,17)-.5f)*23,vy=-55-Random(seed,19)*65;
            float phase=Random(seed,23)*Mathf.Tau;
            float curl=(Mathf.Sin(age*8+phase)-Mathf.Sin(phase))*age*8;
            var delta=new Vector2(dx+vx*age+Air(born,i)*age*age*.5f+curl,dy+vy*age-14*age*age);
            float width=(20+Random(seed,29)*14)*(1-u*.70f);
            Add(source.Position,delta,new Vector2(Mathf.Max(1.4f,width),Mathf.Max(2,width*(1+u*.70f))),
                source.Alpha*.38f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.Pi*u)),.65f),.96f-u*.46f,source.Remote,scale);
        }
        // The small ordinary orbit wake is distinct from the full guard afterimages.
        for(int n=(int)Math.Ceiling((time-.15)/.025);n<=(int)Math.Floor(time/.025);n++)
        {
            float born=n*.025f,age=time-born,life=.22f*2/3;if(age>life)continue;
            var source=sample(born,i);if(source.Remote||source.Alpha<.1f)continue;
            float u=age/life;int id=((n%640)+640)%640;
            float size=.30f*(1-u*.65f);
            Add(source.Position,new Vector2(Mathf.Sin(i*7+id)*age*7,-age*(18+Mathf.Sin(id)*5)),
                new Vector2(40,37)*size,source.Alpha*(1-Smooth((u-.4f)/.6f))*.18f*.72f,.9f,false,scale);
        }
    }

    private void Add(Vector3 point,Vector2 offset,Vector2 size,float alpha,float heat,bool remote,float scale)
    {
        if(alpha<.003f)return;
        Vector2 world=_body.ToGlobal(new Vector2(point.X-360,point.Y-1185));
        _puffs.Add(new Puff(world+offset*scale,size*scale,alpha,heat,remote?2:point.Z>=0?1:0));
    }

    internal static float Random(int n,int seed)
    {
        double value=Math.Sin(n*127.1+seed*311.7)*43758.5453;
        return (float)(value-Math.Floor(value));
    }
    internal static float Smooth(float x){x=Mathf.Clamp(x,0,1);return x*x*(3-2*x);}
    internal static float Noise(float time,int seed,int count)
    {
        float q=Mathf.PosMod(time,16)/16*count;int k=(int)q;
        return Mathf.Lerp(Random(k,seed),Random((k+1)%count,seed),Smooth(q-k));
    }
    internal static float Bob(float time,int i)=>84*(Noise(time,i+37,7)-.5f)+9*Mathf.Sin(Mathf.PosMod(time,16)*Mathf.Tau*3/16+i);
    private static float Air(float time,int i)=>(Noise(time,i+3,9)-.5f)*35+Mathf.Sin(Mathf.PosMod(time,16)*Mathf.Tau*3/16+i)*9;
}

internal partial class IllusionistParticleField : Node2D
{
    private readonly ShaderMaterial _material;
    private readonly Vector4[] _shapes=new Vector4[64];
    private readonly Vector2[] _tones=new Vector2[64];
    private static ImageTexture? _white;
    private Rect2 _rect;
    internal IllusionistParticleField(Shader shader)
    {
        _material=new ShaderMaterial{Shader=shader};Material=_material;
        if(_white==null){var image=Image.CreateEmpty(1,1,false,Image.Format.Rgba8);image.Fill(Colors.White);_white=ImageTexture.CreateFromImage(image);}
    }
    internal void UpdateField(List<IllusionistParticleFire.Puff> puffs,int plane,float alpha,bool enabled)
    {
        int count=0;Vector2 min=new(float.MaxValue,float.MaxValue),max=new(float.MinValue,float.MinValue);
        foreach(var p in puffs)if(p.Plane==plane&&count<64)
        {min=min.Min(p.Position-p.Size*.5f);max=max.Max(p.Position+p.Size*.5f);count++;}
        Visible=enabled&&alpha>.00001f&&count>0;if(!Visible)return;
        _rect=new Rect2(min,max-min);count=0;
        foreach(var p in puffs)if(p.Plane==plane&&count<64)
        {var pos=p.Position-min;_shapes[count]=new Vector4(pos.X,pos.Y,p.Size.X,p.Size.Y);_tones[count]=new Vector2(p.Alpha,p.Heat);count++;}
        // Canvas order stays with the actor; absolute world transform keeps fire upright.
        GlobalTransform=Transform2D.Identity;
        _material.SetShaderParameter("puffs",_shapes);_material.SetShaderParameter("tones",_tones);
        _material.SetShaderParameter("puff_count",count);_material.SetShaderParameter("field_size",_rect.Size);
        _material.SetShaderParameter("field_opacity",alpha);QueueRedraw();
    }
    public override void _Draw()=>DrawTextureRect(_white!,_rect,false);
}
