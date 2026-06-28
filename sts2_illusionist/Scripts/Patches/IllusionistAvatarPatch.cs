using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Give the standalone Illusionist its own avatar art instead of the borrowed Necrobinder ones:
///
/// <list type="bullet">
/// <item><b>Top-bar / run-history icon</b> (the small square head, native 85×85). Every consumer reads
/// <see cref="CharacterModel.IconTexture"/> (a <see cref="Texture2D"/>), so we postfix that getter and
/// swap in <c>avatar-s.png</c>. (<see cref="ImageTexture"/> derives from <see cref="Texture2D"/>, so
/// the return type matches.)</item>
/// <item><b>Character-select portrait</b> (the立绘 on the select button, native 132×195). That comes
/// from <see cref="CharacterModel.CharacterSelectIcon"/>, whose return type is a
/// <c>CompressedTexture2D</c> we can't substitute an <see cref="ImageTexture"/> for — so instead we
/// postfix <see cref="NCharacterSelectButton.Init"/> and overwrite the button's <c>_icon</c> texture
/// with <c>avatar-m.png</c>.</item>
/// </list>
///
/// Both fall back to the borrowed Necrobinder art if our PNG fails to load, so neither can crash.
/// </summary>
[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconTexture), MethodType.Getter)]
public static class IllusionistTopBarIconPatch
{
    private static void Postfix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is not global::Illusionist.Scripts.Characters.Illusionist)
        {
            return;
        }

        ImageTexture? icon = IllusionistArt.TopBarIcon;
        if (icon != null)
        {
            __result = icon;
        }
    }
}

/// <summary>
/// The in-run HUD avatar (top-left during a run, <c>NCharacterStats</c>) doesn't use
/// <see cref="CharacterModel.IconTexture"/> — it instantiates the <see cref="CharacterModel.Icon"/>
/// <i>scene</i> (<c>ui/character_icons/&lt;id&gt;_icon</c>, redirected to Necrobinder), whose root is a
/// single <see cref="TextureRect"/>. We postfix that getter and swap the texture for our avatar so the
/// top-left icon updates too. (Run-history / multiplayer icons go through <c>IconTexture</c> above.)
/// </summary>
[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.Icon), MethodType.Getter)]
public static class IllusionistTopBarIconScenePatch
{
    private static void Postfix(CharacterModel __instance, ref Control __result)
    {
        if (__instance is not global::Illusionist.Scripts.Characters.Illusionist || __result == null)
        {
            return;
        }

        ImageTexture? icon = IllusionistArt.TopBarIcon;
        if (icon == null)
        {
            return;
        }

        // The icon scene's root is a TextureRect; fall back to the first descendant TextureRect.
        TextureRect? rect = __result as TextureRect ?? __result.FindChild("*", recursive: true, owned: false) as TextureRect;
        if (rect != null)
        {
            rect.Texture = icon;
        }
    }
}

[HarmonyPatch(typeof(NCharacterSelectButton), "Init")]
public static class IllusionistSelectPortraitPatch
{
    private static void Postfix(NCharacterSelectButton __instance)
    {
        if (__instance.Character is not global::Illusionist.Scripts.Characters.Illusionist)
        {
            return;
        }

        ImageTexture? portrait = IllusionistArt.SelectPortrait;
        if (portrait == null)
        {
            return;
        }

        if (AccessTools.Field(typeof(NCharacterSelectButton), "_icon").GetValue(__instance) is TextureRect icon)
        {
            icon.Texture = portrait;
        }
    }
}
