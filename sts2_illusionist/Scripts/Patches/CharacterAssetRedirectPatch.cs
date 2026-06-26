using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Reuse Necrobinder's character ART for the standalone Illusionist. Almost every character asset
/// path is derived from the character's id ("illusionist") and is private/non-virtual, so we can't
/// override them on the model. Instead we rewrite "illusionist" → "necrobinder" in the two path
/// helpers every such path flows through (<see cref="SceneHelper.GetScenePath"/> for scenes and
/// <see cref="ImageHelper.GetImagePath"/> for images), plus the one raw-string path
/// (<see cref="CharacterModel.CharacterSelectTransitionPath"/>). The game ships no "illusionist"
/// assets, so this only ever affects our character's derived paths.
/// </summary>
[HarmonyPatch(typeof(SceneHelper), nameof(SceneHelper.GetScenePath))]
public static class SceneHelperRedirectPatch
{
    private static void Postfix(ref string __result)
    {
        if (__result != null && __result.Contains("illusionist"))
        {
            __result = __result.Replace("illusionist", "necrobinder");
        }
    }
}

[HarmonyPatch(typeof(ImageHelper), nameof(ImageHelper.GetImagePath))]
public static class ImageHelperRedirectPatch
{
    private static void Postfix(ref string __result)
    {
        if (__result != null && __result.Contains("illusionist"))
        {
            __result = __result.Replace("illusionist", "necrobinder");
        }
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectTransitionPath), MethodType.Getter)]
public static class CharacterSelectTransitionRedirectPatch
{
    private static void Postfix(ref string __result)
    {
        if (__result != null && __result.Contains("illusionist"))
        {
            __result = __result.Replace("illusionist", "necrobinder");
        }
    }
}
