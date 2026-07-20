using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 凝固时间 (SolidifyTimeIllusionist) - 1 cost Skill, Rare (upgraded: 0 cost).
/// Apply 凝固时间: at the start of each of your next turns, one stack is consumed to SKIP that turn's
/// transmute revert, so [gold]幻化[/gold] cards hold their form an extra turn. Stacks across plays - two
/// plays = the next two turns skip. A Skill (not Exhaust): replayable, and the freeze buff itself is the
/// commitment, so multiple copies/draws stack more skipped turns.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SOLIDIFY_TIME")]
public sealed class SolidifyTimeIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Transmute };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<SolidifyTimePower>(),
    };

    public SolidifyTimeIllusionist()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Counter power: each play adds a stack; TransmutePower consumes one stack per turn start to
        // skip that turn's revert. N plays -> N turns of no revert.
        await PowerCmd.Apply<SolidifyTimePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
