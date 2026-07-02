using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 拟形之盾 (Mirror Ward) — 1 cost Skill, Common (upgraded: 7 -> 10 Block).
/// Gain 7 Block, then 幻化 a card in your hand into a COPY of this Ward until end of turn (carrying
/// this card's upgrades/enchantments/temporary effects). The defense backbone of the 幻化 system:
/// turn a dead card into more Block, and with FluxweaveIllusionist draw a card for the reshape.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "MIRROR_WARD_ILLUSIONIST")]
public sealed class MirrorWardIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(7m, ValueProp.Move),
    };

    public MirrorWardIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 幻化 a hand card into a copy of THIS Ward (preserving its upgrade/enchant state) this turn.
        await Transmutation.TransmuteToCopyOf(this, choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
    }
}
