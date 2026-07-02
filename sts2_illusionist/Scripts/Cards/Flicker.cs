using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 闪烁 (FlickerIllusionist) — 1 cost Attack, Common.
/// Deal 7 damage to ALL enemies, then gain 6 Block for each enemy that intends to attack. Upgraded:
/// +2 to both (9 damage, 8 Block).
/// </summary>
public sealed class FlickerIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
        new BlockVar(5m, ValueProp.Move),
    };

    public FlickerIllusionist()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat == null || combat.HittableEnemies.Count == 0)
        {
            return;
        }

        // Snapshot how many enemies intend to attack BEFORE the hit, so one that dies to the blast
        // still counts the blow you braced against.
        int attackers = combat.Enemies.Count(e => e.IsAlive && e.Monster != null && e.Monster.IntendsToAttack);

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(combat)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // Gain Block per attacking enemy, in one event so Dexterity applies once to the total.
        if (attackers > 0)
        {
            BlockVar block = base.DynamicVars.Block;
            await CreatureCmd.GainBlock(base.Owner.Creature, block.BaseValue * attackers, block.Props, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
        base.DynamicVars.Block.UpgradeValueBy(1m);
    }
}
