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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 干扰 (Disrupt) — 1 cost Skill, Basic (starter). Targets an enemy.
/// Gain 6 Block and apply 1 Frail (脆弱: the enemy gains 25% less Block).
/// Upgraded: 8 Block / 2 Frail.
/// </summary>
public sealed class Disrupt : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    public override bool GainsBlock => true;

    // Block tip comes from GainsBlock; the Frail power needs its tip added explicitly (base-game Bash pattern).
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<FrailPower>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move),
        new PowerVar<FrailPower>(1m),
    };

    public Disrupt()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<FrailPower>(choiceContext, cardPlay.Target, base.DynamicVars["FrailPower"].BaseValue, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
        base.DynamicVars["FrailPower"].UpgradeValueBy(1m);
    }
}
