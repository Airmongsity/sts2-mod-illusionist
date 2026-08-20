using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Makes the mirror clone's visual match the owner's character instead of hardcoding Necrobinder.
/// Patches <see cref="MirrorClone.VisualsPath"/> getter (via <c>get_VisualsPath</c>) to return the
/// owner's character visual path. For the Illusionist, the original Necrobinder path is kept so that
/// <see cref="IllusionistMirrorVisualPatch"/> can swap to the Illusionist's Spine skeleton.
/// Supports both base-game characters (via the standard path pattern) and mod characters (via
/// <see cref="IModCharacterAssetOverrides.CustomVisualsPath"/>).
/// </summary>
public sealed class MirrorCloneVisualsPatch : IPatchMethod
{
    private static readonly FieldInfo CreatureField =
        typeof(MonsterModel).GetField("_creature", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static string PatchId => "mirror_clone_visuals_path";

    public static string Description => "Show mirror clone as the owner's character instead of Necrobinder";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(MirrorClone), "get_VisualsPath"),
    };

    private static bool Prefix(ref string __result, MirrorClone __instance)
    {
        // Get the creature that owns this monster model, and its owner player.
        Creature? creature = CreatureField.GetValue(__instance) as Creature;
        Player? owner = creature?.PetOwner;
        if (owner == null)
        {
            return true; // fall through to original (Necrobinder)
        }

        CharacterModel character = owner.Character;

        // The Illusionist keeps the Necrobinder path — IllusionistMirrorVisualPatch swaps to Spine.
        if (character is global::Illusionist.Scripts.Characters.Illusionist)
        {
            return true; // fall through to original (Necrobinder)
        }

        // Mod characters with a custom visual path: use their override.
        if (character is IModCharacterAssetOverrides overrides && overrides.CustomVisualsPath is { } customPath)
        {
            __result = customPath;
            return false; // skip the original getter
        }

        // Base-game characters: use the standard path pattern.
        __result = SceneHelper.GetScenePath("creature_visuals/" + character.Id.Entry.ToLowerInvariant());
        return false; // skip the original getter
    }
}
