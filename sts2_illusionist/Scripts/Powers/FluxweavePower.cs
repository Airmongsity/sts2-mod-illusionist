using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 流变 (FluxweaveIllusionist) power — the 幻化 system's engine. For every 2 cards you transform (变化)
/// or transmute (幻化), you draw 1 card. The progress is advanced from
/// <see cref="Illusionist.Scripts.Transmutation.NotifyTransformed"/> (and from the turn-start revert
/// in <see cref="TransmutePower"/>) — every transform path runs through one of those choke points.
///
/// <para>Modeled on the base game's OrbitPower: each played 流变 is its OWN power instance
/// (<see cref="PowerInstanceType.Instanced"/>), so multiple copies show as SEPARATE status entries
/// and settle SEPARATELY, each keeping its own running card count. The status number
/// (<see cref="DisplayAmount"/>) shows which card of the current 2-card cycle that copy is on (0 → 1
/// → draw → 0). <see cref="PowerModel.Amount"/> is the per-trigger draw count (1).</para>
/// </summary>
[RegisterPower]
public sealed class FluxweavePower : IllusionistPower
{
    private const int CardsPerDraw = 2;

    private sealed class Data
    {
        // Cards this copy has counted toward its next draw (0..CardsPerDraw-1).
        public int TransformCount;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Each play is a distinct status entry that settles on its own, exactly like OrbitPower.
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // Show "which card of the current cycle you're on": 0 when fresh, 1 after one transform, back to
    // 0 right after the draw. (Amount is the draw count, not the progress, so it isn't shown here.)
    public override int DisplayAmount => GetInternalData<Data>().TransformCount;

    protected override object InitInternalData()
    {
        return new Data();
    }

    /// <summary>
    /// Advance THIS copy by one transform. When it reaches 2 it draws <see cref="PowerModel.Amount"/>
    /// card(s) and resets. Called once per card transformed/transmuted, on every live instance.
    /// </summary>
    public async Task OnTransform(PlayerChoiceContext choiceContext)
    {
        Player? player = base.Owner.Player;
        if (player == null)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.TransformCount++;
        if (data.TransformCount >= CardsPerDraw)
        {
            data.TransformCount = 0;
            Flash();
            await CardPileCmd.Draw(choiceContext, base.Amount, player);
        }

        InvokeDisplayAmountChanged();
    }
}
