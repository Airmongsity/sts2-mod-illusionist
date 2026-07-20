using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 幻象风暴 (PhantasmStormIllusionist) power - the unstable storm sheds one mirror each turn. At the
/// start of your turn, destroy one mirror via <see cref="MirrorImagePower.ConsumeOne"/> (active
/// destruction: a loaded mirror fires its stored card first, then shatters; an empty mirror just breaks).
/// Runs in <see cref="AfterPlayerTurnStart"/>, before the turn-start volley, so the destroyed mirror is
/// gone before the remaining mirrors fire. This is the cost of the card's Copy 8 burst.
/// </summary>
[RegisterPower]
public sealed class PhantasmStormPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature)
        {
            return;
        }

        MirrorImagePower? mirror = base.Owner.GetPower<MirrorImagePower>();
        if (mirror == null)
        {
            return;
        }

        // Active destruction (fire-then-shatter if loaded). Runs before the volley (AfterPlayerTurnStartLate),
        // so this mirror is removed first and the volley fires only the survivors.
        await mirror.ConsumeOne(choiceContext);
    }
}
