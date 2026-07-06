using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 渊镜 (Abyss Mirror) — Rare mirror type. At the start of your turn, Copy 1 per stack (spawn that many
/// new random mirrors). A self-replicating snowball; most copies roll Common, so it floods the board.
/// </summary>
[RegisterPower]
public sealed class AbyssMirrorPower : MirrorTypePower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature || base.Amount <= 0m)
        {
            return;
        }

        await MirrorClone.Copy(player, (int)base.Amount, choiceContext);
    }
}
