using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 催化 (AgingIllusionist) — 1 cost Skill, Rare (upgraded: 0 cost).
/// Double your damage this turn (via DoubleDamagePower, same as Shadow Step).
/// If the target enemy's intent this turn consists ONLY of Attack and/or Defend,
/// advance it to next turn's intent, discarding the current one.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "AGING")]
public sealed class AgingIllusionist : IllusionistCard
{
    public AgingIllusionist()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        await PowerCmd.Apply<DoubleDamagePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);

        if (target.Monster == null || !target.IsAlive) return;

        IEnumerable<AbstractIntent> intents = target.Monster.NextMove.Intents;
        bool onlyAttackOrDefend = intents.Any()
            && intents.All(i => i.IntentType == IntentType.Attack || i.IntentType == IntentType.Defend);
        if (!onlyAttackOrDefend) return;

        bool changed = false;
        try
        {
            MoveState? nextMove = ResolveNextMove(target, out MonsterState? loggedState);
            if (nextMove == null)
            {
                Log.Info("[illusionist] Aging: no next move; no intent change.");
                return;
            }

            target.Monster.SetMoveImmediate(nextMove, forceTransition: true);
            if (loggedState != null)
            {
                target.Monster.MoveStateMachine?.StateLog.Add(loggedState);
            }
            changed = true;
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Aging failed to advance intent: {ex}");
        }

        if (changed)
        {
            await IntentManipulation.NotifyChanged(choiceContext, base.Owner);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }

    private static MoveState? ResolveNextMove(Creature monster, out MonsterState? loggedState)
    {
        loggedState = null;
        if (monster.Monster == null) return null;

        MonsterMoveStateMachine? machine = monster.Monster.MoveStateMachine;
        if (machine == null) return null;

        var rng = monster.Monster.RunRng.MonsterAi;
        MonsterState state = monster.Monster.NextMove;
        for (int hops = 0; hops < 64; hops++)
        {
            string nextId = state.GetNextState(monster, rng);
            if (string.IsNullOrEmpty(nextId) || !machine.States.TryGetValue(nextId, out MonsterState? next))
                return null;

            state = next;
            if (loggedState == null && state.ShouldAppearInLogs)
            {
                loggedState = state;
            }

            if (state.IsMove) return (MoveState)state;
        }

        return null;
    }
}
