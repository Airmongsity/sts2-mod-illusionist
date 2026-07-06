using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 焰镜 (Ember Mirror) — Uncommon mirror type. At the end of your turn, deal 2 damage per stack to ALL
/// enemies (power-sourced, flat).
/// </summary>
[RegisterPower]
public sealed class EmberMirrorPower : MirrorTypePower
{
    private const int DamagePerStack = 2;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // Only at the END of the player's own turn (the owner is among the side that just ended).
        if (base.Amount <= 0m || !participants.Contains(base.Owner))
        {
            return;
        }

        ICombatState? combat = base.CombatState;
        if (combat == null)
        {
            return;
        }

        int dmg = DamagePerStack * (int)base.Amount;
        Flash();
        foreach (Creature enemy in combat.HittableEnemies.ToList())
        {
            if (enemy.IsAlive)
            {
                await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered | ValueProp.SkipHurtAnim, base.Owner, null, null);
            }
        }
    }
}
