using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 恃盾者亡 (Shield Tax) - 1 cost Power, Uncommon (upgraded: 0 cost). Whenever a
/// creature gains Block, every {Threshold} points grant you 1 Strength. The intent flow's scaling
/// spine: rides the 虚张声势/逆转 feed line, turns naturally block-happy enemies into a tax base,
/// and closes the loop with 拆穿 (Strength raises the crack's base damage; the crack harvests the
/// very Block that paid the tax).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SHIELD_TAX")]
public sealed class ShieldTaxIllusionist : IllusionistCard
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<ShieldTaxPower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Threshold", 10m),
    };

    public ShieldTaxIllusionist()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int threshold = (int)base.DynamicVars["Threshold"].BaseValue;
        await PowerCmd.Apply<ShieldTaxPower>(choiceContext, base.Owner.Creature, threshold, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1); // 1 -> 0
    }
}
