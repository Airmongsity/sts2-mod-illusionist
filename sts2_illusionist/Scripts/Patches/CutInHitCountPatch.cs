using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Hooks;
using STS2RitsuLib.Patching.Models;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Applies Cut In once at the native hit-count boundary. The model hook is deliberately
/// not overridden, so there is only one execution-time reduction path.
/// </summary>
public sealed class CutInHitCountPatch : IPatchMethod
{
    private sealed class ProcessedAttack
    {
    }

    private static readonly ConditionalWeakTable<AttackCommand, ProcessedAttack> ProcessedAttacks = new();

    public static string PatchId => "illusionist_cut_in_hit_count";

    public static string Description => "Apply Cut In after native attack hit-count listeners";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(
            typeof(Hook),
            nameof(Hook.ModifyAttackHitCount),
            new[]
            {
                typeof(ICombatState),
                typeof(AttackCommand),
                typeof(int),
            }),
    };

    private static void Postfix(AttackCommand attackCommand, ref decimal __result)
    {
        CutInPower? power = attackCommand.Attacker?.GetPower<CutInPower>();
        if (power == null || ProcessedAttacks.TryGetValue(attackCommand, out _))
        {
            return;
        }

        ProcessedAttacks.Add(attackCommand, new ProcessedAttack());
        __result = power.ConsumeHitCount((int)__result);
    }
}
