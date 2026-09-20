using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// A temporary, enemy-owned adaptation of Osty's Die For You power.
/// The player creature stored as <see cref="PowerModel.Applier"/> is the protected target;
/// its unblocked powered-attack damage is redirected to this power's owner.
/// </summary>
[RegisterPower]
public sealed class BodyDoublePower : IllusionistPower
{
    // Keep Osty's title so the card text ("gains Die for You") still names it. The description is our
    // own: the borrowed one credits Osty, who isn't here — on this enemy it's the enemy that absorbs.
    public override LocString Title => ModelDb.Power<DieForYouPower>().Title;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;

    public override bool ShouldPlayVfx => false;

    // This is intentionally the same visible status as Osty's native power.
    public override string? CustomIconPath => ModelDb.Power<DieForYouPower>().IconPath;

    public override string? CustomBigIconPath => ModelDb.Power<DieForYouPower>().ResolvedBigIconPath;

    public override Creature ModifyUnblockedDamageTarget(
        Creature target,
        decimal _,
        ValueProp props,
        Creature? __)
    {
        if (target != base.Applier || base.Owner.IsDead || !props.IsPoweredAttack())
        {
            return target;
        }

        return base.Owner;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.Remove(this);
        }
    }
}
