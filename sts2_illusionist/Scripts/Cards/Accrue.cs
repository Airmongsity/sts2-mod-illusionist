using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Powers;
using STS2RitsuLib.Cards.DynamicVars;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 积蓄 (Accrue) - 2 cost Attack, Uncommon (upgraded: 2 -> 1). Deal 11 damage, plus 1 more for every
/// 5 points of Block ANY creature has gained this combat (tracked by the hidden
/// <see cref="CombatBlockGainedCount"/>, applied at combat start). The 养龟 line's scaling finisher:
/// 假想敌 (Imagined Foe) feeds every creature Block every turn, and Accrue turns that accumulated
/// Block into a single growing hit. The card face shows 11 + (total block gained / 5) (Body Slam-style
/// <see cref="ModCardVars.ComputedDamage"/>) so the player never has to mental-math the bonus.
/// Rate tuned down from 1:1 (too strong in playtest).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "ACCRUE")]
public sealed class AccrueIllusionist : IllusionistCard
{
    private const decimal BaseDamage = 11m;

    /// <summary>Block gained (by any creature, this combat) required for +1 damage. Tuned down from 1:1 (too strong).</summary>
    private const int BlockPerBonus = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        // Displayed damage = BaseDamage + (cumulative block-gained total / BlockPerBonus), live (Body Slam-style).
        ModCardVars.ComputedDamage("Damage", BaseDamage, card => AccrueDamage(card), ValueProp.Move),
    };

    public AccrueIllusionist()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // Same formula the card face shows (ComputedDamage).
        decimal damage = AccrueDamage(this);

        await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1); // 2 -> 1
    }

    /// <summary>Total damage: BaseDamage + (cumulative block gained by any creature this combat) / BlockPerBonus.</summary>
    private static decimal AccrueDamage(CardModel? card)
    {
        int total = card?.Owner?.Creature?.GetPower<CombatBlockGainedCount>()?.Total ?? 0;
        return BaseDamage + total / BlockPerBonus;
    }
}
