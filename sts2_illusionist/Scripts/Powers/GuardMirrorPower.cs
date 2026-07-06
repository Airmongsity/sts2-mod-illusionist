using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 守镜 (Guard Mirror) — one of the three mirror types (see the 镜像 keyword). Each stack absorbs
/// <see cref="AbsorbPerGuard"/> of an incoming HP-loss instance and then shatters; a single hit chains
/// across as many Guards as it takes to soak it (each spent Guard shatters and removes one cosmetic
/// clone). A Guard spent on a small hit is still fully consumed — overkill absorption is wasted.
/// </summary>
[RegisterPower]
public sealed class GuardMirrorPower : MirrorTypePower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private const int AbsorbPerGuard = 4;

    // How many Guards to shatter after the current HP-loss instance resolves (accumulated across the
    // ModifyHpLost calls of one instance, spent in AfterModifyingHpLostAfterOsty).
    private int _pendingConsume;

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner || amount <= 0m || base.Amount <= 0m)
        {
            return amount;
        }

        int guards = (int)base.Amount;
        decimal capacity = AbsorbPerGuard * guards;
        decimal absorbed = Math.Min(amount, capacity);
        // Whole Guards are spent per hit; a Guard used for < AbsorbPerGuard damage is still fully spent.
        _pendingConsume += (int)Math.Ceiling(absorbed / AbsorbPerGuard);
        return amount - absorbed;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        if (_pendingConsume <= 0)
        {
            return;
        }

        int guards = (int)base.Amount;
        int consume = Math.Min(_pendingConsume, guards);
        _pendingConsume = 0;
        if (consume <= 0)
        {
            return;
        }

        Flash();
        for (int i = 0; i < consume; i++)
        {
            await MirrorClone.ShatterOneClone(base.Owner.Player);
        }

        if (consume >= guards)
        {
            await PowerCmd.Remove(this);
        }
        else
        {
            for (int i = 0; i < consume; i++)
            {
                await PowerCmd.Decrement(this);
            }
        }
    }
}
