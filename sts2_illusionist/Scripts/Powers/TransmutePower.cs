using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 幻化 (Transmute) revert power — maintains, for each transmuted card, a STACK of its previous
/// forms, and pops ONE layer at the end of each of your turns. So a single 幻化 reverts at end of
/// turn (as before), but chained 幻化 on the same card peel back one step per turn:
///
/// <para>Turn 1: 打击 → Flicker → 防御 (stack under 防御 = [打击, Flicker]). End of turn 1: 防御 → Flicker.
/// End of turn 2: Flicker → 打击. The card "remembers" every form and unwinds one per turn.</para>
///
/// Single-instance, one shared set of chains per combat; only ticks at the end of the player's own
/// turn. A transmuted card keeps unwinding wherever it lives — hand, draw, discard, AND the EXHAUST
/// pile (its <see cref="CardModel.Pile"/> is non-null there). Only a card truly removed from combat
/// (<see cref="CardModel.Pile"/> == null) drops its chain. So transmuting e.g. 彼岸咆哮, then
/// exhausting the new form, reverts it to 彼岸咆哮 in the exhaust pile at end of turn — and its
/// "while in exhaust pile" effect fires next turn.
/// </summary>
public sealed class TransmutePower : PowerModel
{
    /// <summary>One transmuted card and the stack of forms it will revert through (last = newest).</summary>
    private sealed class Chain
    {
        public CardModel Current;
        public readonly List<CardModel> Predecessors = new();

        public Chain(CardModel current)
        {
            Current = current;
        }
    }

    private sealed class Data
    {
        public readonly List<Chain> Chains = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override object InitInternalData()
    {
        return new Data();
    }

    /// <summary>
    /// Record that <paramref name="from"/> (a card's current form) was transmuted into
    /// <paramref name="to"/>. Extends <paramref name="from"/>'s existing chain if it had one (so its
    /// earlier forms are preserved beneath), otherwise starts a new chain from the original card.
    /// </summary>
    public void RegisterTransmute(CardModel from, CardModel to)
    {
        Data data = GetInternalData<Data>();

        Chain? existing = data.Chains.FirstOrDefault(ch => ch.Current == from);
        if (existing != null)
        {
            existing.Predecessors.Add(from);
            existing.Current = to;
        }
        else
        {
            Chain chain = new Chain(to);
            chain.Predecessors.Add(from);
            data.Chains.Add(chain);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // Only at the end of the player's own turn (the owner is among the side that just ended).
        if (!participants.Contains(base.Owner))
        {
            return;
        }

        Data data = GetInternalData<Data>();
        int reverted = 0;

        foreach (Chain chain in data.Chains.ToList())
        {
            // Only drop the chain if the card was truly REMOVED from combat (no pile at all) or
            // there's nothing left to unwind. A card in the exhaust pile still has a (non-null) pile,
            // so it keeps reverting — exactly the 彼岸咆哮 case.
            if (chain.Current.Pile == null || chain.Predecessors.Count == 0)
            {
                data.Chains.Remove(chain);
                continue;
            }

            CardModel previous = chain.Predecessors[^1];
            chain.Predecessors.RemoveAt(chain.Predecessors.Count - 1);

            try
            {
                if (chain.Current.IsTransformable && previous.IsTransformable)
                {
                    await CardCmd.Transform(chain.Current, previous);
                    chain.Current = previous;
                    reverted++;
                }
                else
                {
                    data.Chains.Remove(chain);
                    continue;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[illusionist] Transmute: failed to revert one layer: {ex}");
                data.Chains.Remove(chain);
                continue;
            }

            // Fully unwound back to the original — chain is done.
            if (chain.Predecessors.Count == 0)
            {
                data.Chains.Remove(chain);
            }
        }

        Log.Info($"[illusionist] Transmute: reverted {reverted} card(s) one layer; {data.Chains.Count} chain(s) remain.");

        // Nothing left to unwind — remove the power so it doesn't linger as an empty status.
        if (data.Chains.Count == 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
