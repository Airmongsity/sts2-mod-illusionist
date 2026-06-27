using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Replace the Illusionist's character-select backdrop with our own image (illusionist_background.png)
/// instead of the borrowed Necrobinder one.
///
/// <para>The select screen instantiates <see cref="CharacterModel.CharacterSelectBg"/> as a Control,
/// names it <c>"&lt;id&gt;_bg"</c>, and drops it into its background container. We postfix the two
/// singleplayer entry points that do this and overlay a full-rect <see cref="TextureRect"/> carrying
/// our image. If the image fails to load we do nothing and the borrowed backdrop shows through.</para>
/// </summary>
public static class IllusionistCharacterSelectBg
{
    internal static void Inject(Node screen, CharacterModel characterModel)
    {
        try
        {
            if (characterModel is not global::Illusionist.Scripts.Characters.Illusionist || !GodotObject.IsInstanceValid(screen))
            {
                return;
            }

            ImageTexture? texture = IllusionistArt.CharacterSelectBackground;
            if (texture == null)
            {
                return;
            }

            // The screen just added a Control named "illusionist_bg" (CharacterModel.Id.Entry + "_bg").
            if (screen.FindChild(characterModel.Id.Entry + "_bg", recursive: true, owned: false) is not Control bg)
            {
                return;
            }

            if (bg.GetNodeOrNull("IllusionistBg") != null)
            {
                return;
            }

            TextureRect rect = new TextureRect
            {
                Name = "IllusionistBg",
                Texture = texture,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            bg.AddChild(rect);
            rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] CharacterSelect bg: inject failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
public static class IllusionistSelectCharacterBgPatch
{
    private static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
        => IllusionistCharacterSelectBg.Inject(__instance, characterModel);
}

[HarmonyPatch(typeof(NCharacterSelectScreen), "OnLocalCharacterChangedForRandom")]
public static class IllusionistRandomCharacterBgPatch
{
    private static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
        => IllusionistCharacterSelectBg.Inject(__instance, characterModel);
}
