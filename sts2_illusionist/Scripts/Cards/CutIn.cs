using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts;
using Illusionist.Scripts.Intents;
using Illusionist.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 抢拍 (Cut In) — 1 cost Uncommon Skill, Exhaust.
/// Reduce a multi-attack intent by one hit this turn; otherwise apply 1 Weak.
/// Upgraded: no longer Exhausts.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "CUT_IN")]
public sealed class CutInIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Exhaust,
        IllusionistKeywords.SevereCold,
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CutInPower>(),
        HoverTipFactory.FromPower<WeakPower>(),
    }.Concat(SevereCold.FrozenReminderHoverTips(this));

    public CutInIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int attackCount = cardPlay.Target.Monster?.NextMove.Intents
            .OfType<AttackIntent>()
            .Sum(intent => intent.Repeats) ?? 0;

        if (attackCount >= 2)
        {
            Log.Info(
                $"[illusionist] CutIn: applying one-hit reduction to "
                + $"{cardPlay.Target.Monster?.Id.Entry ?? "unknown"}; displayed attacks={attackCount}.");

            CutInPower? existing = cardPlay.Target.GetPower<CutInPower>();
            int previousAmount = existing?.Amount ?? 0;
            await PowerCmd.Apply<CutInPower>(
                choiceContext,
                cardPlay.Target,
                1m,
                base.Owner.Creature,
                this);

            CutInPower? applied = cardPlay.Target.GetPower<CutInPower>();
            if (applied == null || applied.Amount <= previousAmount)
            {
                Log.Info("[illusionist] CutIn: hit reduction was blocked; visible intent left unchanged.");
                return;
            }

            int expectedAmount = previousAmount + 1;
            if (applied.Amount != expectedAmount)
            {
                Log.Info(
                    $"[illusionist] CutIn: normalizing modified power amount "
                    + $"from {applied.Amount} to {expectedAmount}; each card grants exactly one reduction.");
                applied.SetAmount(expectedAmount);
            }

            bool wrapped = TryReduceVisibleIntent(cardPlay.Target);
            if (wrapped)
            {
                await IntentManipulation.NotifyChanged(choiceContext, base.Owner);
            }
        }
        else
        {
            Log.Info(
                $"[illusionist] CutIn: applying Weak to "
                + $"{cardPlay.Target.Monster?.Id.Entry ?? "unknown"}; displayed attacks={attackCount}.");
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                cardPlay.Target,
                1m,
                base.Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }

    public override Task BeforeCombatStart()
    {
        return SevereCold.ApplyAtCombatStart(this);
    }

    private static bool TryReduceVisibleIntent(Creature target)
    {
        if (target.Monster == null)
        {
            return false;
        }

        try
        {
            MoveState move = target.Monster.NextMove;
            List<AbstractIntent> intents = new();
            int reductionsRemaining = 1;

            foreach (AbstractIntent intent in move.Intents)
            {
                if (reductionsRemaining <= 0 || intent is not AttackIntent attack)
                {
                    intents.Add(intent);
                    continue;
                }

                int repeats = Math.Max(0, attack.Repeats);
                if (repeats == 0)
                {
                    intents.Add(intent);
                    continue;
                }

                int reduction = Math.Min(reductionsRemaining, repeats);
                reductionsRemaining -= reduction;
                if (repeats > reduction)
                {
                    intents.Add(new CutInAttackIntent(attack, reduction));
                }
            }

            if (reductionsRemaining != 0)
            {
                Log.Error("[illusionist] CutIn: could not find an Attack repeat to remove from the visible intent.");
                return false;
            }

            MoveState wrapped = new(
                "CUT_IN_ILLUSIONIST",
                async targets => await move.PerformMove(targets),
                intents.ToArray())
            {
                FollowUpStateId = move.FollowUpStateId,
            };
            wrapped.FollowUpState = move.FollowUpState;

            target.Monster.SetMoveImmediate(wrapped, forceTransition: true);
            Log.Info(
                $"[illusionist] CutIn: visible intent reduced for "
                + $"{target.Monster.Id.Entry}; attacks now="
                + $"{intents.OfType<AttackIntent>().Sum(intent => intent.Repeats)}.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] CutIn: failed to reduce visible intent: {ex}");
            return false;
        }
    }
}
