using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 炫目 (Dazzle) power. At the start of each of your turns, gain Block equal to your current
/// number of mirror clones (复制品). The block is power-sourced (Unpowered), so it is exactly the
/// clone count, unmodified by Dexterity.
/// </summary>
public sealed class DazzlePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature)
        {
            return;
        }

        int block = MirrorClone.CountAlive(player);
        if (block > 0)
        {
            await CreatureCmd.GainBlock(base.Owner, block, ValueProp.Unpowered, null);
        }
    }
}
