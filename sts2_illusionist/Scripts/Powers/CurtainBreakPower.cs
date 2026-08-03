using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// Uses the same block-break ownership test as the base game's Hand Drill.
/// Each instance draws 2 cards and grants 1 energy whenever the owner or one
/// of the owner's pets breaks an enemy's Block.
/// </summary>
[RegisterPower]
public sealed class CurtainBreakPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override async Task AfterBlockBroken(
        PlayerChoiceContext choiceContext,
        Creature target,
        Creature? breaker)
    {
        Player? player = base.Owner.Player;
        if (player == null)
        {
            return;
        }

        if ((breaker == base.Owner || breaker?.PetOwner == player) && !target.IsPlayer)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, 2, player);
            await PlayerCmd.GainEnergy(1, player);
        }
    }
}
