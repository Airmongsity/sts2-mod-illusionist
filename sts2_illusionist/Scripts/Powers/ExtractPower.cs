using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 萃取 (ExtractPower) — per-turn energy boost. Friendship pattern:
/// overrides ModifyMaxEnergy to increase max energy by Amount.
/// </summary>
[RegisterPower]
public sealed class ExtractPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner.Player) return amount;
        return amount + base.Amount;
    }
}
