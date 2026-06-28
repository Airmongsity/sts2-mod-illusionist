using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Give specific Illusionist cards custom portrait art. <c>CardModel.Portrait</c> normally loads an
/// atlas texture from a path derived from the pool title ("illusionist"), which
/// <see cref="ImageHelperRedirectPatch"/> rewrites to "necrobinder" — and the game ships no Illusionist
/// card portraits there, so our cards render blank. This prefix intercepts the getter and, for any card
/// registered in <see cref="CardArt"/>, returns our runtime-decoded <see cref="ImageTexture"/> instead.
/// Cards without custom art fall through to the original getter unchanged.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter)]
public static class CardPortraitPatch
{
    private static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        ImageTexture? art = CardArt.For(__instance);
        if (art == null)
        {
            return true; // no custom art — let the engine resolve the default portrait.
        }

        __result = art;
        return false; // skip the original getter.
    }
}
