using System.Collections.Generic;
using System.Threading.Tasks;
using Illusionist.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 障眼法 (Misdirection) — 0-cost Uncommon Skill. Gain 4 Block, then gain 4 Block whenever one
/// of your cards changes this turn. Every transform notification counts, including each individual
/// layer of a transmutation revert. Upgraded: both values become 5.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "MISDIRECTION")]
public sealed class MisdirectionIllusionist : IllusionistCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(4m, ValueProp.Move),
        new BlockVar("BlockPerChange", 4m, ValueProp.Unpowered),
    };

    public MisdirectionIllusionist()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<MisdirectionPower>(
            choiceContext,
            base.Owner.Creature,
            base.DynamicVars["BlockPerChange"].BaseValue,
            base.Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(1m);
        base.DynamicVars["BlockPerChange"].UpgradeValueBy(1m);
    }
}
