using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 萃取 (ExtractPower) — per-turn energy boost. Friendship pattern:
/// overrides ModifyMaxEnergy to increase max energy by Amount.
/// </summary>
public sealed class ExtractPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner.Player) return amount;
        return amount + base.Amount;
    }
}
