using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 恃盾者亡 (Shield Tax) — persistent Power applied by
/// <see cref="Illusionist.Scripts.Cards.ShieldTaxIllusionist"/>. Whenever an ENEMY gains Block —
/// fed by 虚张声势/逆转, or its own turtling — every accumulated <c>Amount</c> points grant you
/// 1 Strength. The scaling spine of the intent flow's "fatten the turtle" line: block-happy
/// enemies become a tax base, and every feed card doubles as a ramp card.
///
/// Instanced (Momentum pattern): each play is its own tax collector with its own threshold
/// (<c>Amount</c> = block per Strength, so the UPGRADED card's 6-per is a separate, faster
/// instance) and its own progress counter shown on the icon.
/// </summary>
[RegisterPower]
public sealed class ShieldTaxPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override int DisplayAmount => GetInternalData<Data>().Progress;

    private sealed class Data
    {
        public int Progress;
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (amount <= 0m || base.Amount <= 0m)
        {
            return;
        }

        // Enemies only — the owner's own block (and the cosmetic mirror clones, which are allies)
        // pays no tax.
        if (creature == base.Owner
            || base.Owner.CombatState == null || !base.Owner.CombatState.HittableEnemies.Contains(creature))
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.Progress += (int)amount;

        int threshold = (int)base.Amount;
        int strength = data.Progress / threshold;
        if (strength > 0)
        {
            data.Progress -= strength * threshold;
            Flash();
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Owner, strength, base.Owner, null);
        }

        InvokeDisplayAmountChanged();
    }
}
