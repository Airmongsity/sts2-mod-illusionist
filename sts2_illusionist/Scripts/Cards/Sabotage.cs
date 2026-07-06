using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 破坏 (SabotageIllusionist) — 1 cost Attack, Ancient. Orobas's reward, now a hand-hoarding burst:
/// deal 5 damage to a single enemy, plus one extra 5-damage hit for every card left in your hand
/// (this card is already in the Play pile, so it doesn't count itself). Then gain 15 Block and 幻化
/// (transmute) a card in your hand into a copy of this card, chaining another swing this turn.
/// Upgraded: 7 per hit / 23 Block. The old attack-intent bonus was dropped in this redesign.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SABOTAGE")]
public sealed class SabotageIllusionist : IllusionistCard
{

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Transmute };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(5m, ValueProp.Move),
        new BlockVar(15m, ValueProp.Move),
    };

    public SabotageIllusionist()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // Base hit, plus one extra hit of the same damage for each card left in your hand.
        int handCards = PileType.Hand.GetPile(base.Owner).Cards.Count;
        int hits = 1 + handCards;

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(hits)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // 幻化 a hand card into a copy of THIS card (preserving its upgrade state), this turn.
        await Transmutation.TransmuteToCopyOf(this, choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m); // 5 -> 7 per hit
        base.DynamicVars.Block.UpgradeValueBy(8m);  // 15 -> 23
    }
}
