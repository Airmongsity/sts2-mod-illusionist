using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Sets the mirror pile button's icon to the owning character's avatar, so the button shows the
/// character's portrait (e.g. Ironclad's icon) instead of a hardcoded Illusionist icon. This is
/// needed because <see cref="MirrorPile.Register"/> clears <c>IconPath</c> to enable cross-character
/// mirror visual support — the icon is set dynamically here in a postfix on
/// <see cref="NModCardPileButton.Initialize"/>.
/// </summary>
public sealed class MirrorPileIconPatch : IPatchMethod
{
    private static readonly FieldInfo IconField =
        typeof(NModCardPileButton).GetField("_icon", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static string PatchId => "mirror_pile_icon";

    public static string Description => "Show the owning character's avatar for the mirror pile button";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(NModCardPileButton), nameof(NModCardPileButton.Initialize)),
    };

    private static void Postfix(NModCardPileButton __instance, Player player)
    {
        // Only apply to the mirror pile button.
        if (__instance.Definition?.Id != MirrorPile.Id)
        {
            return;
        }

        // Use the character's pre-resolved IconTexture (the top-bar avatar). RitsuLib's
        // CharacterIconTexturePathPatch ensures mod characters return their correct icon.
        Texture2D? texture = player.Character?.IconTexture;
        if (texture == null)
        {
            return;
        }

        // The combat bottom-left pile icon is always a TextureRect in practice.
        if (IconField.GetValue(__instance) is TextureRect textureRect)
        {
            textureRect.Texture = texture;
        }
    }
}
