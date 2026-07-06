using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 折光 (Refraction) — persistent Power applied by <see cref="Illusionist.Scripts.Cards.RefractionIllusionist"/>.
/// Whenever you 变化 (transform) a card, a refracted bolt strikes a random enemy for <c>Amount</c> damage.
/// Dispatched from <see cref="Illusionist.Scripts.Transmutation.NotifyTransformed"/>, so it fires on EVERY
/// transform — and one 幻化 (transmute) is two transforms (the forward morph now, the revert next turn), so
/// a single transmute lands two bolts across the two turns. Playing another 折光 adds its damage (Counter).
/// </summary>
[RegisterPower]
public sealed class RefractionPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Called by <see cref="Illusionist.Scripts.Transmutation.NotifyTransformed"/> on each transform.</summary>
    internal async Task OnTransform(PlayerChoiceContext choiceContext)
    {
        if (base.Amount <= 0m)
        {
            return;
        }

        Player? player = base.Owner.Player;
        if (player == null)
        {
            return;
        }

        ICombatState? combat = base.CombatState;
        if (combat == null)
        {
            return;
        }

        List<Creature> enemies = combat.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
        {
            return;
        }

        Creature target = enemies[IllusionistRng.RunRng(player).NextInt(enemies.Count)];
        Flash();
        await CreatureCmd.Damage(choiceContext, target, (int)base.Amount, ValueProp.Unpowered | ValueProp.SkipHurtAnim, base.Owner, null, null);
    }
}
