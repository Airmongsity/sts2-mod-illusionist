using System;
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

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 变幻之刃 (Shifting Blade) — 1 cost Attack, Common (upgraded: 11 -> 15 damage).
/// Deal 11 damage, then 幻化 a card in your hand into a COPY of this Blade until end of turn (carrying
/// this card's upgrades/enchantments/temporary effects). The attack backbone of the 幻化 system: turn
/// a dead card into more damage, and with Fluxweave draw a card for the reshape.
/// </summary>
public sealed class ShiftingBlade : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Transmute,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(11m, ValueProp.Move),
    };

    public ShiftingBlade()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 幻化 a hand card into a copy of THIS Blade (preserving its upgrade/enchant state) this turn.
        await Transmutation.TransmuteToCopyOf(this, choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
