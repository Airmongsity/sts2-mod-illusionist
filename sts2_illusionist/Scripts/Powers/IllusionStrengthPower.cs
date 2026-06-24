using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Illusionist.Scripts.Cards;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// Temporary Strength applied by 错觉 (Illusion) to flip negative Strength up to its absolute
/// value for the turn. Reuses the engine's <see cref="TemporaryStrengthPower"/> machinery, which
/// adds the Strength on apply and removes it at end of turn automatically.
/// </summary>
public sealed class IllusionStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<Illusion>();
}
