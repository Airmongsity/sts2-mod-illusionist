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
/// Once per turn, each instance draws 1 card and grants 1 energy when the owner
/// or one of the owner's pets first breaks an enemy's Block.
/// </summary>
[RegisterPower]
public sealed class CurtainBreakPower : IllusionistPower
{
    private sealed class Data
    {
        public bool TriggeredThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData() => new Data();

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player.Creature)
        {
            GetInternalData<Data>().TriggeredThisTurn = false;
        }

        return Task.CompletedTask;
    }

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

        Data data = GetInternalData<Data>();
        if (!data.TriggeredThisTurn
            && (breaker == base.Owner || breaker?.PetOwner == player)
            && !target.IsPlayer)
        {
            // Consume before awaiting rewards so nested Block-break hooks cannot trigger twice.
            data.TriggeredThisTurn = true;
            Flash();
            await CardPileCmd.Draw(choiceContext, 1, player);
            await PlayerCmd.GainEnergy(1, player);
        }
    }
}
