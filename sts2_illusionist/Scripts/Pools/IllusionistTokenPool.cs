using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

// Namespace is the parent Illusionist.Scripts (not .Pools) so cards resolve the pool via their
// enclosing namespace, matching IllusionistCardPool.
namespace Illusionist.Scripts;

/// <summary>
/// A NON-reward pool for Illusionist cards that must never appear in card rewards: generated tokens
/// (先见 / 暗淡油灯 / 熄灭油灯 — only ever spawned by other cards) and any card retired from the normal
/// pool (e.g. 逆转 Reversal, kept as an Event/先古 model but unobtainable). Card rewards are pulled
/// exclusively from the
/// character's assigned pool (<see cref="IllusionistCardPool"/>, i.e. <c>player.Character.CardPool</c>),
/// so any token registered there can be rolled into a reward: the game's reward/merchant filters
/// exclude Basic/Ancient/Event but NOT Token (base-game tokens like Minion Sacrifice are simply in no
/// pool). Keeping tokens in this separate pool takes them out of the reward roll while still giving
/// them a real model + ID so <c>CreateCard&lt;T&gt;()</c> works. Visual identity mirrors
/// <see cref="IllusionistCardPool"/> so the tokens still render as Illusionist cards.
/// </summary>
public sealed class IllusionistTokenPool : TypeListCardPoolModel
{
    public override string Title => "illusionist";

    public override string EnergyColorName => "necrobinder";

    public override Color DeckEntryCardColor => new Color("CD4EED");

    public override Color EnergyOutlineColor => new Color("803367");

    public override bool IsColorless => false;

    // Mirrors IllusionistCardPool.PoolFrameMaterial (hsv.gdshader: dark, muted, red-brown).
    private static Material? _frame;

    public override Material? PoolFrameMaterial =>
        _frame ??= MaterialUtils.CreateHsvShaderMaterial(0.025f, 0.50f, 0.42f);

    public override string? TextEnergyIconPath => "res://illusionist/art/illusionist_energy_icon_text.webp";

    public override string? BigEnergyIconPath => "res://illusionist/art/illusionist_energy_icon.webp";
}
