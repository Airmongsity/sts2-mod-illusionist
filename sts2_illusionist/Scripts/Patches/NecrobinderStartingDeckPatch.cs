using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using Illusionist.Scripts.Cards;
using Illusionist.Scripts.Relics;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// The Illusionist reskins the Necrobinder's slot (we reuse its art rather than ship a new,
/// art-less character — a standalone character crashed the select screen). These patches turn
/// that slot into the Illusionist: a 4-attack / 4-defend starting deck built around the
/// signature cards, and a new starter relic (迷幻灯 / Hallucinatory Lamp) in place of
/// Bound Phylactery — which also removes Osty, since Osty is spawned by Bound Phylactery.
/// </summary>
[HarmonyPatch(typeof(Necrobinder), nameof(Necrobinder.StartingDeck), MethodType.Getter)]
public static class NecrobinderStartingDeckPatch
{
    private static void Postfix(ref IEnumerable<CardModel> __result)
    {
        MegaCrit.Sts2.Core.Logging.Log.Info("[illusionist] NecrobinderStartingDeckPatch applied (Illusionist starting deck).");
        __result = new CardModel[]
        {
            // 4 Strikes
            ModelDb.Card<StrikeNecrobinder>(),
            ModelDb.Card<StrikeNecrobinder>(),
            ModelDb.Card<StrikeNecrobinder>(),
            ModelDb.Card<StrikeNecrobinder>(),
            // 4 Defends
            ModelDb.Card<DefendNecrobinder>(),
            ModelDb.Card<DefendNecrobinder>(),
            ModelDb.Card<DefendNecrobinder>(),
            ModelDb.Card<DefendNecrobinder>(),
            // Signature starter cards
            ModelDb.Card<Riposte>(),
            ModelDb.Card<Disrupt>(),
        };
    }
}

/// <summary>
/// Replace the Necrobinder's starter relic (Bound Phylactery) with the Illusionist's
/// 迷幻灯 (Hallucinatory Lamp). Because Bound Phylactery is what summons Osty at combat
/// start, swapping it out also gives the Illusionist no Osty.
/// </summary>
[HarmonyPatch(typeof(Necrobinder), nameof(Necrobinder.StartingRelics), MethodType.Getter)]
public static class NecrobinderStartingRelicsPatch
{
    private static void Postfix(ref IReadOnlyList<RelicModel> __result)
    {
        __result = new RelicModel[]
        {
            ModelDb.Relic<HallucinatoryLamp>(),
        };
    }
}
