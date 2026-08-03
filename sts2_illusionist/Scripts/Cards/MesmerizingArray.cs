using System.Collections.Generic;
using System.Threading.Tasks;
using Illusionist.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// Mesmerizing Array - 1 cost Rare Power. The first successful player-driven structural change to
/// an enemy intent each turn Copies and draws once per stack. Upgraded: Innate.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "MESMERIZING_ARRAY")]
public sealed class MesmerizingArrayIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        IllusionistKeywords.Copy,
        IllusionistKeywords.MirrorImage,
        IllusionistKeywords.Execute,
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<MesmerizingArrayPower>(),
    };

    public MesmerizingArrayIllusionist()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MesmerizingArrayPower>(
            choiceContext,
            base.Owner.Creature,
            1,
            base.Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
