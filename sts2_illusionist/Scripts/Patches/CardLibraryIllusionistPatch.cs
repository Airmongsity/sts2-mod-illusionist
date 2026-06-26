using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Make the Illusionist's cards visible in the card-library compendium. The library's pool filters
/// are HARDCODED to the five base characters (each checks <c>c.Pool is &lt;X&gt;CardPool</c>) — there
/// is no Illusionist tab, so our cards (in <see cref="IllusionistCardPool"/>) match no filter and are
/// hidden (only our Ancient cards show, under the Ancients tab).
///
/// We fold them into the existing "Misc" catch-all filter by OR-ing <see cref="IllusionistCardPool"/>
/// into its predicate after <c>_Ready</c> builds the filter dictionary. Misc is chosen so we don't
/// pollute a base character's tab. (A dedicated tab would require adding a UI node to the base-game
/// card-library scene, which a code mod can't do cleanly.)
/// </summary>
[HarmonyPatch(typeof(NCardLibrary), "_Ready")]
public static class CardLibraryIllusionistPatch
{
    private static void Postfix(NCardLibrary __instance)
    {
        var poolFilters = Traverse.Create(__instance).Field("_poolFilters")
            .GetValue<Dictionary<NCardPoolFilter, Func<CardModel, bool>>>();
        var miscNode = Traverse.Create(__instance).Field("_miscPoolFilter")
            .GetValue<NCardPoolFilter>();

        if (poolFilters == null || miscNode == null || !poolFilters.ContainsKey(miscNode))
        {
            return;
        }

        Func<CardModel, bool> original = poolFilters[miscNode];
        poolFilters[miscNode] = c => original(c) || c.Pool is IllusionistCardPool;
    }
}
