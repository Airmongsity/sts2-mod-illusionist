using System;
using System.Collections.Generic;
using System.Threading;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts;

/// <summary>Visual-only context that follows the native async damage operation across awaits.
/// Nested damage gets its own scope; unrelated dealers never inherit Illusionist effects.</summary>
internal sealed class IllusionistDamageFxScope(Action<Creature> onImpact)
{
    internal static readonly AsyncLocal<IllusionistDamageFxScope?> Current = new();
    private readonly HashSet<Creature> _receivers = new();

    internal void Notify(Creature receiver, ValueProp props)
    {
        if (props.HasFlag(ValueProp.Move) && _receivers.Add(receiver)) onImpact(receiver);
    }
}
