using STS2RitsuLib.Scaffolding.Content;

namespace Illusionist.Scripts;

/// <summary>
/// The Illusionist's dedicated relic pool. Relics register themselves with
/// <c>[RegisterRelic(typeof(IllusionistRelicPool))]</c> (RitsuLib auto-registration).
/// </summary>
public sealed class IllusionistRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "necrobinder";
}
