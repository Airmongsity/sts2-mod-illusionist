using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 棘镜 (Thorn Mirror) — one of the three mirror types. When an enemy attacks you, reflects
/// <see cref="DamagePerStack"/> damage per stack back to the attacker (the vanilla ThornsPower pattern).
/// </summary>
[RegisterPower]
public sealed class ThornMirrorPower : MirrorTypePower
{
    private const int DamagePerStack = 1;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (base.Amount <= 0m || target != base.Owner || dealer == null || !dealer.IsAlive || !props.IsPoweredAttack())
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(choiceContext, dealer, DamagePerStack * base.Amount, ValueProp.Unpowered | ValueProp.SkipHurtAnim, base.Owner, null, null);
    }
}
