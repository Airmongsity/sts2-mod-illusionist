using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 谢幕 (CurtainCallIllusionist) - 1 cost Attack, Rare. Deal 6 damage to ALL enemies; if NO living enemy
/// intends to attack, the bonus is folded into the SAME damage instance (one hit of 30, not 6 then 24),
/// the intent-control payoff (pacify every attacker with Provoke / Blind / Counter / Reversal / ... then
/// take the stage). Always playable: the old Grand-Finale playability gate is gone, so the safe board is
/// REWARDED with a bonus rather than required.
/// Upgraded: the bonus goes 24 -> 39 (the base 6 is unchanged, so 6 or 45).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "CURTAIN_CALL")]
public sealed class CurtainCallIllusionist : IllusionistCard
{
    // Two DamageVars: the base AoE ("Damage") and the safe-board bonus. A second var of the same type
    // MUST be given an explicit name or DynamicVarSet throws on the duplicate "Damage" key. The bonus is
    // ADDED to the base in ONE hit (not a second damage instance).
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
        new DamageVar("BonusDamage", 24m, ValueProp.Move),
    };

    public CurtainCallIllusionist()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat == null || combat.HittableEnemies.Count == 0)
        {
            return;
        }

        // One damage instance: the base 6, PLUS the bonus (24 / 39 upgraded) folded into the SAME hit
        // when no living enemy intends to attack (so 6, or 30 / 45 upgraded), not a second separate hit.
        var damage = base.DynamicVars.Damage.BaseValue;
        if (!combat.Enemies.Any(e => e.IsAlive && e.Monster != null && e.Monster.IntendsToAttack))
        {
            damage += ((DamageVar)base.DynamicVars["BonusDamage"]).BaseValue;
        }

        await DamageCmd.Attack(damage).FromCard(this, cardPlay)
            .TargetingAllOpponents(combat)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        ((DamageVar)base.DynamicVars["BonusDamage"]).UpgradeValueBy(15m); // 24 -> 39
    }
}
