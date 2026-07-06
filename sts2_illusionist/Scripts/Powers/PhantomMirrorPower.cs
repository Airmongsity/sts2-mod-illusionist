using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 虚镜 (Phantom Mirror) — Rare mirror type. Each stack fully prevents one instance of unblocked HP loss,
/// then shatters (one Phantom + one cosmetic clone spent per prevention). Unlike Guard it absorbs the
/// WHOLE instance regardless of size, but it is a one-shot: a board of N Phantoms negates the next N
/// unblocked hits and is gone.
/// </summary>
[RegisterPower]
public sealed class PhantomMirrorPower : MirrorTypePower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // How many Phantoms to shatter after the current HP-loss window resolves (accumulated across the
    // ModifyHpLost calls, spent in AfterModifyingHpLostAfterOsty).
    private int _pendingConsume;

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner || amount <= 0m || _pendingConsume >= (int)base.Amount)
        {
            return amount;
        }

        // Fully negate this unblocked instance; one Phantom will be spent for it once the osty resolves.
        _pendingConsume++;
        return 0m;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        if (_pendingConsume <= 0)
        {
            return;
        }

        int stacks = (int)base.Amount;
        int consume = Math.Min(_pendingConsume, stacks);
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

        if (consume >= stacks)
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
