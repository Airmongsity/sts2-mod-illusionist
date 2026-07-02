using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 积蓄 (AccrueIllusionist) — 2 cost Attack, Uncommon. Deal 10 damage. Retain.
/// At the start of your turn, if this is in your hand, its damage grows by a flat Bonus (8; +2 when
/// upgraded). The growth ACCUMULATES turn over turn (10 -> 18 -> 26 -> ...) — the "accrue" fantasy:
/// a Retain attack that snowballs the longer you hold it. No 幻化 involved, just a flat add.
/// Upgraded: gains Innate, base damage 10 -> 14, and the per-turn Bonus 8 -> 10.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "ACCRUE")]
public sealed class AccrueIllusionist : IllusionistCard
{
    // Total flat damage added at runtime by the turn-start growth; restored if the card is downgraded
    // (a downgrade recomputes DynamicVars from canonical, which would otherwise drop the growth).
    private decimal _accrued;

    // Retain always; Innate is added only on upgrade (see OnUpgrade).
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Retain,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
        new DynamicVar("Bonus", 8m),
    };

    public AccrueIllusionist()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        decimal damage = base.DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return Task.CompletedTask;
        if (!IsInHand()) return Task.CompletedTask;

        decimal bonus = base.DynamicVars["Bonus"].BaseValue;
        _accrued += bonus;
        base.DynamicVars.Damage.BaseValue += bonus;
        CardCmd.Preview(this);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);   // 10 -> 14
        base.DynamicVars["Bonus"].UpgradeValueBy(2m); // 8 -> 10
        AddKeyword(CardKeyword.Innate);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        base.DynamicVars.Damage.BaseValue += _accrued;
    }

    private bool IsInHand()
    {
        return PileType.Hand.GetPile(base.Owner).Cards.Contains(this);
    }
}
