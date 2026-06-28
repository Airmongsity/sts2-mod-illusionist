using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Recolour the Illusionist's card frame to a fixed colour (#573534) instead of the borrowed pink HSV
/// material. The game tints frames with <c>hsv.gdshader</c> (a hue-rotate that can't target an exact
/// colour), so we substitute a small multiply-tint ShaderMaterial (<c>shaders/frame_tint.gdshader</c>)
/// via the pool's <see cref="CardPoolModel.FrameMaterial"/> getter — only for our pool. Built once and
/// cached. If the shader fails to load we leave the borrowed material, so this can't break other cards.
/// </summary>
[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.FrameMaterial), MethodType.Getter)]
public static class IllusionistFramePatch
{
    /// <summary>Card-frame colour. Tune to taste.</summary>
    private static readonly Color FrameColor = new Color("573534");

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

        Shader? shader = ResourceLoader.Load<Shader>("res://illusionist/shaders/frame_tint.gdshader");
        if (shader == null)
        {
            Log.Error("[illusionist] Frame: frame_tint.gdshader not found; keeping borrowed frame.");
            return null;
        }

        _material = new ShaderMaterial { Shader = shader };
        _material.SetShaderParameter("tint", FrameColor);
        return _material;
    }
}
