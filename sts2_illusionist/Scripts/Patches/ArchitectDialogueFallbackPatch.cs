using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using STS2RitsuLib.Patching.Models;
using IllusionistCharacter = Illusionist.Scripts.Characters.Illusionist;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Fix the run-WIN freeze: ancient events store per-character dialogue, and the Illusionist has none,
/// so <see cref="AncientDialogueSet.GetValidDialogues"/> returns an empty set. Most ancients fall back
/// to agnostic dialogue, but <c>TheArchitect</c> (the final-boss win sequence) asks with
/// <c>allowAnyCharacterDialogues: false</c>, gets nothing, and its <c>WinRun()</c> then dereferences a
/// null <c>Dialogue</c> → NullReferenceException that hangs the victory.
///
/// When the lookup comes up empty for the Illusionist, reuse Necrobinder's dialogues (consistent with
/// our asset reuse) so <c>Dialogue</c> is non-null and the win proceeds. RitsuLib ships its own
/// Architect guard, but it is gated behind a debug-compatibility setting that is off by default, and
/// it injects empty lines rather than real dialogue — ours is nicer until we write real
/// <c>ancients.json</c> dialogue for the Illusionist.
/// </summary>
public sealed class ArchitectDialogueFallbackPatch : IPatchMethod
{
    public static string PatchId => "illusionist_architect_dialogue_fallback";

    public static string Description => "Fall back to Necrobinder ancient dialogue so TheArchitect's WinRun can't NRE";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues)),
    };

    private static void Postfix(AncientDialogueSet __instance, ModelId characterId, int charVisits, int totalVisits, ref IEnumerable<AncientDialogue> __result)
    {
        if (characterId != ModelDb.Character<IllusionistCharacter>().Id)
        {
            return;
        }

        if (__result != null && __result.Any())
        {
            return;
        }

        // Necrobinder has its own dialogues, so this inner call resolves normally (no recursion).
        __result = __instance.GetValidDialogues(
            ModelDb.Character<Necrobinder>().Id, charVisits, totalVisits, allowAnyCharacterDialogues: true);
    }
}
