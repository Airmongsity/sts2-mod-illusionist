using Godot;

namespace Illusionist.Scripts;

/// <summary>Seat-anchored campsite pose, with fixed lower cloth, feet and lamp.</summary>
public partial class IllusionistRestPose : Sprite2D
{
    public const string Art = "res://illusionist/art/motion5/";
    public static readonly Vector2 SeatPixel = new(440,884);
    public const float DisplayHeight = 280f;
    private ShaderMaterial _breathing = null!;
    private float _clock;

    public static IllusionistRestPose? Create(Vector2 seat, float parentGlobalScale)
    {
        var texture = ResourceLoader.Load<Texture2D>(Art+"rest_seated.png");
        var shader = ResourceLoader.Load<Shader>(Art+"rest_breathe.gdshader");
        if(texture == null || shader == null) return null;
        var pose = new IllusionistRestPose { Name="IllusionistRestSpine",Texture=texture,
            Centered=false,Offset=-SeatPixel,Position=seat };
        pose.Scale=Vector2.One*(DisplayHeight/texture.GetHeight()/Mathf.Max(.001f,parentGlobalScale));
        pose._breathing=new ShaderMaterial{Shader=shader};pose.Material=pose._breathing;
        return pose;
    }

    public override void _Process(double delta)
    {
        _clock+=(float)delta;
        _breathing.SetShaderParameter("phase",Mathf.Tau*_clock/8f);
    }
}
