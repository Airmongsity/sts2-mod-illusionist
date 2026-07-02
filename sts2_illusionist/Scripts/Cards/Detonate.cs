using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 引爆 (DetonateIllusionist) — 1 cost Attack, Uncommon.
/// Destroy all mirror clones; for each one destroyed, deal 12 damage to ALL enemies once.
/// Upgraded: 15 damage.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "DETONATE")]
public sealed class DetonateIllusionist : IllusionistCard
{

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[] { IllusionHoverTips.CopyToken };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(20m, ValueProp.Move),
    };

    public DetonateIllusionist()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Spend every mirror; the number destroyed scales the blast.
        int clones = await MirrorClone.ConsumeAll(base.Owner);
        if (clones <= 0)
        {
            return;
        }

        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat == null || combat.HittableEnemies.Count == 0)
        {
            return;
        }

        // One 12-damage hit to all enemies per destroyed clone.
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(combat)
            .WithHitCount(clones)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(8m);
    }
}
