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
/// 积蓄 (AccrueIllusionist) — 0 cost Attack, Uncommon. Deal 2 damage. Retain, Exhaust.
/// At the start of your turn, if this is in your hand, its damage DOUBLES (2 -> 4 -> 8 -> 16 -> ...).
/// The "charge then fire" finisher: it costs nothing to play and Retains so it keeps doubling in hand,
/// then Exhausts when you finally unleash it for one big free hit.
/// Upgraded: gains Innate (drawn turn 1, so it starts doubling immediately).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "ACCRUE")]
public sealed class AccrueIllusionist : IllusionistCard
{
    // Total damage added at runtime by the turn-start doubling; restored if the card is downgraded
    // (a downgrade recomputes DynamicVars from canonical, which would otherwise drop the growth).
    private decimal _accrued;

    // Retain + Exhaust always; Innate is added only on upgrade (see OnUpgrade).
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Retain,
        CardKeyword.Exhaust,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(2m, ValueProp.Move),
    };

    public AccrueIllusionist()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        decimal damage = base.DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return Task.CompletedTask;
        if (!IsInHand()) return Task.CompletedTask;

        // Doubling adds "current value" again; track that so a downgrade can restore the growth.
        _accrued += base.DynamicVars.Damage.BaseValue;
        base.DynamicVars.Damage.BaseValue *= 2;
        CardCmd.Preview(this);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
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
