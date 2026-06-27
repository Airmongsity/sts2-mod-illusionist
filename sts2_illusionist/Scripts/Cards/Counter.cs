using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 抗衡 (CounterIllusionist) — 2 cost Attack, Rare, Exhaust + Ethereal (upgraded: loses Ethereal).
/// Deal damage equal to the target's current attack intent. Heavily gated: Exhaust makes it a
/// one-shot (no EncoreIllusionist loop), and Ethereal means you must play it the turn you draw it or it
/// vanishes. Upgrading removes Ethereal so you can hold it for the right (inflated) telegraph.
/// </summary>
public sealed class CounterIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    // Exhaust (one-shot, can't be recurred via discard) + Ethereal (use it this turn or lose it).
    // Upgrade strips Ethereal.
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust, CardKeyword.Ethereal };

    public CounterIllusionist()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        Creature target = cardPlay.Target;
        if (target.Monster != null)
        {
            IReadOnlyList<Creature> playerTargets = new[] { base.Owner.Creature };

            // Mirror the enemy's attack EXACTLY: same per-hit damage and same number of hits
            // (e.g. an intent of "1 damage x8" deals 1 damage 8 times, not 8 damage once).
            foreach (AbstractIntent intent in target.Monster.NextMove.Intents)
            {
                if (intent is not AttackIntent attack)
                {
                    continue;
                }

                int perHit = attack.GetSingleDamage(playerTargets, target);
                int hits = attack.Repeats;
                if (perHit <= 0 || hits <= 0)
                {
                    continue;
                }

                await DamageCmd.Attack(perHit).FromCard(this).Targeting(target)
                    .WithHitCount(hits)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
            }
        }
    }

    protected override void OnUpgrade()
    {
        // Lose Ethereal so the upgraded CounterIllusionist can be held across turns (Exhaust still applies).
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
