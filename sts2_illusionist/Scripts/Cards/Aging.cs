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
/// 老化 (AgingIllusionist) — 2 cost Attack, Uncommon, Retain (upgraded: 5 -> 8 HP loss).
/// The enemy loses 5 HP (unblockable). If its intent this turn consists ONLY of Attack and/or
/// Defend, advance it to next turn's intent, DISCARDING the current one (it performs what it would
/// have done next turn, and the current move is dropped — like ReversalIllusionist but without queuing the
/// original back).
///
/// Shares ReversalIllusionist's known limitation: reordering moves can desync enemies whose moves carry
/// order-dependent cross-turn state (e.g. Toadpole's Thorns). Accepted by design.
/// </summary>
public sealed class AgingIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    // Retain: advancing/affecting an enemy's intent is situational, so let the player hold it.
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HpLossVar(5m),
    };

    public AgingIllusionist()
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
            MoveState currentMove = target.Monster.NextMove;
            MoveState? nextMove = ResolveNextMove(target);

            // No distinct next move (e.g. a single-move monster whose move loops back to itself) —
            // leave the intent untouched.
            if (nextMove == null || ReferenceEquals(nextMove, currentMove))
            {
                Log.Info("[illusionist] AgingIllusionist: no distinct next move to advance to; no intent change.");
                return;
            }

            // Discard the current move: the enemy performs next turn's move now. We force the
            // transition (forceTransition) and resolve the next move via the state machine's follow-up
            // chain directly — NOT RollMove, which the engine refuses to advance on the monster's first
            // turn or for a freshly-rolled, not-yet-performed telegraph (that's why this used to do
            // nothing). Unlike ReversalIllusionist we don't queue the original back, so it's dropped.
            target.Monster.SetMoveImmediate(nextMove, forceTransition: true);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] AgingIllusionist failed to advance intent: {ex}");
        }
    }

    /// <summary>
    /// The next MOVE the enemy would transition to after its current one — walking the move state
    /// machine's follow-up chain (resolving random branches with the monster-AI rng) until a concrete
    /// move is reached, or null if it can't be resolved. Calls <see cref="MonsterState.GetNextState"/>
    /// exactly as the engine does, so the AI's branch/cooldown bookkeeping stays consistent; it just
    /// skips the transition guards that stop <c>RollMove</c> from advancing a not-yet-performed move.
    /// </summary>
    private static MoveState? ResolveNextMove(Creature monster)
    {
        if (monster.Monster == null)
        {
            return null;
        }

        MonsterMoveStateMachine? machine = monster.Monster.MoveStateMachine;
        if (machine == null)
        {
            return null;
        }

        var rng = monster.Monster.RunRng.MonsterAi;
        MonsterState state = monster.Monster.NextMove;
        for (int hops = 0; hops < 64; hops++)
        {
            string nextId = state.GetNextState(monster, rng);
            if (string.IsNullOrEmpty(nextId) || !machine.States.TryGetValue(nextId, out MonsterState? next))
            {
                return null;
            }

            state = next;
            if (state.IsMove)
            {
                return (MoveState)state;
            }
        }

        return null;
    }

    protected override void OnUpgrade()
    {
        // Retain is now innate, so the upgrade bumps the HP loss instead (shows 5 -> 8 via diff()).
        base.DynamicVars.HpLoss.UpgradeValueBy(3m);
    }
}
