using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts.Afflictions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// Solidify Time - Freeze every eligible card in the hand, draw, discard, exhaust, and mirror piles
/// without separate preview animations. Exhausts; upgraded cost is 1.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SOLIDIFY_TIME")]
public sealed class SolidifyTimeIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromAffliction<Frozen>();

    public SolidifyTimeIllusionist()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PileType[] pileTypes =
        {
            PileType.Hand,
            PileType.Draw,
            PileType.Discard,
            PileType.Exhaust,
            MirrorPile.Type,
        };
        List<CardModel> cards = pileTypes
            .SelectMany(pileType => pileType.GetPile(base.Owner).Cards)
            .Distinct()
            .ToList();

        foreach (CardModel card in cards)
        {
            await CardCmd.Afflict<Frozen>(card, 1);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
