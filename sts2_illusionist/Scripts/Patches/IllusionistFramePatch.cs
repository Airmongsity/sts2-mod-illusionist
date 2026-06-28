using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Recolour the Illusionist's card frame to a dark red-brown (≈#573534) instead of the borrowed pink.
/// Reuses the GAME's own frame shader (<c>res://shaders/hsv.gdshader</c>, a hue/sat/value adjust) — no
/// custom shader — applied via the pool's <see cref="CardPoolModel.FrameMaterial"/> getter, only for our
/// pool. The h/s/v below approximate the target (exact-hex precision is unnecessary — visually identical);
/// tune them if the shade looks off. Built once and cached; if the shader fails to load we leave the
/// borrowed material.
/// </summary>
[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.FrameMaterial), MethodType.Getter)]
public static class IllusionistFramePatch
{
    // hsv.gdshader params (compare: card_frame_red = 0.025/0.85/1.0). Dark, muted, red-brown. Tune.
    private const float H = 0.025f;
    private const float S = 0.50f;
    private const float V = 0.42f;

    private static ShaderMaterial? _material;
    private static bool _tried;

    private static void Postfix(CardPoolModel __instance, ref Material __result)
    {
        if (__instance is not global::Illusionist.Scripts.IllusionistCardPool)
        {
            return;
        }

        ShaderMaterial? mat = Ensure();
        if (mat != null)
        {
            __result = mat;
        }
    }

    private static ShaderMaterial? Ensure()
    {
        if (_tried)
        {
            return _material;
        }
        _tried = true;

        Shader? shader = ResourceLoader.Load<Shader>("res://shaders/hsv.gdshader");
        if (shader == null)
        {
            Log.Error("[illusionist] Frame: hsv.gdshader not found; keeping borrowed frame.");
            return null;
        }

        _material = new ShaderMaterial { Shader = shader };
        _material.SetShaderParameter("h", H);
        _material.SetShaderParameter("s", S);
        _material.SetShaderParameter("v", V);
        return _material;
    }
}
