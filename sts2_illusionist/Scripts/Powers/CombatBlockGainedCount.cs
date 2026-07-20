using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 积甲 (hidden) - counts EVERY point of Block gained by ANY creature this combat (player, allies,
/// enemies). Applied to the player at combat start (see <see cref="Illusionist.Scripts.Entry"/> -
/// CombatStartingEvent subscriber) so it is live from turn 1. Read by 积蓄 (Accrue) to scale its
/// damage: 13 + <see cref="Total"/>. Mirrors <see cref="CombatTransformCount"/> (hidden counter),
/// but for block-gain events - <see cref="AfterBlockGained"/> fires for all creatures, like Shield Tax.
/// </summary>
[RegisterPower]
public sealed class CombatBlockGainedCount : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override bool IsVisibleInternal => false;

    private sealed class Data
    {
        public int Total;
    }

    protected override object InitInternalData() => new Data();

    public int Total => GetInternalData<Data>().Total;

    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (amount > 0m)
        {
            GetInternalData<Data>().Total += (int)amount;
        }

        return Task.CompletedTask;
    }
}
