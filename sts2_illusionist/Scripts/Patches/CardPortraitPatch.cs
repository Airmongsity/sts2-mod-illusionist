using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Give specific Illusionist cards custom portrait art. <c>CardModel.Portrait</c> normally loads an
/// atlas texture from a path derived from the pool title ("illusionist"), where the game ships no
/// Illusionist card portraits, so our cards render blank. This prefix intercepts the getter and, for
/// any card registered in <see cref="CardArt"/>, returns our runtime-decoded <see cref="ImageTexture"/>
/// instead. Cards without custom art fall through to the original getter unchanged.
/// (Phase 2 of the RitsuLib migration replaces this with per-card <c>CardAssetProfile</c>s.)
/// </summary>
public sealed class CardPortraitPatch : IPatchMethod
{
    public static string PatchId => "illusionist_card_portrait";

    public static string Description => "Serve runtime-decoded card portraits for Illusionist cards";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter),
    };

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
