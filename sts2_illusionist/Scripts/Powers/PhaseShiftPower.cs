using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 虚实转换 (Phase Shift) guard. While the owner has at least one charge, the next instance of
/// unblocked damage will NOT shatter their mirror images — <see cref="MirrorImagePower"/> checks
/// for this in AfterDamageReceived and consumes one charge instead of shattering. Pure marker:
/// it has no hooks of its own; all the logic lives in MirrorImagePower.
/// </summary>
public sealed class PhaseShiftPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
