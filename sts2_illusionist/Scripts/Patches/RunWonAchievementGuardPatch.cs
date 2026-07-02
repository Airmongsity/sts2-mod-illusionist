using HarmonyLib;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using IllusionistCharacter = Illusionist.Scripts.Characters.Illusionist;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Defensive guard for <see cref="CharacterModel.RunWonAchievement"/>, which is
/// <c>Enum.Parse&lt;Achievement&gt;(Id.Entry.Capitalize() + "Win")</c> — there is no matching entry in
/// the (hardcoded, 5-character) <see cref="Achievement"/> enum for a mod character, so the getter
/// would throw if ever invoked on a run win. It appears to be dead code (nothing in the assembly reads
/// it), and RitsuLib's epoch/unlock compatibility patches don't cover it, so we skip the parse for the
/// Illusionist and return a harmless default so it can never crash a victory.
/// </summary>
public sealed class RunWonAchievementGuardPatch : IPatchMethod
{
    public static string PatchId => "illusionist_run_won_achievement_guard";

    public static string Description => "Skip the hardcoded Achievement enum parse for the Illusionist";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(CharacterModel), nameof(CharacterModel.RunWonAchievement), MethodType.Getter),
    };

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
