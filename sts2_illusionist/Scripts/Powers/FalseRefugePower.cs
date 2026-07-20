using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// Whenever an enemy gains Block, it loses HP equal to the Block gained per stack.
/// </summary>
[RegisterPower]
public sealed class FalseRefugePower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (amount <= 0m || base.Amount <= 0m)
        {
            return Task.CompletedTask;
        }

        if (creature == base.Owner
            || base.Owner.CombatState == null || !base.Owner.CombatState.HittableEnemies.Contains(creature)
            || !creature.IsAlive)
        {
            return Task.CompletedTask;
        }

        // Defer the HP loss OUT of the AfterBlockGained hook. Dealing it inline can kill a creature
        // mid-iteration while another power loops the creature list (RampartPower grants Block to each
        // enemy at side-turn-start; Imagined Foe grants Block to all creatures), which throws
        // "Collection was modified; enumeration operation may not execute" and freezes the game
        // (observed vs Kaiser Crab / Rampart, 2026-07-13). Fire-and-forget with a one-frame yield so
        // the outer enumeration finishes before the damage - and any death removal - runs.
        Flash();
        Creature target = creature;
        Creature dealer = base.Owner;
        decimal dmg = amount * base.Amount;
        _ = DeferDamageAsync(dealer, target, dmg);
        return Task.CompletedTask;
    }

    private static async Task DeferDamageAsync(Creature dealer, Creature target, decimal dmg)
    {
        try
        {
            await Cmd.Wait(0.01f);
            if (!target.IsAlive || target.CombatState == null || !target.CombatState.HittableEnemies.Contains(target))
            {
                return;
            }

            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                target,
                dmg,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim,
                dealer,
                null,
                null);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] FalseRefuge deferred damage failed: {ex}");
        }
    }
}
