using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 流变 (Fluxweave) power — the 幻化 system's engine. While active, every time you transform (变化)
/// or transmute (幻化) a card, you draw 1 card (the draw is fired from
/// <see cref="Illusionist.Scripts.Transmutation.NotifyTransformed"/>, the single choke point all
/// transform paths run through). Presence-based: a second copy doesn't stack the draw.
/// </summary>
public sealed class FluxweavePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
