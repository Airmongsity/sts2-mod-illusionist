using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

public sealed class IllusionistOrbitImpactPatch : IPatchMethod
{
    public static string PatchId => "illusionist_orbit_damage_impact";
    public static string Description => "Show Illusionist impact on the exact native HP-change frame";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => new[]
    {
        new ModPatchTarget(typeof(Creature), nameof(Creature.LoseHpInternal), new[] { typeof(decimal), typeof(ValueProp) })
    };
    private static void Prefix(Creature __instance, out int __state) => __state = __instance.CurrentHp;
    private static void Postfix(Creature __instance, ValueProp props, int __state)
    {
        try {
            if (__instance.CurrentHp < __state) IllusionistOrbitFx.For(__instance)?.Hurt();
            IllusionistDamageFxScope.Current.Value?.Notify(__instance, props);
        }
        catch (Exception ex) { Log.Error($"[illusionist] Orbit impact FX failed: {ex}"); }
    }
}
