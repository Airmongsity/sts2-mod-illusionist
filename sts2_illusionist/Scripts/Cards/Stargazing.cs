using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 观星 (StargazingIllusionist) - 1 cost Power, Rare (upgraded: Innate). Grants 观星: at the start of
/// each of your turns (after the draw), choose up to 3 cards in hand, put them on the bottom of your
/// draw pile, then draw that many - a per-turn mini-mulligan that cycles dead draws into fresh ones.
/// The Intent pillar's 控顶 (card-order control) ceiling, expressed as hand cycling (the effect lands
/// AFTER the draw to dodge the before-draw timing problem).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "STARGAZING")]
public sealed class StargazingIllusionist : IllusionistCard
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StargazingPower>(),
    };

    public StargazingIllusionist()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StargazingPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
