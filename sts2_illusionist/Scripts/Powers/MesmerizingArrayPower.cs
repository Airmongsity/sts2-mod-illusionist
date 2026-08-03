using System.Threading.Tasks;
using Illusionist.Scripts.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// Once per owner turn, rewards the first successful player-driven structural enemy-intent change.
/// Stacks increase both rewards without adding more triggers.
/// </summary>
[RegisterPower]
public sealed class MesmerizingArrayPower : IllusionistPower
{
    private sealed class Data
    {
        public bool TriggeredThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player.Creature)
        {
            GetInternalData<Data>().TriggeredThisTurn = false;
        }

        return Task.CompletedTask;
    }

    internal async Task OnIntentChanged(PlayerChoiceContext choiceContext, Player player)
    {
        Data data = GetInternalData<Data>();
        if (base.Owner != player.Creature || base.Amount <= 0 || data.TriggeredThisTurn)
        {
            return;
        }

        // Consume before awaiting either reward so nested effects cannot trigger this power twice.
        data.TriggeredThisTurn = true;
        Flash();

        int amount = base.Amount;
        await MirrorClone.Copy(player, amount, choiceContext);
        await CardPileCmd.Draw(choiceContext, amount, player);
    }
}
