using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 假想敌 (Imagined Foe) — persistent Power applied by
/// <see cref="Illusionist.Scripts.Cards.ImaginedFoeIllusionist"/>. At the start of your turn,
/// EVERY living creature — you, allies (mirror clones, Osty), and all enemies — gains
/// <c>Amount</c> Block. Everyone braces against threats that aren't there: your half is real
/// defense, the enemies' half is a tax base for 恃盾者亡 and a harvest for 拆穿. Stacks additively
/// (Amount = block per turn), so upgraded and base copies mix correctly.
/// </summary>
[RegisterPower]
public sealed class ImaginedFoePower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature || base.Amount <= 0m)
        {
            return;
        }

        ICombatState? combat = base.Owner.CombatState;
        if (combat == null)
        {
            return;
        }

        // ALL creatures: the owner, every ally (mirror clones, Osty, other pets), every enemy.
        HashSet<Creature> creatures = new HashSet<Creature> { base.Owner };
        foreach (Creature ally in combat.Allies)
        {
            creatures.Add(ally);
        }
        foreach (Creature enemy in combat.HittableEnemies)
        {
            creatures.Add(enemy);
        }

        Flash();
        foreach (Creature creature in creatures.Where(c => c.IsAlive))
        {
            await CreatureCmd.GainBlock(creature, base.Amount, ValueProp.Unpowered, null);
        }
    }
}
