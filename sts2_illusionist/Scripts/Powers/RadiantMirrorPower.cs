using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 辉镜 (Radiant Mirror) — Uncommon mirror type. At the start of your turn, gain 5 Block per stack.
/// </summary>
[RegisterPower]
public sealed class RadiantMirrorPower : MirrorTypePower
{
    private const int BlockPerStack = 5;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature || base.Amount <= 0m)
        {
            return;
        }

        int block = BlockPerStack * (int)base.Amount;
        if (block > 0)
        {
            await CreatureCmd.GainBlock(base.Owner, block, ValueProp.Unpowered, null);
        }
    }
}
