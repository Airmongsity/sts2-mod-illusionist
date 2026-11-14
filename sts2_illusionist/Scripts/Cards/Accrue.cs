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
/// 积蓄 (Accrue) - 2 cost Exhaust Attack, Uncommon. Deal 18 damage, plus 1 more for every
/// 2 points of Block all enemies have gained this combat (tracked by the hidden
/// <see cref="CombatBlockGainedCount"/>, applied at combat start). The 养龟 line's scaling finisher:
/// 假想敌 (Imagined Foe) feeds every creature Block every turn, and Accrue turns that accumulated
/// Block into a single growing hit. The card face shows 18 + (enemy Block gained / 2) (Body Slam-style
/// <see cref="ModCardVars.ComputedDamage"/>) so the player never has to mental-math the bonus.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "ACCRUE")]
public sealed class AccrueIllusionist : IllusionistCard
{
    private const decimal BaseDamage = 18m;

    /// <summary>Block gained by all enemies this combat required for +1 damage.</summary>
    private const int BlockPerBonus = 2;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        // Displayed damage = BaseDamage + (cumulative enemy Block gained / BlockPerBonus), live (Body Slam-style).
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
        base.DynamicVars["Damage"].UpgradeValueBy(6m); // 18 -> 24
    }

    /// <summary>Total damage: printed base + (cumulative Block gained by all enemies this combat) / BlockPerBonus.</summary>
    private static decimal AccrueDamage(CardModel? card)
    {
        decimal baseDamage = card?.DynamicVars["Damage"].BaseValue ?? BaseDamage;
        int enemyTotal = card?.Owner?.Creature?.GetPower<CombatBlockGainedCount>()?.EnemyTotal ?? 0;
        return baseDamage + enemyTotal / BlockPerBonus;
    }
}
