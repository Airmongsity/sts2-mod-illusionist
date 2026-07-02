using Illusionist.Scripts.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// The temporary-Strength power applied to an enemy by 挑衅 (ProvokeIllusionist). Subclasses the base game's
/// <see cref="TemporaryStrengthPower"/> (same machinery as Flex Potion): it grants real Strength on
/// application and strips it again at the end of the owner's turn. Applied to an enemy, the bonus
/// persists through the enemy's own attack — so the inflated swing is real (the player must block or
/// otherwise neutralize it) — and is cleaned up at the end of the enemy's turn.
/// </summary>
[RegisterPower]
public sealed class ProvokePower : TemporaryStrengthPower, IModPowerAssetOverrides
{
    public override AbstractModel OriginModel => ModelDb.Card<ProvokeIllusionist>();

    // Can't rebase onto IllusionistPower (needs the TemporaryStrengthPower machinery), so implement
    // the RitsuLib asset interface directly for its custom icon (powers/provoke.webp).
    public PowerAssetProfile AssetProfile => PowerAssetProfile.Empty;

    public string? CustomIconPath => IllusionistArtPaths.PowerIcon(GetType());

    public string? CustomBigIconPath => IllusionistArtPaths.PowerIcon(GetType());
}
