using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 裂镜 (Rupture Mirror) — Rare mirror type. Whenever any mirror of yours shatters, deal 8 damage per
/// stack to a random enemy. Weaponizes losing your mirrors (great with Guard / 引爆 Detonate / 献身
/// Sacrifice). Fires once per shattered mirror via the AfterDeath hook.
/// </summary>
[RegisterPower]
public sealed class RuptureMirrorPower : MirrorTypePower
{
    private const int DamagePerStack = 8;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        Player? player = base.Owner.Player;
        if (player == null || base.Amount <= 0m)
        {
            return;
        }

        if (!(creature.Monster is MirrorClone) || creature.PetOwner != player)
        {
            return;
        }

        ICombatState? combat = base.CombatState;
        if (combat == null)
        {
            return;
        }

        List<Creature> enemies = combat.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
        {
            return;
        }

        Creature target = enemies[MirrorRoster.RunRng(player).NextInt(enemies.Count)];
        Flash();
        await CreatureCmd.Damage(choiceContext, target, DamagePerStack * (int)base.Amount, ValueProp.Unpowered | ValueProp.SkipHurtAnim, base.Owner, null, null);
    }
}
