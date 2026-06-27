using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
namespace Illusionist.Scripts.Patches;

/// <summary>
/// Give the Illusionist a STATIC bitmap body (illusionist.jpg) instead of the borrowed Necrobinder
/// Spine skeleton — the same way Slay the Spire 1 draws its characters as flat images.
///
/// <para><see cref="CharacterModel.CreateVisuals"/> still returns Necrobinder's fully-wired
/// <see cref="NCreatureVisuals"/> scene (via the asset redirect), so every position marker the combat
/// system needs — <c>%Bounds</c>, <c>%IntentPos</c>, <c>%CenterPos</c> — stays correct. We just wait
/// for that scene's <c>_Ready</c> (which resolves those nodes), hide the Spine body node
/// (<c>%Visuals</c>), and overlay our own <see cref="Sprite2D"/>. The Spine body keeps "playing"
/// invisibly, so animation calls (<c>SpineAnimation.SetAnimation(...)</c>) and death-timing still work
/// — they just aren't seen.</para>
///
/// <para>If the image fails to load we do nothing and the Necrobinder body shows through, so this can
/// never crash the character.</para>
/// </summary>
[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
public static class IllusionistCombatBodyPatch
{
    /// <summary>Desired on-screen height of the character, in pixels. Tune to taste.</summary>
    private const float TargetHeightPx = 240f;

    /// <summary>Vertical nudge (local px, + = down) applied to the overlaid image. Tune to taste.</summary>
    private const float YOffset = 0f;

    private static void Postfix(CharacterModel __instance, NCreatureVisuals __result)
    {
        if (__instance is not global::Illusionist.Scripts.Characters.Illusionist || __result == null)
        {
            return;
        }

        ImageTexture? texture = IllusionistArt.CombatBody;
        if (texture == null)
        {
            // Couldn't load our art — leave the borrowed Necrobinder body in place.
            return;
        }

        // _Ready hasn't run yet (the node isn't in the tree), so %Bounds / the body node aren't
        // resolved. The shared overlay defers until it is.
        StaticBodyOverlay.ApplyOnReady(__result, texture, TargetHeightPx, YOffset, alpha: 1f, "IllusionistStaticBody");
    }
}
