using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using IllusionistCharacter = Illusionist.Scripts.Characters.Illusionist;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Fix the run-WIN freeze: ancient events store per-character dialogue, and the Illusionist has none,
/// so <see cref="AncientDialogueSet.GetValidDialogues"/> returns an empty set. Most ancients fall back
/// to agnostic dialogue, but <c>TheArchitect</c> (the final-boss win sequence) asks with
/// <c>allowAnyCharacterDialogues: false</c>, gets nothing, and its <c>WinRun()</c> then dereferences a
/// null <c>Dialogue</c> (<c>Dialogue.EndAttackers</c>) → NullReferenceException that hangs the victory.
///
/// When the lookup comes up empty for the Illusionist, reuse Necrobinder's dialogues (consistent with
/// our asset reuse) so <c>Dialogue</c> is non-null and the win proceeds. Only triggers on the truly
/// empty case, so it doesn't disturb ancients that already resolve agnostic dialogue.
/// </summary>
[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
public static class ArchitectDialogueFallbackPatch
{
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
