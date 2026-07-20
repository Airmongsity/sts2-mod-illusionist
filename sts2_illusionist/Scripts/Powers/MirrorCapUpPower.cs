using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 镜界 (MirrorCapUp) power - raises the <see cref="MirrorImagePower"/> cap. Each stack adds +1 to the
/// effective cap (base 8), so 复制 (Copy) can fill mirrors beyond 8. A dedicated buff rather than a field
/// on <see cref="MirrorImagePower"/> itself, so the bonus SURVIVES <see cref="MirrorImagePower"/> being
/// removed when every mirror dies - and self-resets at combat end like every combat power.
/// <see cref="MirrorImagePower.CreateOne"/> reads this power's <see cref="MegaCrit.Sts2.Core.Entities.Powers.PowerModel.Amount"/>
/// and adds it to <see cref="MirrorImagePower.Cap"/>. Counter: 镜界扩张 applies 2 stacks a play, so the
/// cap climbs 8 -> 10 -> 12 -> ... as the self-replicating chain is replayed.
/// </summary>
[RegisterPower]
public sealed class MirrorCapUpPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
