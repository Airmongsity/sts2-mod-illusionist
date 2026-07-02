using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 逆转 (Reversal) — 2 cost Skill, Uncommon (upgraded: Retain).
/// If the target intends to attack, CHANGE that attack into intending to gain Block equal to the
/// damage it would have dealt — this turn's attack is discarded and replaced by a defend. Any
/// non-attack intents on the same move stay in the telegraph, and the enemy's later turns are
/// untouched (its move sequence continues normally after).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "REVERSAL_ILLUSIONIST")]
public sealed class ReversalIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public ReversalIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    /// <summary>
    /// If this card has Ethereal, replace it with Retain instead.
    /// Hook fires on every pile change + turn start to catch Ethereal no matter when it's applied.
    /// </summary>
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        ConvertEtherealToRetain();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        ConvertEtherealToRetain();
        return Task.CompletedTask;
    }

    private void ConvertEtherealToRetain()
    {
        if (Keywords.Contains(CardKeyword.Ethereal))
        {
            RemoveKeyword(CardKeyword.Ethereal);
            AddKeyword(CardKeyword.Retain);
        }
    }

    // Not async: the only await lives inside the MoveState lambda below (which runs when the enemy
    // later takes its turn), so OnPlay itself does no awaiting and returns a completed task.
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        if (target.Monster == null)
        {
            return Task.CompletedTask;
        }

        MoveState move = target.Monster.NextMove;
        List<AttackIntent> attacks = move.Intents.OfType<AttackIntent>().ToList();
        if (attacks.Count == 0)
        {
            return Task.CompletedTask;
        }

        try
        {
            IReadOnlyList<Creature> me = new[] { base.Owner.Creature };
            int block = attacks.Sum(a => a.GetTotalDamage(me, target));

            // New telegraph: a Defend, plus this move's NON-attack intents (kept untouched).
            List<AbstractIntent> intents = new List<AbstractIntent> { new DefendIntent() };
            intents.AddRange(move.Intents.Where(i => i.IntentType != IntentType.Attack && i.IntentType != IntentType.DeathBlow));

            MoveState blockMove = new MoveState(
                "REVERSAL_DEFEND_ILLUSIONIST",
                async (IReadOnlyList<Creature> _) =>
                {
                    if (block > 0)
                    {
                        await CreatureCmd.GainBlock(target, block, ValueProp.Unpowered, null);
                    }
                },
                intents.ToArray())
            {
                // Preserve the enemy's sequence exactly: after this defend it goes wherever the
                // attack would have led, so no cross-turn intent is changed.
                FollowUpStateId = move.FollowUpStateId,
            };
            blockMove.FollowUpState = move.FollowUpState;

            target.Monster.SetMoveImmediate(blockMove, forceTransition: true);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Reversal: failed to convert attack to block: {ex}");
        }

        return Task.CompletedTask;
    }
}
