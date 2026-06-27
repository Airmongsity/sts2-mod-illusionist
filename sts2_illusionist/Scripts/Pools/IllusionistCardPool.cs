using Godot;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts.Cards;

// Namespace is the parent Illusionist.Scripts (not .Pools) so every card in Illusionist.Scripts.Cards
// resolves IllusionistCardPool via its enclosing namespace — no extra using per card file.
namespace Illusionist.Scripts;

/// <summary>
/// The Illusionist's dedicated card pool — the heart of decoupling from the Necrobinder slot. Cards
/// are added through <c>ModHelper.AddModelToPool&lt;IllusionistCardPool, T&gt;()</c> in
/// <see cref="Entry"/> (base <see cref="CardPoolModel.AllCards"/> = GenerateAllCards + mod additions),
/// so this only seeds the reused basic Strike/Defend. Visual props point at Necrobinder's assets
/// (energy color, pink frame) so the look is reused.
/// </summary>
public sealed class IllusionistCardPool : CardPoolModel
{
    public override string Title => "illusionist";

    public override string EnergyColorName => "necrobinder";

    public override string CardFrameMaterialPath => "card_frame_pink";

    public override Color DeckEntryCardColor => new Color("CD4EED");

    public override Color EnergyOutlineColor => new Color("803367");

    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards() => new CardModel[]
    {
        ModelDb.Card<IllusionistStrike>(),
        ModelDb.Card<IllusionistDefend>(),
        // Tokens / Ancient cards that aren't added to the reward pool, but should still appear in the
        // card compendium (the library shows every card in a pool's AllCards). Their Token/Ancient
        // rarity keeps them out of all reward rolls.
        ModelDb.Card<DimLamp>(),
        ModelDb.Card<Sabotage>(),
    };
}
