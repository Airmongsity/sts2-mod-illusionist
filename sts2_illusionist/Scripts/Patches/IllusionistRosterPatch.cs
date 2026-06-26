using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using IllusionistCharacter = Illusionist.Scripts.Characters.Illusionist;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Add the Illusionist to the playable-character roster. <see cref="ModelDb.AllCharacters"/> is a
/// HARDCODED array of the five base characters (it is NOT auto-discovered), and the character-select
/// screen iterates exactly that — so a mod character registered in the ModelDb still never appears
/// until we append it here.
///
/// Guarded by <see cref="ModelDb.Contains"/> so it can't throw if the getter is hit before the mod's
/// models are injected (it just yields the base roster until the Illusionist is registered).
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
public static class IllusionistRosterPatch
{
    private static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        if (ModelDb.Contains(typeof(IllusionistCharacter)))
        {
            __result = __result.Concat(new CharacterModel[] { ModelDb.Character<IllusionistCharacter>() });
        }
    }
}
