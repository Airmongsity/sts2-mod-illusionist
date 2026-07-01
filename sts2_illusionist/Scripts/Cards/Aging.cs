using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 催化 (AgingIllusionist) — 1 cost Skill, Uncommon, Retain (upgraded: 0 cost).
/// Double your damage this turn (via DoubleDamagePower, same as Shadow Step).
/// If the target enemy's intent this turn consists ONLY of Attack and/or Defend,
/// advance it to next turn's intent, discarding the current one.
/// </summary>
public sealed class AgingIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Retain };

    public AgingIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
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

        try
        {
            MoveState currentMove = target.Monster.NextMove;
            MoveState? nextMove = ResolveNextMove(target);
            if (nextMove == null || ReferenceEquals(nextMove, currentMove))
            {
                Log.Info("[illusionist] Aging: no distinct next move; no intent change.");
                return;
            }

            target.Monster.SetMoveImmediate(nextMove, forceTransition: true);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Aging failed to advance intent: {ex}");
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }

    private static MoveState? ResolveNextMove(Creature monster)
    {
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
            if (state.IsMove) return (MoveState)state;
        }

        return null;
    }
}
