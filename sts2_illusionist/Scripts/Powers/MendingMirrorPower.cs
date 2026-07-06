using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 愈镜 (Mending Mirror) — Uncommon mirror type. Whenever any mirror of yours shatters (a clone dies —
/// by damage, Guard use, or being consumed), heal 2 per stack. Fires once per shattered mirror via the
/// AfterDeath hook, so a mass shatter (引爆/汲取) heals for each one.
/// </summary>
[RegisterPower]
public sealed class MendingMirrorPower : MirrorTypePower
{
    private const int HealPerStack = 2;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        Player? player = base.Owner.Player;
        if (player == null || base.Amount <= 0m)
        {
            return;
        }

        if (creature.Monster is MirrorClone && creature.PetOwner == player)
        {
            await CreatureCmd.Heal(base.Owner, HealPerStack * (int)base.Amount, true);
        }
    }
}
