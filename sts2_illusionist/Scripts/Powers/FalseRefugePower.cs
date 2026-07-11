using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// Whenever an enemy gains Block, it loses HP equal to the Block gained per stack.
/// </summary>
[RegisterPower]
public sealed class FalseRefugePower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (amount <= 0m || base.Amount <= 0m)
        {
            return;
        }

        if (creature == base.Owner
            || base.Owner.CombatState == null || !base.Owner.CombatState.HittableEnemies.Contains(creature)
            || !creature.IsAlive)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            creature,
            amount * base.Amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim,
            base.Owner,
            null,
            null);
    }
}
