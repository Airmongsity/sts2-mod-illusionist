using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Enchantments;

/// <summary>
/// 先机:重放1 (First Move: Replay 1) — a permanent card enchantment applied by 重塑 (ReshapeIllusionist).
/// The enchanted card is played one extra time, but ONLY when it is the first card played this
/// turn. Uses the engine's per-card <see cref="EnchantmentModel.EnchantPlayCount"/> hook (the same
/// one the Spiral enchantment uses for plain Replay).
/// </summary>
[RegisterEnchantment]
public sealed class FirstMoveReplay : EnchantmentModel
{
    // The player explicitly chooses which card to enchant, so allow it on anything.
    public override bool CanEnchant(CardModel card) => true;

    public override int EnchantPlayCount(int originalPlayCount)
    {
        if (!base.HasCard)
        {
            return originalPlayCount;
        }

        Player? player = base.Card.Owner;
        ICombatState? combat = player?.Creature.CombatState;
        if (player == null || combat == null)
        {
            return originalPlayCount;
        }

        // GeneratePlayCount runs BEFORE this card's CardPlayStarted is logged, so "first card this
        // turn" means zero prior original (first-in-series) plays this turn.
        Creature owner = player.Creature;
        int playedThisTurn = CombatManager.Instance.History.CardPlaysStarted
            .Count(e => e.Actor == owner && e.CardPlay.IsFirstInSeries && e.HappenedThisTurn(combat));

        return playedThisTurn == 0 ? originalPlayCount + 1 : originalPlayCount;
    }
}
