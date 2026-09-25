using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts;
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
/// 逆转 (Reversal) — 2 cost Uncommon Skill, Exhaust (upgraded: 1 cost).
/// If the target intends to attack, CHANGE that attack into intending to gain Block equal to the
/// damage it would have dealt. Only the damage is reversed: the attack icons come off the
/// telegraph, but the move's non-attack intents stay there AND still happen, because the enemy's
/// original action is what runs — with its damage silenced by <see cref="ReversedAttack"/>. The
/// enemy's later turns are untouched (its move sequence continues normally after).
///
/// <para>Running the original action is the whole point of the wrapper: a monster move is a single
/// delegate, so the Thieving Hopper's THIEVERY_MOVE steals a card and attacks in the same call.
/// Building a replacement action that only gains Block deleted the theft — and 64 other base-game
/// moves' non-attack halves — while the telegraph still advertised them.</para>
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "REVERSAL")]
public sealed class ReversalIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Exhaust,
        IllusionistKeywords.SevereCold,
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Block),
    }.Concat(SevereCold.FrozenReminderHoverTips(this));

    public ReversalIllusionist()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }

    public override Task BeforeCombatStart()
    {
        return SevereCold.ApplyAtCombatStart(this);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        if (target.Monster == null)
        {
            return;
        }

        MoveState move = target.Monster.NextMove;
        List<AttackIntent> attacks = move.Intents.OfType<AttackIntent>().ToList();
        if (attacks.Count == 0)
        {
            return;
        }

        bool changed = false;
        try
        {
            IReadOnlyList<Creature> me = new[] { base.Owner.Creature };
            int block = attacks.Sum(a => a.GetTotalDamage(me, target));

            // New telegraph: a Defend, plus this move's NON-attack intents (kept untouched).
            List<AbstractIntent> intents = new List<AbstractIntent> { new DefendIntent() };
            intents.AddRange(move.Intents.Where(i => i.IntentType != IntentType.Attack && i.IntentType != IntentType.DeathBlow));

            MoveState blockMove = new MoveState(
                "REVERSAL_DEFEND_ILLUSIONIST",
                async (IReadOnlyList<Creature> targets) =>
                {
                    if (block > 0)
                    {
                        await CreatureCmd.GainBlock(target, block, ValueProp.Unpowered, null);
                    }

                    // Then run the enemy's ORIGINAL action with its damage silenced. A monster move
                    // is one delegate: the Thieving Hopper steals AND attacks in the same call, so
                    // replacing the delegate with a block-gain silently deleted the theft while the
                    // telegraph kept promising it. Only the damage is reversed; everything else the
                    // move does still happens. (虚张声势 / 抢拍 already wrap moves this way.)
                    if (!ReversedAttack.IsLive)
                    {
                        // Suppression isn't in the hook chain — running the move now would deal full
                        // damage on top of the Block we just gave away. Fall back to cancelling it.
                        Log.Error("[illusionist] Reversal: damage suppression is not live; skipping the original move.");
                        return;
                    }

                    using (ReversedAttack.Suppress(target))
                    {
                        await move.PerformMove(targets);
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
            changed = true;
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Reversal: failed to convert attack to block: {ex}");
        }

        if (changed)
        {
            await IntentManipulation.NotifyChanged(choiceContext, base.Owner);
        }
    }
}
