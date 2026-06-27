using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 返场 (EncoreIllusionist) power. At the end of your turn, pull ONE random [gold]Retain[/gold] card out of
/// your discard pile and back into your hand. Since you only play your held control cards (CounterIllusionist,
/// ForesightIllusionist, ReversalIllusionist, Catalyze) once they're worth it — and playing them sends them to discard —
/// this recurs the intent suite one card at a time: enough to keep the engine turning, slow enough
/// that it can't loop the whole suite in a single turn. Returning a single card also reliably gives
/// you a first card to lead with, which re-arms the mirror replay.
///
/// Presence-based (<see cref="PowerStackType.Single"/>): a second EncoreIllusionist does nothing extra, so we
/// hide the count.
/// </summary>
public sealed class EncorePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // Only at the END of the player's own turn (the owner is among the side that just ended).
        if (!participants.Contains(base.Owner))
        {
            return;
        }

        Player? player = base.Owner.Player;
        if (player == null)
        {
            return;
        }

        CardPile hand = PileType.Hand.GetPile(player);
        if (hand.Cards.Count >= CardPile.MaxCardsInHand)
        {
            return;
        }

        CardPile discard = PileType.Discard.GetPile(player);
        List<CardModel> retainCards = discard.Cards
            .Where(c => c.Keywords.Contains(CardKeyword.Retain))
            .ToList();
        if (retainCards.Count == 0)
        {
            return;
        }

        // Pick one at random via the seeded combat-card-selection RNG (deterministic for replays).
        CardModel? chosen = player.RunState.Rng.CombatCardSelection.NextItem(retainCards);
        if (chosen == null)
        {
            return;
        }

        // Relocate the chosen Retain card from discard back to hand (same Add that re-piles a card).
        await CardPileCmd.Add(chosen, PileType.Hand);
        Log.Info($"[illusionist] EncoreIllusionist: returned Retain card '{chosen.Id.Entry}' from discard to hand.");
    }
}
