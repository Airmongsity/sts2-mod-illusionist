using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

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
/// ResourceLoader-loadable resource (imported <c>.ctex</c>, see <c>build_pck.gd</c> IMPORTED_DIRS).</para>
///
/// <para><b>Potions</b>: still use the runtime-decoded raw image via <see cref="PotionArt"/> on the
/// <c>PotionModel.Image</c> getter (potions aren't shown on screens that inline it).</para>
///
/// (Phase 2 of the RitsuLib migration replaces this with <c>ModRelicTemplate</c>/<c>ModPotionTemplate</c>
/// asset profiles.)
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

public sealed class RelicIconPathPatch : IPatchMethod
{
    public static string PatchId => "illusionist_relic_icon_paths";

    public static string Description => "Redirect relic icon path getters to the Illusionist's imported webps";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(RelicModel), "PackedIconPath", MethodType.Getter),
        new(typeof(RelicModel), "PackedIconOutlinePath", MethodType.Getter),
        new(typeof(RelicModel), "BigIconPath", MethodType.Getter),
    };

    private static void Postfix(RelicModel __instance, ref string __result)
    {
        string? path = RelicArtPath.For(__instance);
        if (path != null)
        {
            __result = path;
        }
    }
}

public sealed class PotionImageArtPatch : IPatchMethod
{
    public static string PatchId => "illusionist_potion_image_art";

    public static string Description => "Serve runtime-decoded images for Illusionist potions";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(PotionModel), nameof(PotionModel.Image), MethodType.Getter),
    };

    private static void Postfix(PotionModel __instance, ref Texture2D __result)
    {
        ImageTexture? art = PotionArt.For(__instance);
        if (art != null)
        {
            __result = art;
        }
    }
}
