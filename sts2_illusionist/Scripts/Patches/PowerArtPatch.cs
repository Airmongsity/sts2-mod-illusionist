using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Swap in custom bitmap art for the Illusionist's own power icons (the buff/debuff icons under the
/// character). <c>Icon</c> (small) and <c>BigIcon</c> (tooltip) both return <see cref="Texture2D"/>, so a
/// postfix can substitute a runtime-decoded image. Only our own powers are affected
/// (<see cref="PowerArt"/> returns null otherwise); a power with no art file keeps its borrowed icon.
/// </summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.Icon), MethodType.Getter)]
public static class PowerIconArtPatch
{
    private static void Postfix(PowerModel __instance, ref Texture2D __result)
    {
        ImageTexture? art = PowerArt.For(__instance);
        if (art != null)
        {
            __result = art;
        }
    }
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.BigIcon), MethodType.Getter)]
public static class PowerBigIconArtPatch
{
    private static void Postfix(PowerModel __instance, ref Texture2D __result)
    {
        ImageTexture? art = PowerArt.For(__instance);
        if (art != null)
        {
            __result = art;
        }
    }
}
