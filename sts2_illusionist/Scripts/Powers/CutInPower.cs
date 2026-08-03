using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// Each remaining stack removes one hit from the owner's next attack command this turn.
/// Multiple applications therefore reduce the enemy's total number of attacks once per stack.
/// </summary>
[RegisterPower]
public sealed class CutInPower : IllusionistPower
{
    private sealed class Data
    {
        public int Spent;
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    internal int Remaining => Math.Max(0, base.Amount - GetInternalData<Data>().Spent);

    public override int DisplayAmount => Remaining;

    protected override object InitInternalData()
    {
        return new Data();
    }

    internal int ConsumeHitCount(int hitCount)
    {
        if (hitCount <= 0)
        {
            return hitCount;
        }

        Data data = GetInternalData<Data>();
        int reduction = Math.Min(hitCount, Remaining);
        if (reduction <= 0)
        {
            return hitCount;
        }

        data.Spent += reduction;
        Flash();
        InvokeDisplayAmountChanged();
        Log.Info(
            $"[illusionist] CutInPower: reduced attack hit count for "
            + $"{base.Owner.Monster?.Id.Entry ?? "unknown"} from {hitCount} to {hitCount - reduction} "
            + "via native hit-count patch.");
        return hitCount - reduction;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.Remove(this);
        }
    }
}
