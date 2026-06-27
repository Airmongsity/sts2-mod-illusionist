using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 重燃 (RekindleIllusionist) delayed-Copy power. A one-shot: at the start of the player's NEXT turn it does
/// Copy N (N = stacks, one per RekindleIllusionist played this turn) and then removes itself. Because the copy
/// lands at the start of next turn — AFTER any unblocked damage this turn would have shattered your
/// mirrors — it's a guaranteed rebuild from 0, the reliable restart button after a shatter.
/// </summary>
public sealed class RekindlePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature)
        {
            return;
        }

        int count = base.Amount;
        // Remove first so the copy can't re-trigger us, then rebuild.
        await PowerCmd.Remove(this);
        await MirrorClone.Copy(player, count, choiceContext);
    }
}
