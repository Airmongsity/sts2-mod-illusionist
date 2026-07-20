using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 凝固时间 (SolidifyTime) power - a per-stack freeze on transmute reverts. At each of the owner's turn
/// starts this power spends ONE of its own stacks (strictly "next turn" - the freeze is consumed even when
/// there are no transmuted cards to skip, so it is never held back for a later revert) and signals
/// <see cref="TransmutePower"/> to skip that turn's one-layer revert, so transmuted cards hold their form
/// an extra turn. N stacks = N turns of no revert (e.g. two 凝固时间 plays = the next two turns skip).
/// <para>Self-removal in a turn-start hook follows the 重燃 (<see cref="RekindlePower"/>) pattern - safe
/// here, unlike damage hooks (no re-entrant power-list mutation).</para>
/// </summary>
[RegisterPower]
public sealed class SolidifyTimePower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature)
        {
            return;
        }

        // Signal TransmutePower (if present) to skip this turn's revert BEFORE consuming the stack, so the
        // flag lands on TransmutePower (which persists through the turn) even when this power is removed.
        base.Owner.GetPower<TransmutePower>()?.RequestSkipNextRevert();

        // Spend one stack strictly at the start of THIS turn - "next turn" is next turn, so the freeze is
        // wasted if there were no transmuted cards to skip (no holding for a later revert). Decrement when
        // more stacks remain, otherwise remove - exactly one stack per turn start.
        if (base.Amount > 1m)
        {
            await PowerCmd.Decrement(this);
        }
        else
        {
            await PowerCmd.Remove(this);
        }
    }
}
