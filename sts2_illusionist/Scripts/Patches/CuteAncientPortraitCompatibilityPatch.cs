using System;
using System.Threading;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Patching.Models;
using IllusionistCharacter = Illusionist.Scripts.Characters.Illusionist;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Compatibility guard for CuteAncientIllustrationFZ-Cards 1.0.0. That mod caches the
/// <see cref="Godot.Texture2D"/> previously shown by an <see cref="NCard"/>. STS2 can unload the
/// underlying imported resource between rooms while the managed wrapper remains in that cache. Its
/// portrait postfix then assigns the disposed wrapper during a deck-selection screen and lets the
/// resulting <see cref="ObjectDisposedException"/> fault the screen's initialization task.
///
/// The native NCard method has already restored the current portrait before the third-party postfix
/// runs, so suppressing only this known postfix failure leaves a valid base/Illusionist portrait and
/// allows the selection flow to finish. All unrelated exceptions continue to propagate.
/// </summary>
public sealed class CuteAncientPortraitCompatibilityPatch : IPatchMethod
{
    private const string FaultingPatchName = "CuteAncientIllustrationFZ_Cards.PortraitPatch";
    private static int _hasLoggedSuppression;

    public static string PatchId => "illusionist_cute_ancient_portrait_disposed_texture_guard";

    public static string Description => "Keep Illusionist card screens alive when Cute Ancient Portrait reuses an unloaded texture";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCard), nameof(NCard._EnterTree), Type.EmptyTypes),
        new(typeof(NCard), nameof(NCard.UpdateVisuals), [typeof(PileType), typeof(CardPreviewMode)]),
    ];

    private static Exception? Finalizer(NCard __instance, Exception? __exception)
    {
        if (__exception == null || !BelongsToIllusionist(__instance) || !IsDisposedPortraitCacheFailure(__exception))
        {
            return __exception;
        }

        if (Interlocked.Exchange(ref _hasLoggedSuppression, 1) == 0)
        {
            Entry.Logger.Warn(
                "[illusionist] Suppressed CuteAncientIllustrationFZ-Cards disposed portrait cache failure; " +
                "the native card portrait remains active.");
        }

        return null;
    }

    private static bool BelongsToIllusionist(NCard card)
    {
        try
        {
            CardModel? model = card.Model;
            return model is IllusionistCard || model?.Owner.Character is IllusionistCharacter;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDisposedPortraitCacheFailure(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is ObjectDisposedException disposed &&
                string.Equals(disposed.ObjectName, "Godot.CompressedTexture2D", StringComparison.Ordinal) &&
                current.ToString().Contains(FaultingPatchName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
