using Illusionist.Scripts.Monsters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>Hide cosmetic mirror HP without changing targeting, powers, or Block displays.</summary>
public sealed class MirrorCloneHealthBarPatch : IPatchMethod
{
    public static string PatchId => "mirror_clone_hide_health_bar";
    public static string Description => "Hide the health bar and HP numbers of mirror clones";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new[]
    {
        new ModPatchTarget(typeof(NHealthBar), nameof(NHealthBar.SetCreature), new[] { typeof(Creature) })
    };

    private static void Postfix(NHealthBar __instance, Creature creature)
    {
        if (creature.Monster is MirrorClone)
        {
            // HP numbers are children of this container. Native refresh/hover animations
            // update its descendants, so hiding their parent survives those updates.
            __instance.HpBarContainer.Hide();
        }
    }
}
