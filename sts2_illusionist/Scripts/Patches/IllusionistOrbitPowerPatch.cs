using System;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

public sealed class IllusionistOrbitPowerPatch : IPatchMethod
{
    public static string PatchId => "illusionist_orbit_power_fx";
    public static string Description => "Accelerate orbit flames when a power card is played";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => new[] { new ModPatchTarget(typeof(CardModel), nameof(CardModel.OnPlayWrapper)) };
    private static void Prefix(CardModel __instance)
    {
        if (__instance.Type != CardType.Power) return;
        try { IllusionistOrbitFx.For(__instance.Owner.Creature)?.PowerCast(); }
        catch (Exception ex) { Log.Error($"[illusionist] Orbit power FX failed: {ex}"); }
    }
}
