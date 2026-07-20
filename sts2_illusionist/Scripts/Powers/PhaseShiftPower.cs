using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 虚实转换 (PhaseShiftIllusionist) power - a per-stack mirror shield. The next time the owner takes
/// unblocked damage, <see cref="MirrorImagePower.AfterDamageReceived"/> consumes one stack here INSTEAD
/// of killing a mirror, so every mirror survives that hit. N stacks absorb N separate unblocked-damage
/// instances. This power owns no hook of its own; <see cref="MirrorImagePower"/> checks for and spends
/// it as the first responder to unblocked damage (before DieOne).
/// </summary>
[RegisterPower]
public sealed class PhaseShiftPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
