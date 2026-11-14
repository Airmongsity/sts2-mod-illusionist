using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

public sealed class IllusionistOrbitDamageScopePatch : IPatchMethod
{
    public static string PatchId => "illusionist_orbit_damage_scope";
    public static string Description => "Associate native async attack damage with its visual dealer";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => new[]
    {
        new ModPatchTarget(typeof(CreatureCmd), nameof(CreatureCmd.Damage), new[]
        {
            typeof(PlayerChoiceContext), typeof(IEnumerable<DamageTargetData>), typeof(decimal),
            typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay), typeof(ModifyDamageHookType)
        })
    };

    private static void Prefix(Creature? dealer, ValueProp props, out IllusionistDamageFxScope? __state)
    {
        __state = IllusionistDamageFxScope.Current.Value;
        IllusionistDamageFxScope.Current.Value = null;
        try
        {
            IllusionistOrbitFx? fx = props.HasFlag(ValueProp.Move) ? IllusionistOrbitFx.For(dealer) : null;
            if (fx != null) IllusionistDamageFxScope.Current.Value = new(fx.StrikeCreature);
        }
        catch (Exception ex) { Log.Error($"[illusionist] Orbit damage scope failed: {ex}"); }
    }

    // Restore the caller immediately. The native async state machine has captured
    // its own execution context, retaining this damage scope through hook awaits.
    private static void Postfix(IllusionistDamageFxScope? __state) => IllusionistDamageFxScope.Current.Value = __state;
    private static Exception? Finalizer(Exception? __exception, IllusionistDamageFxScope? __state)
    {
        if (__exception != null) IllusionistDamageFxScope.Current.Value = __state;
        return __exception;
    }
}
