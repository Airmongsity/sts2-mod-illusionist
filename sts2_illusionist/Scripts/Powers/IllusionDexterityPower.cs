using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Illusionist.Scripts.Cards;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// Temporary Dexterity applied by 错觉 (Illusion) to flip negative Dexterity up to its absolute
/// value for the turn. Reuses the engine's <see cref="TemporaryDexterityPower"/> machinery, which
/// adds the Dexterity on apply and removes it at end of turn automatically.
/// </summary>
public sealed class IllusionDexterityPower : TemporaryDexterityPower
{
    public override AbstractModel OriginModel => ModelDb.Card<Illusion>();
}
