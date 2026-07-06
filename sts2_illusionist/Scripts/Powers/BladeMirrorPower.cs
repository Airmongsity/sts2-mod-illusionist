using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 刃镜 (Blade Mirror) — one of the three mirror types. Whenever you play an Attack, deals
/// <see cref="DamagePerStack"/> damage per stack to that attack's target (or a random enemy for
/// untargeted AoE attacks).
/// </summary>
[RegisterPower]
public sealed class BladeMirrorPower : MirrorTypePower
{
    private const int DamagePerStack = 3;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (base.Amount <= 0m || cardPlay.Card.Owner.Creature != base.Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        ICombatState? combat = base.CombatState;
        if (combat == null)
        {
            return;
        }

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive)
        {
            target = combat.HittableEnemies.FirstOrDefault();
        }
        if (target == null || !target.IsAlive)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(choiceContext, target, DamagePerStack * base.Amount, ValueProp.Unpowered | ValueProp.SkipHurtAnim, base.Owner, null, null);
    }
}
