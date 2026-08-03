using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 幻象风暴: each stack grants one exhaust-triggered mirror volley per turn. The first stack responds
/// to the first exhaust, the second to the second exhaust, and so on. Mirror-fired cards never count,
/// preventing the volley from recursively triggering itself.
/// </summary>
[RegisterPower]
public sealed class FantasyStormPower : IllusionistPower
{
    private sealed class Data
    {
        public int ExhaustsThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomIconPath =>
        IllusionistArtPaths.PowerIcon(typeof(PhantasmStormPower));

    public override string? CustomBigIconPath =>
        IllusionistArtPaths.PowerIcon(typeof(PhantasmStormPower));

    protected override object InitInternalData() => new Data();

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player.Creature)
        {
            GetInternalData<Data>().ExhaustsThisTurn = 0;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        Player? player = base.Owner.Player;
        if (player == null || card.Owner != player)
        {
            return;
        }

        MirrorImagePower? mirror = base.Owner.GetPower<MirrorImagePower>();
        if (mirror?.IsExecuting(card) == true)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.ExhaustsThisTurn++;
        if (data.ExhaustsThisTurn > (int)base.Amount || mirror == null)
        {
            return;
        }

        Flash();
        await mirror.FireAllAfterPendingExhausts(choiceContext);
    }
}
