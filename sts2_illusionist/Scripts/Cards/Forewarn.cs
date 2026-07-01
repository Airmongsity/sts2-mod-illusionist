using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 预警 (ForewarnIllusionist) — 2 cost Skill, Rare (upgraded: Innate).
/// Target an enemy and put a 先见 (Prescience) into your hand whose [gold]Block[/gold]
/// equals the target's current attack-intent damage. When upgraded, +10 extra Block.
/// </summary>
public sealed class ForewarnIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<PrescienceIllusionist>(),
    };

    public ForewarnIllusionist()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int block = GetIncomingAttackDamage(cardPlay.Target);
        if (block <= 0) return;

        if (base.IsUpgraded) block += 10;

        CardModel card = base.CardScope!.CreateCard<PrescienceIllusionist>(base.Owner);
        card.DynamicVars.Block.BaseValue = block;

        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, base.Owner);
        CardCmd.PreviewCardPileAdd(result, 1.8f);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }

    private int GetIncomingAttackDamage(Creature target)
    {
        if (target.Monster == null) return 0;

        IReadOnlyList<Creature> playerTargets = new[] { base.Owner.Creature };
        int total = 0;
        foreach (AbstractIntent intent in target.Monster.NextMove.Intents)
        {
            if (intent is AttackIntent attack)
                total += attack.GetTotalDamage(playerTargets, target);
        }
        return total;
    }
}
