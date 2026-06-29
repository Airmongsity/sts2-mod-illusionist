using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Custom relic/potion art for the Illusionist.
///
/// <para><b>Relics</b>: we redirect the relic's <c>virtual</c> PATH getters
/// (<c>PackedIconPath</c> / <c>PackedIconOutlinePath</c> / <c>BigIconPath</c>) to our imported webp,
/// rather than patching the <c>Icon</c>/<c>BigIcon</c> texture getters. Those texture getters are tiny
/// (<c>=&gt; ResourceLoader.Load(PackedIconPath)</c>) and the JIT inlines them at some call sites (e.g.
/// the character-select relic preview), which bypasses a getter postfix — that's why the icon showed
/// "NOPE" there. The path getters are <c>virtual</c>, so they're never inlined and every relic-icon
/// load (inlined or not, preload or not) flows through them. The webp must therefore be a
/// ResourceLoader-loadable resource (imported <c>.ctex</c>, see <c>build_pck.gd</c> IMPORTED_TEXTURES).</para>
///
/// <para><b>Potions</b>: still use the runtime-decoded raw image via <see cref="PotionArt"/> on the
/// <c>PotionModel.Image</c> getter (potions aren't shown on screens that inline it).</para>
/// </summary>
internal static class RelicArtPath
{
    private const string Dir = "res://illusionist/art/relics/";

    /// <summary>Our imported webp path for <paramref name="relic"/>, or null (not ours / no art).</summary>
    public static string? For(RelicModel relic)
    {
        if (relic.GetType().Assembly != typeof(RelicArtPath).Assembly)
        {
            return null;
        }
        string path = Dir + relic.GetType().Name.ToLowerInvariant() + ".webp";
        return ResourceLoader.Exists(path) ? path : null;
    }
}

[HarmonyPatch(typeof(RelicModel), "PackedIconPath", MethodType.Getter)]
public static class RelicPackedIconPathPatch
{
    private static void Postfix(RelicModel __instance, ref string __result)
    {
        string? path = RelicArtPath.For(__instance);
        if (path != null)
        {
            __result = path;
        }
    }
}

[HarmonyPatch(typeof(RelicModel), "PackedIconOutlinePath", MethodType.Getter)]
public static class RelicIconOutlinePathPatch
{
    private static void Postfix(RelicModel __instance, ref string __result)
    {
        string? path = RelicArtPath.For(__instance);
        if (path != null)
        {
            __result = path;
        }
    }
}

[HarmonyPatch(typeof(RelicModel), "BigIconPath", MethodType.Getter)]
public static class RelicBigIconPathPatch
{
    private static void Postfix(RelicModel __instance, ref string __result)
    {
        string? path = RelicArtPath.For(__instance);
        if (path != null)
        {
            __result = path;
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
