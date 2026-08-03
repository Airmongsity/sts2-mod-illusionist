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

using Illusionist.Scripts;
using Illusionist.Scripts.Afflictions;
using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 幻形 (TransmuteIllusionist) revert power — maintains, for each transmuted card, a STACK of its previous
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
[RegisterPower]
public sealed class TransmutePower : IllusionistPower
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

    // Whether this turn's start-of-turn revert already ran — 镜像 (MirrorImagePower) forces the revert
    // before its volley regardless of power-hook order, so both entry points share this guard.
    private bool _turnStartRevertDone;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == base.Owner)
        {
            _turnStartRevertDone = false;
        }

        return Task.CompletedTask;
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

        await EnsureTurnStartRevert(choiceContext);
    }

    /// <summary>
    /// Run this turn's start-of-turn one-layer revert exactly once, whoever asks first — the 镜像
    /// volley (which must fire AFTER the revert, so stored cards fire in their reverted form) calls
    /// this before firing; our own Late hook calls it too. 揭露 (Unveil) uses
    /// <see cref="RevertAllExcept"/> for its all-pile mid-turn unwind while excluding itself.
    /// </summary>
    internal async Task EnsureTurnStartRevert(PlayerChoiceContext choiceContext)
    {
        if (_turnStartRevertDone)
        {
            return;
        }

        _turnStartRevertDone = true;

        await RevertOneLayer(choiceContext, isTurnStart: true, excludedCard: null);
    }

    /// <summary>
    /// Revert every transmuted card across all piles one layer except the exact card instance passed
    /// by the caller. This keeps a resolving 揭露 out of its own batch without excluding any other
    /// copy of that card.
    /// </summary>
    internal Task<int> RevertAllExcept(PlayerChoiceContext choiceContext, CardModel excludedCard)
    {
        return RevertOneLayer(choiceContext, isTurnStart: false, excludedCard);
    }

    /// <summary>
    /// Fully unwind the exact transmuted <paramref name="card"/> one layer at a time and return a
    /// snapshot of every form revealed, newest predecessor first. Used by 层层杀机: the caller waits
    /// for the chain to be stable before executing any Attack snapshots, so those plays cannot mutate
    /// the chain while it is being traversed.
    ///
    /// Each successful layer follows the normal mid-turn revert contract: transform the live card in
    /// its current pile, keep a mirror slot pointing at the replacement, and notify every 变化 payoff.
    /// Other chains are untouched.
    /// </summary>
    internal async Task<IReadOnlyList<CardModel>> RevertFully(
        PlayerChoiceContext choiceContext,
        CardModel card)
    {
        List<CardModel> revealedForms = new();
        Data data = GetInternalData<Data>();
        Chain? chain = data.Chains.FirstOrDefault(candidate => ReferenceEquals(candidate.Current, card));
        if (chain == null)
        {
            return revealedForms;
        }

        while (data.Chains.Contains(chain) && chain.Predecessors.Count > 0)
        {
            CardModel current = chain.Current;

            // Frozen pauses this chain without consuming or deleting any predecessor.
            if (Frozen.IsAppliedTo(current))
            {
                break;
            }

            // A live card normally has a pile. Preserve the same legacy mirror fallback used by the
            // regular revert path if an older stored card is temporarily pile-less.
            if (current.Pile == null)
            {
                Player? player = base.Owner.Player;
                int slot = player == null ? -1 : await MirrorImagePower.EjectForRevert(player, current);
                if (slot < 0)
                {
                    data.Chains.Remove(chain);
                    break;
                }
            }

            CardModel previous = chain.Predecessors[^1];
            if (!current.IsTransformable || !previous.IsTransformable)
            {
                data.Chains.Remove(chain);
                break;
            }

            chain.Predecessors.RemoveAt(chain.Predecessors.Count - 1);
            previous.HasBeenRemovedFromState = false;

            try
            {
                await CardCmd.Transform(current, previous);
            }
            catch (Exception ex)
            {
                Log.Error($"[illusionist] TransmuteIllusionist: focused full revert failed: {ex}");
                data.Chains.Remove(chain);
                break;
            }

            chain.Current = previous;
            await MirrorImagePower.OnCardTransformed(base.Owner.Player!, current, previous);

            // Snapshot BEFORE transform payoffs run. 即兴 may auto-play and move the live form, but
            // 层层杀机 must still remember exactly which form this layer revealed.
            revealedForms.Add(previous.CreateClone());
            await Transmutation.NotifyTransformed(base.Owner.Player!, choiceContext, previous);

            if (chain.Predecessors.Count == 0)
            {
                data.Chains.Remove(chain);
            }
        }

        if (data.Chains.Count == 0)
        {
            await PowerCmd.Remove(this);
        }

        Log.Info($"[illusionist] TransmuteIllusionist: fully reverted one chain through {revealedForms.Count} layer(s).");
        return revealedForms;
    }

    /// <summary>
    /// Revert eligible transmuted cards one layer toward their original form, in one atomic batch, and
    /// run each reverted card through the transmute-payoff choke point
    /// (<see cref="Transmutation.NotifyTransformed"/>). Turn start excludes nothing; 揭露 excludes
    /// only its exact resolving instance while still reverting cards in every pile.
    /// </summary>
    private async Task<int> RevertOneLayer(PlayerChoiceContext choiceContext, bool isTurnStart, CardModel? excludedCard)
    {
        Player? player = base.Owner.Player;
        if (player == null)
        {
            return 0;
        }

        Data data = GetInternalData<Data>();

        // Collect one-layer reverts, then transform them in ONE atomic, fully-awaited
        // batch (the base game's multi-card Transform): all originals are removed and all replacements
        // added together, with a single combined preview animation. Doing it per-card instead let pile
        // state drift between the awaited transforms, leaving some reverted cards out of the draw pile
        // (you'd draw fewer than your draw count next turn).
        List<CardTransformation> batch = new List<CardTransformation>();
        List<(Chain chain, CardModel replaced, CardModel previous)> pending = new List<(Chain, CardModel, CardModel)>();
        // Chains whose card was stored INSIDE a mirror: ejected into the exhaust pile for the batch
        // (standard transform animation; 即兴 can play them at notify time), recaptured afterwards.
        List<(Chain chain, int slot)> ejected = new List<(Chain, int)>();

        foreach (Chain chain in data.Chains.ToList())
        {
            if (chain.Predecessors.Count == 0)
            {
                data.Chains.Remove(chain);
                continue;
            }

            if (ReferenceEquals(chain.Current, excludedCard))
            {
                continue;
            }

            // Frozen pauses this chain without consuming or deleting any predecessor.
            if (Frozen.IsAppliedTo(chain.Current))
            {
                continue;
            }

            // Only drop the chain if the card was truly REMOVED from combat (no pile at all) or
            // there's nothing left to unwind. A card in the exhaust pile still has a (non-null) pile,
            // so it keeps reverting — exactly the 彼岸咆哮 case. A card stored in a Mirror Pile also
            // has a non-null pile, so CardCmd.Transform replaces it in-place: the reverted form lands
            // in the Mirror Pile at the same slot, and the fly-back animation targets the Mirror Pile
            // button via RitsuLib's GetTargetPosition patch. No eject/recapture needed.
            // A truly pile-less card (removed from combat) cannot be transformed (Transform requires
            // a non-null Pile) — eject it to the exhaust pile first, then recapture.
            if (chain.Current.Pile == null)
            {
                int slot = await MirrorImagePower.EjectForRevert(player, chain.Current);
                if (slot < 0)
                {
                    data.Chains.Remove(chain);
                    continue;
                }

                ejected.Add((chain, slot));
                // Fall through: the card is in the exhaust pile now and takes the normal revert path.
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
            pending.Add((chain, chain.Current, previous));
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

            foreach ((Chain chain, CardModel replaced, CardModel previous) in pending)
            {
                chain.Current = previous;
                // Turn-start only (NOT Unveil's mid-turn revert): strip any stale affliction (e.g.
                // Queen's ChainsOfBinding / Bound) from the reverted card. Afflictions clear from
                // cards-in-piles at side-turn-end, but the original sat in transmute limbo (held by this
                // power, not in any pile) so its affliction survived the cleanup - the reverted card
                // would otherwise re-enter play with an expired affliction. Mid-turn reverts (Unveil)
                // skip this: the affliction is still fresh this turn and must not be cleared early.
                if (isTurnStart && previous.Affliction != null)
                {
                    CardCmd.ClearAffliction(previous);
                }
                // A stored (in-mirror) card that just reverted must keep its mirror pointing at the
                // new (reverted) form — stored cards participate fully in 幻化回退.
                await MirrorImagePower.OnCardTransformed(player, replaced, previous);
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
            foreach ((Chain _, CardModel _, CardModel previous) in pending)
            {
                await Transmutation.NotifyTransformed(player, choiceContext, previous);
            }
        }

        // Let the center-screen transform preview finish BEFORE pulling the cards back into their
        // mirrors: NCardTransformVfx (and its shine stage) self-aborts the moment its end card
        // leaves its pile, so an instant recapture kills the animation. The full sequence is
        // ~3.2s (pop-in 0.75 + shine ~1.6 + holds 0.8) followed by the card flying INTO the
        // exhaust pile — which doubles as the "then it's consumed" visual before the silent
        // recapture. Only pay the wait when a mirror card actually reverted.
        if (ejected.Count > 0)
        {
            await Cmd.Wait(3.5f);
        }

        // Pull each ejected card back into its mirror at its old stack slot. chain.Current is the
        // reverted form by now (or the old form, if its transform was dropped). If 即兴 played the
        // card away — or its own exhaust already re-stored it — recapture no-ops and the mirror
        // simply stays empty.
        foreach ((Chain chain, int slot) in ejected)
        {
            await MirrorImagePower.RecaptureAfterRevert(player, chain.Current, slot);
        }

        Log.Info($"[illusionist] TransmuteIllusionist: reverted {batch.Count} card(s) one layer ({ejected.Count} via mirrors); {data.Chains.Count} chain(s) remain.");

        // Nothing left to unwind — remove the power so it doesn't linger as an empty status.
        if (data.Chains.Count == 0)
        {
            await PowerCmd.Remove(this);
        }

        return batch.Count;
    }
}
