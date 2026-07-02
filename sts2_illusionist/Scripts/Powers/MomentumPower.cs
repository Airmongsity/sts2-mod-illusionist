using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// Persistent Power applied by 势能 (Momentum). Instanced — playing the card twice
/// creates two independent instances. Every transform (via OnTransform) and every
/// exhaust (via AfterCardExhausted) counts toward a 0–9 counter; on the 10th tick
/// draws 1 + gains 1 energy and resets. Pattern follows the base-game Orbit power.
/// </summary>
[RegisterPower]
public sealed class MomentumPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override int DisplayAmount => GetInternalData<Data>().Counter;

    private sealed class Data
    {
        public int Counter;
    }

    protected override object InitInternalData() => new Data();

    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        Tick();
        return Task.CompletedTask;
    }

    internal Task OnTransform()
    {
        Tick();
        return Task.CompletedTask;
    }

    private async void Tick()
    {
        Data d = GetInternalData<Data>();
        d.Counter++;
        InvokeDisplayAmountChanged();

        if (d.Counter >= 10)
        {
            d.Counter = 0;
            var player = base.Owner.Player;
            if (player == null)
            {
                return;
            }
            Flash();
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1, player);
            await PlayerCmd.GainEnergy(1, player);
            InvokeDisplayAmountChanged();
        }
    }
}
