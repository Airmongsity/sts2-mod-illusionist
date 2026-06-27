using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Give mirror clones the Illusionist's own static image, at 75% opacity, instead of the borrowed
/// Necrobinder Spine body. Same mechanism as the player's combat body
/// (<see cref="IllusionistCombatBodyPatch"/>) — hide the Spine, overlay a translucent sprite.
/// </summary>
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
public static class IllusionistMirrorVisualPatch
{
    /// <summary>On-screen height of a clone (matches the player body). Tune to taste.</summary>
    private const float TargetHeightPx = 240f;

    /// <summary>Vertical nudge for the clone image (local px, + = down).</summary>
    private const float YOffset = 0f;

    /// <summary>Clone opacity (0..1).</summary>
    private const float Alpha = 0.60f;

    private static void Postfix(MonsterModel __instance, NCreatureVisuals __result)
    {
        if (__instance is not MirrorClone || __result == null)
        {
            return;
        }

        ImageTexture? texture = IllusionistArt.CombatBody;
        if (texture == null)
        {
            return;
        }

        StaticBodyOverlay.ApplyOnReady(__result, texture, TargetHeightPx, YOffset, Alpha, "IllusionistMirrorBody");
    }
}

/// <summary>
/// Arrange a player's mirror clones in a RING around them instead of the engine's default horizontal
/// row. The base <c>NCombatRoom.AddCreature</c> re-lays-out every pet in a line each time a pet is
/// added; we postfix it and, whenever a mirror clone is added, move all that player's clones onto
/// stable points sampled in an annulus (ring) around the player — close enough to read as "around
/// you", never overlapping far away. Each clone keeps its sampled spot (cached per creature) so
/// existing clones don't jump when a new one is summoned.
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public static class IllusionistMirrorRingPatch
{
    // Ring shape, in the ally container's pixels. Tune to taste.
    private const float RingInnerPx = 70f;   // never closer than this to the player
    private const float RingOuterPx = 150f;  // never farther than this
    private const float RingYScale = 0.6f;    // flatten vertically → ground-plane ellipse
    private const float RingCenterYOffset = -50f; // raise the ring center toward the body's middle

    private static readonly ConditionalWeakTable<Creature, object> _offsets = new();
    private static readonly Random _rng = new();

    private static void Postfix(NCombatRoom __instance, Creature creature)
    {
        try
        {
            if (creature?.Monster is not MirrorClone)
            {
                return;
            }

            Player? owner = creature.PetOwner;
            if (owner == null)
            {
                return;
            }

            NCreature? ownerNode = __instance.GetCreatureNode(owner.Creature);
            ICombatState? combat = owner.Creature.CombatState;
            if (ownerNode == null || combat == null)
            {
                return;
            }

            Node parent = ownerNode.GetParent();
            if (parent == null)
            {
                return;
            }

            foreach (Creature clone in combat.Allies.Where(c => c.Monster is MirrorClone && c.PetOwner == owner && c.IsAlive))
            {
                NCreature? node = __instance.GetCreatureNode(clone);
                if (node == null || node.GetParent() != parent)
                {
                    continue;
                }

                node.Position = ownerNode.Position + OffsetFor(clone);

                // Depth-sort by reordering siblings within the ally container (it draws in child
                // order — later children on top — so this keeps every clone in the visible band).
                // With Godot's Y-down coords, a clone ABOVE the character (smaller Y) is farther back
                // and must render BEHIND it; a clone BELOW (larger Y) renders in FRONT. (In the user's
                // bottom-left-origin frame: clones with Y > the character go under it, Y < over it.)
                bool behindPlayer = node.Position.Y < ownerNode.Position.Y;
                int playerIndex = ownerNode.GetIndex();
                int cloneIndex = node.GetIndex();
                if (behindPlayer && cloneIndex > playerIndex)
                {
                    parent.MoveChild(node, playerIndex);   // drop it just before the player
                }
                else if (!behindPlayer && cloneIndex < playerIndex)
                {
                    parent.MoveChild(node, playerIndex);   // lift it just after the player
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorRing: layout failed: {ex}");
        }
    }

    /// <summary>A stable, area-uniform random point on the annulus for this clone.</summary>
    private static Vector2 OffsetFor(Creature clone)
    {
        if (_offsets.TryGetValue(clone, out object? boxed) && boxed is Vector2 cached)
        {
            return cached;
        }

        float angle = (float)(_rng.NextDouble() * Math.Tau);
        float r = Mathf.Sqrt(Mathf.Lerp(RingInnerPx * RingInnerPx, RingOuterPx * RingOuterPx, (float)_rng.NextDouble()));
        Vector2 offset = new Vector2(
            Mathf.Cos(angle) * r,
            Mathf.Sin(angle) * r * RingYScale + RingCenterYOffset);

        _offsets.Add(clone, offset);
        return offset;
    }
}
