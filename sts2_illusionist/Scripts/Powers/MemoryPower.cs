using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 记忆 (Memory) power. Whenever one of your mirror clones is destroyed (shattered by damage, or
/// consumed by 引爆/汲取), draw 2 cards and gain 1 energy. Self-contained via the AfterDeath hook,
/// which fires once per clone death.
/// </summary>
public sealed class MemoryPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        Player? player = base.Owner.Player;
        if (player == null)
        {
            return;
        }

        if (creature.Monster is MirrorClone && creature.PetOwner == player)
        {
            await CardPileCmd.Draw(choiceContext, 2, player);
            await PlayerCmd.GainEnergy(1, player);
        }
    }
}
