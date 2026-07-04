using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 镜厅 (HallOfMirrorsIllusionist) — 1 cost Skill, Rare. Exhaust your whole hand; [gold]Copy[/gold] 1 for
/// every 2 cards exhausted — turn the cards you were holding into a mirror army. Upgraded: if you
/// exhausted 9 cards (a full hand: this + 9 others), gain 1 Intangible (无实体 / see Wraith Form),
/// vanishing behind the reflections for a turn.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "HALL_OF_MIRRORS")]
public sealed class HallOfMirrorsIllusionist : IllusionistCard
{
    // A full hand is 10 cards; playing this leaves at most 9 others to exhaust.
    private const int IntangibleThreshold = 9;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Copy, IllusionistKeywords.MirrorImage };

    // The Intangible payoff only exists on the upgraded card, so only then does it need a hover tip.
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => base.IsUpgraded
        ? new IHoverTip[] { HoverTipFactory.FromPower<IntangiblePower>() }
        : Array.Empty<IHoverTip>();

    public HallOfMirrorsIllusionist()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;

        // Snapshot the hand (this card already left it on play), then exhaust every card in it.
        // CardCmd.Exhaust fires the exhaust hooks so exhaust-synergy still triggers.
        List<CardModel> handCards = PileType.Hand.GetPile(owner).Cards.ToList();
        foreach (CardModel card in handCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        int exhausted = handCards.Count;
        int copies = exhausted / 2;
        if (copies > 0)
        {
            // Copy 1 for every 2 cards exhausted — a mirror image for each pair you gave up.
            await MirrorClone.Copy(owner, copies, choiceContext);
        }

        // Upgraded payoff: dumping a full hand (9 others) makes you Intangible for a turn.
        if (base.IsUpgraded && exhausted >= IntangibleThreshold)
        {
            await PowerCmd.Apply<IntangiblePower>(choiceContext, owner.Creature, 1, owner.Creature, this);
        }
    }
}
