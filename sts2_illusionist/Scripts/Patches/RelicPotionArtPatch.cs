using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Swap in custom bitmap art for the Illusionist's own relics and potions. The relevant getters all
/// return <see cref="Texture2D"/> (and <see cref="ImageTexture"/> derives from it), so a postfix can
/// substitute a runtime-decoded image. Only our own models are affected (<see cref="RelicArt"/> /
/// <see cref="PotionArt"/> return null for base-game models), and a model with no art file keeps its
/// borrowed atlas icon.
///
/// <para>The relic's small bar-icon (<c>Icon</c>) and big tooltip-icon (<c>BigIcon</c>) reuse the same
/// source image. The outline (<c>IconOutline</c>) is left borrowed for now — the selection glow keeps
/// the donor relic's silhouette; a dedicated outline can be added later.</para>
/// </summary>
[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.Icon), MethodType.Getter)]
public static class RelicIconArtPatch
{
    private static void Postfix(RelicModel __instance, ref Texture2D __result)
    {
        ImageTexture? art = RelicArt.For(__instance);
        if (art != null)
        {
            __result = art;
        }
    }
}

[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.BigIcon), MethodType.Getter)]
public static class RelicBigIconArtPatch
{
    private static void Postfix(RelicModel __instance, ref Texture2D __result)
    {
        ImageTexture? art = RelicArt.For(__instance);
        if (art != null)
        {
            __result = art;
        }
    }
}

[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.IconOutline), MethodType.Getter)]
public static class RelicIconOutlineArtPatch
{
    private static void Postfix(RelicModel __instance, ref Texture2D __result)
    {
        ImageTexture? outline = RelicArt.OutlineFor(__instance);
        if (outline != null)
        {
            __result = outline;
        }
    }
}

[HarmonyPatch(typeof(PotionModel), nameof(PotionModel.Image), MethodType.Getter)]
public static class PotionImageArtPatch
{
    private static void Postfix(PotionModel __instance, ref Texture2D __result)
    {
        ImageTexture? art = PotionArt.For(__instance);
        if (art != null)
        {
            __result = art;
        }
    }
}
