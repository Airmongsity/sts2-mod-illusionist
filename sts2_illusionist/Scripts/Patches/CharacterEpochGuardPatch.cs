using HarmonyLib;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Managers;
using IllusionistCharacter = Illusionist.Scripts.Characters.Illusionist;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// The base game's per-character progress/epoch checks switch on the five base characters and throw
/// for anything else — <c>CheckFifteenElitesDefeatedEpoch</c> / <c>CheckFifteenBossesDefeatedEpoch</c>
/// throw <see cref="System.ArgumentOutOfRangeException"/> (the reported elite-win crash), and
/// <c>ObtainCharUnlockEpoch</c> calls <c>EpochModel.Get("ILLUSIONIST*_EPOCH")</c> which throws because
/// the Illusionist has no epochs (crashes at act transitions). The Illusionist has nothing to track
/// in these, so we skip them entirely for our character (Prefix returning false). Normal win
/// tracking (FightStats, discovered enemies) lives in the callers and still runs.
/// </summary>
[HarmonyPatch]
public static class CharacterEpochGuardPatch
{
    private static bool IsIllusionist(Player? player) => player?.Character is IllusionistCharacter;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")]
    private static bool SkipElitesEpoch(Player localPlayer) => !IsIllusionist(localPlayer);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")]
    private static bool SkipBossesEpoch(Player localPlayer) => !IsIllusionist(localPlayer);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
    private static bool SkipActUnlockEpoch(Player localPlayer) => !IsIllusionist(localPlayer);
}

/// <summary>
/// Defensive guard for <see cref="CharacterModel.RunWonAchievement"/>, which is
/// <c>Enum.Parse&lt;Achievement&gt;(Id.Entry.Capitalize() + "Win")</c> — there is no "IllusionistWin"
/// in the (hardcoded, 5-character) <see cref="Achievement"/> enum, so the getter would throw if ever
/// invoked on a run win. It appears to be dead code (nothing in the assembly reads it), but we skip
/// the parse for the Illusionist and return a harmless default so it can never crash a victory.
/// </summary>
[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.RunWonAchievement), MethodType.Getter)]
public static class RunWonAchievementGuardPatch
{
    private static bool Prefix(CharacterModel __instance, ref Achievement __result)
    {
        if (__instance is IllusionistCharacter)
        {
            __result = default; // never actually consumed; just avoids the Enum.Parse throw.
            return false;
        }

        return true;
    }
}
