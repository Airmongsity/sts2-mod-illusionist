using System;
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
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 幻化 (TransmuteIllusionist) revert power — maintains, for each transmuted card, a STACK of its previous
/// forms, and pops ONE layer at the START of each of your turns (after the hand is drawn, before you
/// can play). A transmuted card therefore stays transmuted through the enemy's turn and unwinds one
/// step when your next turn begins — so a single 幻化 lasts until your next turn, and chained 幻化 on
/// the same card peel back one step per turn:
///
/// <para>Turn 1: 打击 → FlickerIllusionist → 防御 (stack under 防御 = [打击, FlickerIllusionist]). Start of turn 2: 防御 → FlickerIllusionist.
/// Start of turn 3: FlickerIllusionist → 打击. The card "remembers" every form and unwinds one per turn.</para>
///
/// Reverting at turn START (not end) avoids any race between the revert and the enemy acting — most
/// importantly, ImproviseIllusionist's auto-played card resolves fully and nothing reverts mid-enemy-turn.
///
/// Single-instance, one shared set of chains per combat. A transmuted card keeps unwinding wherever
/// it lives — hand, draw, discard, AND the EXHAUST pile (its <see cref="CardModel.Pile"/> is non-null
/// there). Only a card truly removed from combat (<see cref="CardModel.Pile"/> == null) drops its
/// chain. So transmuting e.g. 彼岸咆哮, then exhausting the new form, reverts it to 彼岸咆哮 in the
/// exhaust pile at the start of your next turn — and its "while in exhaust pile" effect keeps firing.
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

    /// <summary>
    /// The card <paramref name="card"/> will revert into at the end of this turn (the top of its
    /// chain's predecessor stack), or null if it isn't a transmuted card. Used to show players what a
    /// transmuted card reverts to.
    /// </summary>
    public CardModel? GetRevertTarget(CardModel card)
    {
        Data data = GetInternalData<Data>();
        Chain? chain = data.Chains.FirstOrDefault(ch => ch.Current == card);
        return (chain != null && chain.Predecessors.Count > 0) ? chain.Predecessors[^1] : null;
    }

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        // Revert at the START of the owner's turn — after the hand is drawn, before they can play.
        // Doing it here instead of at end-of-turn keeps transmuted cards intact through the enemy's
        // turn, so there's no race between reverting and the enemy acting — in particular,
        // ImproviseIllusionist's auto-played card resolves fully on the player's turn and nothing
        // reverts mid-enemy-turn.
        //
        // We use the LATE turn-start phase specifically so this runs AFTER every power's regular
        // AfterPlayerTurnStart — most importantly after ImproviseIllusionist has reset its
        // "first transmute this turn" flag. The revert is itself a transformation that must be able
        // to count as that first transmute (see the NotifyTransformed call below).
        if (player.Creature != base.Owner)
        {
            return;
        }

        Data data = GetInternalData<Data>();

        // Collect this turn's one-layer reverts, then transform them in ONE atomic, fully-awaited
        // batch (the base game's multi-card Transform): all originals are removed and all replacements
        // added together, with a single combined preview animation. Doing it per-card instead let pile
        // state drift between the awaited transforms, leaving some reverted cards out of the draw pile
        // (you'd draw fewer than your draw count next turn).
        List<CardTransformation> batch = new List<CardTransformation>();
        List<(Chain chain, CardModel previous)> pending = new List<(Chain, CardModel)>();

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
            if (!chain.Current.IsTransformable || !previous.IsTransformable)
            {
                data.Chains.Remove(chain);
                continue;
            }

            chain.Predecessors.RemoveAt(chain.Predecessors.Count - 1);

            // CRITICAL: revive the predecessor. When this card was first transmuted AWAY,
            // CardCmd.Transform finished by calling original.RemoveFromState() on it, which set
            // HasBeenRemovedFromState = true (the card is "gone"). Reverting re-adds that same
            // instance via Transform's AddInternal, which NEVER clears the flag. A re-added card
            // with the flag still set is a ghost: CardPileCmd.Add (and therefore Draw) silently
            // no-ops on it, so if it lands on top of the draw pile, drawing it fails and the whole
            // draw stalls. Clearing the flag here is exactly how the engine resurrects a card it
            // previously removed (see RunState.AddCard / ThievingHopper).
            previous.HasBeenRemovedFromState = false;

            batch.Add(new CardTransformation(chain.Current, previous));
            pending.Add((chain, previous));
        }

        if (batch.Count > 0)
        {
            try
            {
                // Many cards can revert in one turn (a hand-wide 千面, chained 幻化, etc.). The base
                // game's own rule (CardPileCmd.AddToCombatAndPreview, slimed_berserker's 10 Slimed)
                // is: <=5 cards fan out horizontally, more than that use the "messy" pile layout so
                // they don't run off the sides of the screen — the engine even logs a warning when a
                // horizontal preview exceeds five cards.
                CardPreviewStyle revertStyle = batch.Count > 5
                    ? CardPreviewStyle.MessyLayout
                    : CardPreviewStyle.HorizontalLayout;
                await CardCmd.Transform(batch, null, revertStyle);
            }
            catch (Exception ex)
            {
                Log.Error($"[illusionist] TransmuteIllusionist: batch revert failed: {ex}");
            }

            foreach ((Chain chain, CardModel previous) in pending)
            {
                chain.Current = previous;
                if (chain.Predecessors.Count == 0)
                {
                    data.Chains.Remove(chain);
                }
            }

            // The turn-start revert is itself a transformation, so each reverted card runs through
            // the same transmute-payoff choke point a forward 幻化 does: 流变 advances once per
            // reverted card ("two transforms is two transforms"), and 即兴 auto-plays the first
            // reverted card of the turn at a random enemy. Two transforms is two transforms — every
            // reverted card pings NotifyTransformed, in revert order.
            foreach ((Chain _, CardModel previous) in pending)
            {
                await Transmutation.NotifyTransformed(player, choiceContext, previous);
            }
        }

        Log.Info($"[illusionist] TransmuteIllusionist: reverted {batch.Count} card(s) one layer; {data.Chains.Count} chain(s) remain.");

        // Nothing left to unwind — remove the power so it doesn't linger as an empty status.
        if (data.Chains.Count == 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
