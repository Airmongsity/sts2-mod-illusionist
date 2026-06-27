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
/// 炫目 (DazzleIllusionist) power. At the start of each of your turns, gain Block equal to your current number
/// of mirror clones (复制品) MULTIPLIED by this power's stacks (one per 炫目 played) — so 4 Dazzles with
/// 7 mirrors = 28 Block. Power-sourced (Unpowered), unmodified by Dexterity.
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

        // Each DazzleIllusionist (this power's stacks) independently grants Block = your mirror count.
        int block = MirrorClone.CountAlive(player) * base.Amount;
        if (block > 0)
        {
            await CreatureCmd.GainBlock(base.Owner, block, ValueProp.Unpowered, null);
        }
    }
}
