using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Swap in custom bitmap art for the Illusionist's own power icons (the buff/debuff icons under the
/// character). <c>Icon</c> (small) and <c>BigIcon</c> (tooltip) both return <see cref="Texture2D"/>, so a
/// postfix can substitute a runtime-decoded image. Only our own powers are affected
/// (<see cref="PowerArt"/> returns null otherwise); a power with no art file keeps its borrowed icon.
/// (Phase 2 of the RitsuLib migration replaces this with <c>ModPowerTemplate.CustomIconPath</c>.)
/// </summary>
public sealed class PowerArtPatch : IPatchMethod
{
    public static string PatchId => "illusionist_power_art";

    public static string Description => "Serve runtime-decoded icons for Illusionist powers";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(PowerModel), nameof(PowerModel.Icon), MethodType.Getter),
        new(typeof(PowerModel), nameof(PowerModel.BigIcon), MethodType.Getter),
    };

    private static void Postfix(PowerModel __instance, ref Texture2D __result)
    {
        ImageTexture? art = PowerArt.For(__instance);
        if (art != null)
        {
            __result = art;
        }
    }
}
