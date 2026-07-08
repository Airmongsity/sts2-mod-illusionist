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
/// 假想敌 (Imagined Foe) — 1 cost Power, Uncommon (upgraded: 4 -> 6). At the start of your turn,
/// ALL creatures (you, real allies like Osty, every enemy — mirror clones excluded) gain 4 Block.
/// The automated feed engine of the intent flow: your half is real defense, the enemies' half
/// fuels 恃盾者亡's tax and 拆穿's harvest — and the more enemies there are, the bigger the tax
/// base. (6/8-threshold made ~26 Strength by turn 4 with Shield Tax; slowed to 4/10.)
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "IMAGINED_FOE")]
public sealed class ImaginedFoeIllusionist : IllusionistCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<ImaginedFoePower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Block", 4m),
    };

    public ImaginedFoeIllusionist()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = (int)base.DynamicVars["Block"].BaseValue;
        await PowerCmd.Apply<ImaginedFoePower>(choiceContext, base.Owner.Creature, amount, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["Block"].UpgradeValueBy(2m); // 4 -> 6
    }
}
