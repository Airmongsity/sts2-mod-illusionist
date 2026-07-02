using Illusionist.Scripts.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// Temporary Strength LOSS applied to the player by 征召 (Conscript). Subclasses the base game's
/// <see cref="TemporaryStrengthPower"/> with <c>IsPositive => false</c> (the same machinery as
/// Monarch's Gaze / Shackling Potion's down-powers): it strips real Strength on application and
/// restores it at the end of the owner's turn — i.e. "lose N Strength this turn".
/// </summary>
[RegisterPower]
public sealed class ConscriptStrengthDownPower : TemporaryStrengthPower
{
    protected override bool IsPositive => false;

    public override AbstractModel OriginModel => ModelDb.Card<ConscriptIllusionist>();
}

/// <summary>
/// Temporary Dexterity LOSS applied to the player by 征召 (Conscript) — the Dexterity twin of
/// <see cref="ConscriptStrengthDownPower"/> (restored at end of the owner's turn).
/// </summary>
[RegisterPower]
public sealed class ConscriptDexterityDownPower : TemporaryDexterityPower
{
    protected override bool IsPositive => false;

    public override AbstractModel OriginModel => ModelDb.Card<ConscriptIllusionist>();
}
