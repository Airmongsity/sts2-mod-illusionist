using System.Collections.Generic;
using System.Threading.Tasks;
using Illusionist.Scripts.Monsters;
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
/// Mirror Realm Expansion - 1 cost Rare Power. Raise the mirror cap by 1, then Copy 1.
/// Upgraded: Innate.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "MIRROR_REALM_EXPANSION")]
public sealed class MirrorRealmExpansionIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { IllusionistKeywords.Copy, IllusionistKeywords.MirrorImage };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<MirrorCapUpPower>(),
    };

    public MirrorRealmExpansionIllusionist()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MirrorCapUpPower>(
            choiceContext,
            base.Owner.Creature,
            1,
            base.Owner.Creature,
            this);
        await MirrorClone.Copy(base.Owner, 1, choiceContext);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
