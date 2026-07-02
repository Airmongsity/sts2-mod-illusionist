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
/// 积蓄 (AccrueIllusionist) — 1 cost Attack, Uncommon. Deal 3 damage. Retain.
/// At the start of your turn, if this is in your hand, 幻化 (Transmute) its damage to double it — the
/// doubling ACCUMULATES turn over turn (3 -> 6 -> 12 -> ...), which is the whole "accrue" fantasy.
/// The doubling is a REAL transmute: it pings Transmutation.NotifyTransformed, so it counts toward
/// the 幻化 payoffs (Fluxweave draw, Momentum, Improvise, Metamorphosis) like every other 幻化 card.
/// Upgraded: gains Innate, and base damage becomes 4 (+1).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "ACCRUE")]
public sealed class AccrueIllusionist : IllusionistCard
{
    private decimal _doubledAmount;


    // Retain always; Transmute (幻化) marks the self-doubling as a transmute; Innate is added only
    // on upgrade (see OnUpgrade).
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Retain,
        IllusionistKeywords.Transmute,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
    };

    public AccrueIllusionist()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
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

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (!IsInHand()) return;

        _doubledAmount += base.DynamicVars.Damage.BaseValue;
        base.DynamicVars.Damage.BaseValue *= 2;
        CardCmd.Preview(this);

        // The doubling IS a 幻化 (transform): route it through the shared transmute choke point so it
        // tallies toward transmute-count payoffs and triggers Fluxweave / Momentum / Improvise.
        await Transmutation.NotifyTransformed(base.Owner, choiceContext, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
        AddKeyword(CardKeyword.Innate);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        base.DynamicVars.Damage.BaseValue += _doubledAmount;
    }

    private bool IsInHand()
    {
        return PileType.Hand.GetPile(base.Owner).Cards.Contains(this);
    }
}
