using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 镜涌 (Cascade) power. At the start of each of your turns, Copy mirrors equal to this power's stacks
/// (1 per 镜涌 played). Stacks add, so multiple copies spawn more mirrors per turn.
/// </summary>
[RegisterPower]
public sealed class CascadePower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature || base.Amount <= 0m)
        {
            return;
        }

        Flash();
        await MirrorClone.Copy(player, (int)base.Amount, choiceContext);
    }
}
