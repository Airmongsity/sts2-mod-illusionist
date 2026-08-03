using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// Temporary Block payoff granted by 障眼法. Amount is Block per card change; multiple plays stack
/// additively. Transmutation.NotifyTransformed dispatches both forward transforms and every reverted
/// layer here. The effect expires when its owner participates in the end of its side's turn.
/// </summary>
[RegisterPower]
public sealed class MisdirectionPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    internal async Task OnCardChanged()
    {
        if (base.Amount <= 0m || !base.Owner.IsAlive)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
