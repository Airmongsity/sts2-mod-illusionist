using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 老化 (Aging) — 2 cost Attack, Uncommon, Retain (upgraded: 5 -> 8 HP loss).
/// The enemy loses 5 HP (unblockable). If its intent this turn consists ONLY of Attack and/or
/// Defend, advance it to next turn's intent, DISCARDING the current one (it performs what it would
/// have done next turn, and the current move is dropped — like Reversal but without queuing the
/// original back).
///
/// Shares Reversal's known limitation: reordering moves can desync enemies whose moves carry
/// order-dependent cross-turn state (e.g. Toadpole's Thorns). Accepted by design.
/// </summary>
public sealed class Aging : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    // Retain: advancing/affecting an enemy's intent is situational, so let the player hold it.
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HpLossVar(5m),
    };

    public Aging()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        // 失去生命: unblockable, unpowered HP loss (same props as Bloodletting).
        await CreatureCmd.Damage(choiceContext, target, base.DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);

        if (target.Monster == null || !target.IsAlive)
        {
            return;
        }

        // Only advance if this turn's intent consists ONLY of Attack and/or Defend (no buff,
        // debuff, or anything else).
        IEnumerable<AbstractIntent> intents = target.Monster.NextMove.Intents;
        bool onlyAttackOrDefend = intents.Any()
            && intents.All(i => i.IntentType == IntentType.Attack || i.IntentType == IntentType.Defend);
        if (!onlyAttackOrDefend)
        {
            return;
        }

        try
        {
            MoveState thisTurnMove = target.Monster.NextMove;

            IReadOnlyList<Creature> playerTargets = new[] { base.Owner.Creature };
            target.Monster.RollMove(playerTargets);
            MoveState nextTurnMove = target.Monster.NextMove;

            // If there's no distinct next move (a "must perform once" move can't advance yet),
            // RollMove returns the same move — leave the intent untouched.
            if (ReferenceEquals(nextTurnMove, thisTurnMove))
            {
                Log.Info("[illusionist] Aging: no distinct next move to advance to; no intent change.");
                return;
            }

            // Discard the current move: the enemy performs next turn's move now. Unlike Reversal,
            // we do NOT set FollowUpState back to the original, so the current move is dropped.
            target.Monster.SetMoveImmediate(nextTurnMove, forceTransition: true);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Aging failed to advance intent: {ex}");
        }
    }

    protected override void OnUpgrade()
    {
        // Retain is now innate, so the upgrade bumps the HP loss instead (shows 5 -> 8 via diff()).
        base.DynamicVars.HpLoss.UpgradeValueBy(3m);
    }
}
