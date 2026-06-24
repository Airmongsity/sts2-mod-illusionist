using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using Illusionist.Scripts.Cards;
using Illusionist.Scripts.Relics;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Filter the Necrobinder relic pool down to the Illusionist's relics (parallel to the card-pool
/// patch). The base Necrobinder relics are Osty/Doom/Soul-themed and mostly dead for us; we keep
/// only the two that work generically (Bookmark, Ivory Tile) plus everything from this mod's
/// assembly. AllRelicIds / GetUnlockedRelics both derive from AllRelics, so they are filtered too.
/// </summary>
[HarmonyPatch(typeof(RelicPoolModel), nameof(RelicPoolModel.AllRelics), MethodType.Getter)]
public static class NecrobinderRelicPoolPatch
{
    private static readonly Assembly ModAssembly = typeof(Entry).Assembly;

    private static void Postfix(RelicPoolModel __instance, ref IEnumerable<RelicModel> __result)
    {
        if (__instance is not NecrobinderRelicPool)
        {
            return;
        }

        __result = __result
            .Where(r => r.GetType().Assembly == ModAssembly || r is Bookmark || r is IvoryTile)
            .ToArray();
    }
}

/// <summary>
/// Filter the Necrobinder potion pool down to the Illusionist's potions (parallel to the relic
/// pool). The base Necrobinder character potions (Bone Brew, Pot of Ghouls, Potion of Doom) are
/// Summon/Soul/Doom-themed and dead for us; drop them, keep only this mod's potions.
/// </summary>
[HarmonyPatch(typeof(PotionPoolModel), nameof(PotionPoolModel.AllPotions), MethodType.Getter)]
public static class NecrobinderPotionPoolPatch
{
    private static readonly Assembly ModAssembly = typeof(Entry).Assembly;

    private static void Postfix(PotionPoolModel __instance, ref IEnumerable<PotionModel> __result)
    {
        if (__instance is not NecrobinderPotionPool)
        {
            return;
        }

        __result = __result.Where(p => p.GetType().Assembly == ModAssembly).ToArray();
    }
}

/// <summary>
/// Teach Touch of Orobas (the Ancient relic that upgrades your starter relic) about the
/// Illusionist's starter: map HallucinatoryLamp -> AncientLamp. Without this, the base
/// RefinementUpgrades map has no entry for the Lamp, so taking Touch of Orobas would replace it
/// with the useless Circlet fallback. The getter returns a fresh dictionary each call, so a postfix
/// can safely add our entry.
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), "RefinementUpgrades", MethodType.Getter)]
public static class TouchOfOrobasRefinementPatch
{
    private static void Postfix(ref Dictionary<ModelId, RelicModel> __result)
    {
        __result[ModelDb.Relic<HallucinatoryLamp>().Id] = ModelDb.Relic<AncientLamp>();
    }
}

/// <summary>
/// Teach Archaic Tooth (the Ancient relic that transcends a starter card) about the Illusionist:
/// map Riposte -> Sabotage (破坏). Same private-static-getter postfix pattern as Touch of Orobas.
/// Without an entry the option simply isn't offered to the Illusionist (no trap), but we want it.
/// </summary>
[HarmonyPatch(typeof(ArchaicTooth), "TranscendenceUpgrades", MethodType.Getter)]
public static class ArchaicToothTranscendencePatch
{
    private static void Postfix(ref Dictionary<ModelId, CardModel> __result)
    {
        __result[ModelDb.Card<Riposte>().Id] = ModelDb.Card<Sabotage>();
    }
}
