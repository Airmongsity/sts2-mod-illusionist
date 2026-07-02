using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// Hidden power that counts EVERY transform (forward transmutes + turn-start reverts)
/// across the whole combat. Used by 势 (Momentum) to grant draw/energy per 10 transforms.
/// </summary>
[RegisterPower]
public sealed class CombatTransformCount : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    private sealed class Data
    {
        public int TotalTransforms;
    }

    protected override object InitInternalData() => new Data();

    public int Total => GetInternalData<Data>().TotalTransforms;

    public void Increment() => GetInternalData<Data>().TotalTransforms++;
}
