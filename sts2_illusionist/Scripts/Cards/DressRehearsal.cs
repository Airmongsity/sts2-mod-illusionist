using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 点睛之笔 (Finishing Touch) — 1-cost Rare Skill (upgraded: 0 cost).
/// Transmute every unupgraded, upgradable card remaining in hand into an upgraded clone of itself.
/// Already-upgraded and unupgradable cards are left unchanged.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "DRESS_REHEARSAL")]
public sealed class DressRehearsalIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        IllusionistKeywords.Transmute,
    };

    public DressRehearsalIllusionist()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> cards = PileType.Hand.GetPile(base.Owner).Cards
            .Where(card => !card.IsUpgraded && card.IsUpgradable)
            .ToList();

        await Transmutation.TransmuteCards(cards, this, choiceContext, original =>
        {
            CardModel upgraded = original.CreateClone();
            CardCmd.Upgrade(upgraded, CardPreviewStyle.None);
            return upgraded;
        });
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
