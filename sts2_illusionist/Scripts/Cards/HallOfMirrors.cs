using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 镜厅 (HallOfMirrorsIllusionist) — 1 cost Skill, Rare (upgraded: 0 cost). [gold]Copy[/gold] 1 for every 2
/// cards in your hand — the more you're holding, the bigger the mirror army you conjure.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "HALL_OF_MIRRORS")]
public sealed class HallOfMirrorsIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Copy, IllusionistKeywords.MirrorImage, IllusionistKeywords.Execute };

    public HallOfMirrorsIllusionist()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;

        // Copy 1 for every 2 cards remaining in your hand (this card has already left it on play).
        int copies = PileType.Hand.GetPile(owner).Cards.Count / 2;
        if (copies > 0)
        {
            await MirrorClone.Copy(owner, copies, choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
