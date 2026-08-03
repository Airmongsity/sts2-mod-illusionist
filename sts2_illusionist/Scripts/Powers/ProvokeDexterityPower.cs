using Illusionist.Scripts.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// Temporary Dexterity granted by Provoke; removed at the end of the player's turn.
/// </summary>
[RegisterPower]
public sealed class ProvokeDexterityPower : TemporaryDexterityPower, IModPowerAssetOverrides
{
    public override AbstractModel OriginModel => ModelDb.Card<ProvokeIllusionist>();

    public PowerAssetProfile AssetProfile => PowerAssetProfile.Empty;

    public string? CustomIconPath => IllusionistArtPaths.PowerIcon(typeof(ProvokePower));

    public string? CustomBigIconPath => IllusionistArtPaths.PowerIcon(typeof(ProvokePower));
}
