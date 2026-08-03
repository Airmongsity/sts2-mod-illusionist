using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 逢场作戏 (Play Along) — 1-cost Uncommon Skill (upgraded: 0 cost).
/// Add an Extinguished Lamp to hand, then transmute every Extinguished Lamp in the draw, discard,
/// hand, mirror, and exhaust piles into an independently random Illusionist Skill if the target
/// intends to attack, or Attack otherwise.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "PLAY_ALONG")]
public sealed class PlayAlongIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        IllusionistKeywords.Transmute,
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<ExtinguishedLampIllusionist>(),
    };

    public PlayAlongIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        CardType resultType = cardPlay.Target.Monster?.IntendsToAttack == true
            ? CardType.Skill
            : CardType.Attack;

        List<CardModel> randomPool = base.Owner.Character.CardPool
            .GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.Type == resultType)
            .ToList();
        if (randomPool.Count == 0)
        {
            return;
        }

        CardModel lamp = base.CardScope!.CreateCard<ExtinguishedLampIllusionist>(base.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(lamp, PileType.Hand, base.Owner);

        PileType[] lampPiles =
        {
            PileType.Draw,
            PileType.Discard,
            PileType.Hand,
            MirrorPile.Type,
            PileType.Exhaust,
        };
        List<CardModel> lamps = lampPiles
            .SelectMany(pileType => pileType.GetPile(base.Owner).Cards.ToList())
            .Where(card => card is ExtinguishedLampIllusionist)
            .ToList();

        await Transmutation.TransmuteCards(lamps, this, choiceContext, _ =>
        {
            CardModel template = base.Owner.RunState.Rng.CombatCardSelection.NextItem(randomPool)
                ?? randomPool[0];
            return base.CardScope.CreateCard(template, base.Owner);
        });
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
