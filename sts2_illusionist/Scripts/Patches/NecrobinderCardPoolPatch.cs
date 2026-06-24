using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// REPLACE (not add) the Necrobinder card pool with the Illusionist's own cards. The base
/// <see cref="CardPoolModel.AllCards"/> getter returns the 88 Necrobinder cards plus modded
/// cards; for the reskinned Illusionist we want ONLY our cards to appear in rewards / random
/// card effects / the card library. This postfix keeps just the cards defined in this mod's
/// assembly, so every original Necrobinder reward card drops out of the pool.
///
/// IMPORTANT: we must also KEEP StrikeNecrobinder / DefendNecrobinder. They are base-game cards
/// in the Illusionist's starting deck, and a base card resolves <see cref="CardModel.Pool"/> by
/// SEARCHING pool membership (<c>AllCardPools.First(p =&gt; p.AllCardIds.Contains(Id))</c>). If we
/// remove them from this pool they belong to no pool, so the search falls through to the
/// test-only MockCardPool whose <c>GenerateAllCards()</c> throws "You monster!" — which crashed
/// the deck-transform screen (New Leaf relic). Our own cards override <c>Pool</c> directly, so
/// they never trigger that search. Strike/Defend are Basic rarity, so keeping them here does NOT
/// add them to reward rolls.
///
/// <see cref="CardPoolModel.AllCardIds"/> derives from <c>AllCards</c>, so it is filtered too.
/// </summary>
[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.AllCards), MethodType.Getter)]
public static class NecrobinderCardPoolPatch
{
    private static readonly Assembly ModAssembly = typeof(Entry).Assembly;

    private static void Postfix(CardPoolModel __instance, ref IEnumerable<CardModel> __result)
    {
        if (__instance is not NecrobinderCardPool)
        {
            return;
        }

        __result = __result
            .Where(c => c.GetType().Assembly == ModAssembly || c is StrikeNecrobinder || c is DefendNecrobinder)
            .ToArray();
    }
}
