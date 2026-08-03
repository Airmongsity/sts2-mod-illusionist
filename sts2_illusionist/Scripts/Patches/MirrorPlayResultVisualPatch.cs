using System;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Patching.Models;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Replaces the misleading mirror-result sequence "fly to Mirror, then burn away as Exhaust" with
/// one direct flight to the exhaust pile. The model still lands provisionally in the mirror pile so
/// post-play Exhaust removal remains generic; only the result animation is redirected here.
/// </summary>
public sealed class MirrorPlayResultVisualPatch : IPatchMethod
{
    public static string PatchId => "illusionist_mirror_play_result_visual";

    public static string Description => "Fly mirror-fired cards directly to their post-play destination";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(
            typeof(CardPileCmd),
            nameof(CardPileCmd.Add),
            new[]
            {
                typeof(CardModel),
                typeof(PileType),
                typeof(CardPilePosition),
                typeof(AbstractModel),
                typeof(bool),
            }),
    };

    private static void Prefix(CardModel card, PileType newPileType, ref bool skipVisuals)
    {
        if (skipVisuals || !MirrorImagePower.ShouldFlyDirectlyToExhaust(card, newPileType))
        {
            return;
        }

        try
        {
            NCombatRoom? room = NCombatRoom.Instance;
            NCard? cardNode = NCard.FindOnTable(card);
            Control? vfxContainer = card.Owner.Creature.GetVfxContainer();
            if (room == null || cardNode == null || vfxContainer == null)
            {
                return;
            }

            // Capture the displayed play-area position before taking the node out of the play queue.
            NCardFlyVfx? fly = NCardFlyVfx.Create(
                cardNode,
                PileType.Exhaust,
                isAddingToPile: true,
                card.Owner.Character.TrailPath);
            if (fly == null)
            {
                return;
            }

            Vector2 globalPosition = cardNode.GlobalPosition;
            NCardPlayQueue playQueue = room.Ui.PlayQueue;
            NPlayerHand hand = room.Ui.Hand;
            if (playQueue.IsAncestorOf(cardNode))
            {
                playQueue.RemoveCardFromQueueForExecution(card);
            }

            if (hand.IsAncestorOf(cardNode))
            {
                hand.Remove(card);
            }
            else
            {
                cardNode.GetParent()?.RemoveChildSafely(cardNode);
            }

            room.Ui.AddChildSafely(cardNode);
            cardNode.GlobalPosition = globalPosition;
            cardNode.PlayPileTween?.FastForwardToCompletion();

            vfxContainer.AddChildSafely(fly);
            MirrorImagePower.MarkDirectExhaustFlightStarted(card);

            // Let the model complete its provisional Play -> Mirror move without a second visual.
            // FireOne then runs CardCmd.Exhaust silently, preserving Exhaust history and hooks.
            skipVisuals = true;
        }
        catch (Exception ex)
        {
            // The normal Mirror -> native Exhaust visuals remain a safe fallback.
            Log.Error($"[illusionist] Mirror result direct-flight visual failed: {ex}");
        }
    }
}
