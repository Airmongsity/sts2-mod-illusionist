using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 逆转 (Reversal) — 1 cost Skill. Upgraded: also applies 1 Weak to the target.
/// If the target's intent is NOT a buff (强化), swap its intent this turn with its
/// intent next turn: it now performs what it would have done next turn, and performs
/// its original move on the following turn.
///
/// KNOWN LIMITATION (accepted for MVP, by design decision 2026-06-22):
/// This effect reorders the monster's move sequence. A few enemies whose moves carry
/// cross-turn state by relying on a fixed move ORDER will glitch when reordered — e.g.
/// Toadpole's SPIKEN move grants +Thorns and its SPIKE_SPIT move removes that same
/// Thorns next turn, so reordering can leave Thorns lingering an extra turn or going
/// negative. This is inherent to ANY "swap/reorder intents" approach (including
/// pre-caching rolled intents), because the side effects live in the monster's own
/// move code, not in how the next move is chosen. We intentionally keep the swap and
/// treat these as rare cosmetic quirks rather than redesigning the card.
/// </summary>
public sealed class Reversal : CardModel
{
    // Belong to the Necrobinder pool explicitly, so CardModel.Pool never throws even if
    // pool registration (ModHelper.AddModelToPool) did not run.
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    // Retain: intent-manipulation is situational, so let the player hold it until it's worthwhile.
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Retain };

    // Only the upgraded version applies Weak, so only surface its tip when upgraded.
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        IsUpgraded ? new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>() } : Array.Empty<IHoverTip>();

    public Reversal()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        Creature target = cardPlay.Target;

        // Upgrade bonus: apply 1 Weak to the target.
        if (IsUpgraded)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, target, 1, base.Owner.Creature, this);
        }

        if (target.Monster == null)
        {
            return;
        }

        // Per design: only reverse intents that are NOT a buff (强化). Many enemies have
        // special strengthen moves, and reversing those was both off-theme and the source
        // of a self-reinforcing loop, so leave buff intents untouched.
        bool intendsToBuff = target.Monster.NextMove.Intents.Any(i => i.IntentType == IntentType.Buff);
        if (intendsToBuff)
        {
            return;
        }

        try
        {
            // The move the enemy currently intends to perform this turn.
            MoveState thisTurnMove = target.Monster.NextMove;

            // Force the enemy to decide what it would do next turn.
            IReadOnlyList<Creature> playerTargets = new[] { base.Owner.Creature };
            target.Monster.RollMove(playerTargets);
            MoveState nextTurnMove = target.Monster.NextMove;

            // If the move can't transition yet (e.g. a "must perform once" move that hasn't
            // been performed), RollMove returns the same move. Setting its FollowUpState to
            // itself would loop the move forever, so only swap when we got a distinct move.
            if (ReferenceEquals(nextTurnMove, thisTurnMove))
            {
                Log.Info("[illusionist] Reversal: enemy has no distinct next move to swap; no effect.");
                return;
            }

            // This turn the enemy now performs what it would have done next turn, and its
            // original move is queued to follow.
            nextTurnMove.FollowUpState = thisTurnMove;
            target.Monster.SetMoveImmediate(nextTurnMove, forceTransition: true);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Reversal failed to swap intents: {ex}");
        }
    }
}
