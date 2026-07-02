using STS2RitsuLib.Scaffolding.Content;

namespace Illusionist.Scripts;

/// <summary>
/// The Illusionist's dedicated potion pool. Potions register themselves with
/// <c>[RegisterPotion(typeof(IllusionistPotionPool))]</c> (RitsuLib auto-registration). Shared/common
/// potions come from the game's shared pools; this only holds the Illusionist's own.
/// </summary>
public sealed class IllusionistPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "necrobinder";
}
