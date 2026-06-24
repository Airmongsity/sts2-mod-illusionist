using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Illusionist.Scripts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 预判 (Foresight) — 3 cost Skill, Uncommon (upgraded: 2 cost). Targets an enemy.
/// 先机 (First Move): if this is the first card you play this turn, gain Block equal to the
/// selected enemy's current attack intent damage.
/// </summary>
public sealed class Foresight : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    public override bool GainsBlock => true;

    // Retain: value depends on the enemy's attack intent, so let the player hold it for the moment.
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Retain };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { IllusionHoverTips.FirstMove };

    public Foresight()
        : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 先机: only the first card played this turn grants the block.
        if (!FirstMove.IsActive(base.Owner.Creature))
        {
            return;
        }

        int block = GetIncomingAttackDamage(cardPlay.Target);
        if (block > 0)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, block, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }

    /// <summary>Total damage this enemy's current attack intent would deal (0 if not attacking).</summary>
    private int GetIncomingAttackDamage(Creature target)
    {
        if (target.Monster == null)
        {
            return 0;
        }

        IReadOnlyList<Creature> playerTargets = new[] { base.Owner.Creature };
        int total = 0;
        foreach (AbstractIntent intent in target.Monster.NextMove.Intents)
        {
            if (intent is AttackIntent attack)
            {
                total += attack.GetTotalDamage(playerTargets, target);
            }
        }
        return total;
    }
}
