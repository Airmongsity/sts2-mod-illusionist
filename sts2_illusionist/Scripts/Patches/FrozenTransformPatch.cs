using Illusionist.Scripts.Afflictions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Makes Frozen a global transformation guard, not merely an Illusionist-transmutation special case.
/// </summary>
public sealed class FrozenTransformPatch : IPatchMethod
{
    public static string PatchId => "illusionist_frozen_transform_guard";

    public static string Description => "Prevent Frozen cards from transforming";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardModel), nameof(CardModel.IsTransformable), MethodType.Getter),
    ];

    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (__result && Frozen.IsAppliedTo(__instance))
        {
            __result = false;
        }
    }
}
