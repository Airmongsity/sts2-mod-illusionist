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
/// 谢幕 (CurtainCallIllusionist) — 0 cost Attack, Rare. Deal 40 damage to ALL enemies, but can
/// only be played while NO enemy's intent includes an attack — the intent-control payoff (à la Grand
/// Finale): pacify every attacker (Provoke / Blind / Counter / …) then take the stage. The gate is an
/// <see cref="IsPlayable"/> override → CanPlay reports UnplayableReason.BlockedByCardLogic, so the card
/// greys out until the board is safe.
/// Upgraded: 55 damage.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "CURTAIN_CALL")]
public sealed class CurtainCallIllusionist : IllusionistCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(40m, ValueProp.Move),
    };

    public CurtainCallIllusionist()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    // Grand-Finale gate: unplayable while any living enemy intends to attack. CardModel.CanPlay ORs in
    // UnplayableReason.BlockedByCardLogic whenever IsPlayable is false.
    protected override bool IsPlayable
    {
        get
        {
            ICombatState? combat = base.Owner?.Creature?.CombatState;
            if (combat == null)
            {
                return true;
            }

            return !combat.Enemies.Any(e => e.IsAlive && e.Monster != null && e.Monster.IntendsToAttack);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat == null || combat.HittableEnemies.Count == 0)
        {
            return;
        }

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .TargetingAllOpponents(combat)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(15m); // 40 -> 55
    }
}
