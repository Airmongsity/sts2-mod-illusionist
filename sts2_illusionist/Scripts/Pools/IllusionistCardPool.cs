using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

// Namespace is the parent Illusionist.Scripts (not .Pools) so every card in Illusionist.Scripts.Cards
// resolves IllusionistCardPool via its enclosing namespace — no extra using per card file.
namespace Illusionist.Scripts;

/// <summary>
/// The Illusionist's dedicated card pool. Cards register themselves with
/// <c>[RegisterCard(typeof(IllusionistCardPool))]</c> (RitsuLib auto-registration); nothing is listed
/// here. The frame material and energy icons live on the pool so RitsuLib applies them to every card
/// in it (this replaced IllusionistFramePatch and the card-orb half of IllusionistEnergyPatch).
/// </summary>
public sealed class IllusionistCardPool : TypeListCardPoolModel
{
    public override string Title => "illusionist";

    public override string EnergyColorName => "necrobinder";

    public override Color DeckEntryCardColor => new Color("CD4EED");

    public override Color EnergyOutlineColor => new Color("803367");

    public override bool IsColorless => false;

    // hsv.gdshader params (compare: card_frame_red = 0.025/0.85/1.0). Dark, muted, red-brown.
    private static Material? _frame;

    public override Material? PoolFrameMaterial =>
        _frame ??= MaterialUtils.CreateHsvShaderMaterial(0.025f, 0.50f, 0.42f);

    public override string? TextEnergyIconPath => "res://illusionist/art/illusionist_energy_icon.webp";

    public override string? BigEnergyIconPath => "res://illusionist/art/illusionist_energy_icon.webp";
}
