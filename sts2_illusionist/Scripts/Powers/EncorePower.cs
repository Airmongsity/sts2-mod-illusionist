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

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 返场 (EncoreIllusionist) power. At the end of your turn, pull <see cref="PowerModel.Amount"/> random
/// [gold]Retain[/gold] cards out of your discard pile and back into your hand — one per stack, i.e. one
/// for each 返场 you've played this combat. Since you only play your held control cards (CounterIllusionist,
/// ForesightIllusionist, ReversalIllusionist, Catalyze) once they're worth it — and playing them sends them to discard —
/// this recurs the intent suite, and stacking 返场 recurs more of it per turn.
///
/// Stacks (<see cref="PowerStackType.Counter"/>): each EncoreIllusionist adds one to the per-turn return
/// count, shown on the power counter.
/// </summary>
[RegisterPower]
public sealed class EncorePower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

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

        // Return one random Retain card per stack (base.Amount): each 返场 played adds 1. Re-read the
        // piles each iteration since Add mutates hand/discard, and stop early if hand is full or the
        // discard runs out of Retain cards.
        for (int i = 0; i < base.Amount; i++)
        {
            CardPile hand = PileType.Hand.GetPile(player);
            if (hand.Cards.Count >= CardPile.MaxCardsInHand)
            {
                break;
            }

            CardPile discard = PileType.Discard.GetPile(player);
            List<CardModel> retainCards = discard.Cards
                .Where(c => c.Keywords.Contains(CardKeyword.Retain))
                .ToList();
            if (retainCards.Count == 0)
            {
                break;
            }

            // Pick one at random via the seeded combat-card-selection RNG (deterministic for replays).
            CardModel? chosen = player.RunState.Rng.CombatCardSelection.NextItem(retainCards);
            if (chosen == null)
            {
                break;
            }

            // Relocate the chosen Retain card from discard back to hand (same Add that re-piles a card).
            await CardPileCmd.Add(chosen, PileType.Hand);
            Log.Info($"[illusionist] EncoreIllusionist: returned Retain card '{chosen.Id.Entry}' from discard to hand.");
        }
    }
}
