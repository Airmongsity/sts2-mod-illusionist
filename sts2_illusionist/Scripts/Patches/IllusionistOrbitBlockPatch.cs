using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

public sealed class IllusionistOrbitBlockPatch : IPatchMethod
{
    public static string PatchId => "illusionist_orbit_block_fx";
    public static string Description => "Form Illusionist's orbit shield when block actually increases";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => new[] { new ModPatchTarget(typeof(Creature), nameof(Creature.GainBlockInternal)) };
    private static void Prefix(Creature __instance, out int __state) => __state = __instance.Block;
    private static void Postfix(Creature __instance, int __state)
    {
        if (__instance.Block <= __state) return;
        try { IllusionistOrbitFx.For(__instance)?.ShowGuard(); }
        catch (Exception ex) { Log.Error($"[illusionist] Orbit block FX failed: {ex}"); }
    }
}
