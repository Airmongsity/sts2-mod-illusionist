using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 破坏 (SabotageIllusionist) — 1 cost Attack, Ancient. Orobas's reward — the strongest of the Illusionist's
/// ancient cards. Deal 20 damage, gain 15 Block, then 幻化 (transmute) a card in your hand into a
/// copy of this card (so you can chain another huge swing this turn). Upgraded: 27 damage / 22 Block.
/// </summary>
public sealed class SabotageIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(20m, ValueProp.Move),
        new BlockVar(15m, ValueProp.Move),
    };

    public SabotageIllusionist()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // 幻化 a hand card into a copy of THIS card (preserving its upgrade state), this turn.
        await Transmutation.TransmuteToCopyOf(this, choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(7m);
        base.DynamicVars.Block.UpgradeValueBy(7m);
    }
}
