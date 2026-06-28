using System;
using System.Collections;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Give the Illusionist its own Spine body at the rest site (a separate skeleton from combat:
/// <c>illusionist_rest.skel</c>). The rest character is a dedicated scene
/// (<c>rest_site/characters/&lt;id&gt;_rest_site</c>, redirected to Necrobinder's), so unlike combat we
/// don't have a creature-visuals hook — we postfix <see cref="NRestSiteCharacter"/>'s <c>_Ready</c>,
/// hide the borrowed Necrobinder rest body and its pet/fire, and overlay our own <c>SpineSprite</c>.
///
/// <para>The borrowed scene's spine bodies are found via the private <c>GetChildSpineNodes()</c>
/// (reflection) and hidden; the pet anchors (<c>Osty</c> / <c>OstyRightAnchor</c>) and fire
/// (<c>%NecroFire</c> / <c>%OstyFire</c>) are hidden by name. Placement reuses
/// <see cref="SpineBody.Place"/>; tune <see cref="TargetHeightPx"/> / <see cref="YOffset"/> in-game.</para>
/// </summary>
[HarmonyPatch(typeof(NRestSiteCharacter), "_Ready")]
public static class IllusionistRestSitePatch
{
    /// <summary>On-screen height of the rest-site body, in pixels. Tune to taste.</summary>
    private const float TargetHeightPx = 320f;

    /// <summary>Vertical nudge (local px, + = down). Tune to taste.</summary>
    private const float YOffset = 0f;

    private const string NodeName = "IllusionistRestSpine";

    private static void Postfix(NRestSiteCharacter __instance)
    {
        try
        {
            if (__instance.Player?.Character is not global::Illusionist.Scripts.Characters.Illusionist)
            {
                return;
            }
            if (__instance.GetNodeOrNull(NodeName) != null)
            {
                return;
            }

            // Build our sprite FIRST; if Spine is unavailable, leave the borrowed body untouched.
            Node2D? sprite = SpineBody.CreateSprite(SpineBody.RestSkel, alpha: 1f, NodeName);
            if (sprite == null)
            {
                return;
            }

            // Hide the borrowed Necrobinder rest body via the private spine-node enumerator, and the
            // pet / fire nodes by name. The character spine is our anchor for placement.
            Node2D? anchor = null;
            var method = AccessTools.Method(typeof(NRestSiteCharacter), "GetChildSpineNodes");
            if (method?.Invoke(__instance, null) is IEnumerable spineNodes)
            {
                foreach (object? n in spineNodes)
                {
                    if (n is Node2D nd)
                    {
                        anchor ??= nd;
                        nd.Visible = false;
                    }
                }
            }
            foreach (string name in new[] { "Necro", "Osty", "OstyRightAnchor", "%NecroFire", "%OstyFire" })
            {
                if (__instance.GetNodeOrNull<Node2D>(name) is Node2D extra)
                {
                    if (name == "Necro")
                    {
                        anchor ??= extra;
                    }
                    extra.Visible = false;
                }
            }

            __instance.AddChild(sprite);
            Vector2 anchorLocal = (anchor != null) ? anchor.Position : Vector2.Zero;
            SpineBody.Place(sprite, SpineBody.RestSkel, anchorLocal, TargetHeightPx, Mathf.Abs(__instance.GlobalScale.Y), YOffset);
            SpineBody.Play(sprite, SpineBody.RestSkel);   // start idle AFTER it's in the tree
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] RestSite: spine apply failed: {ex}");
        }
    }
}
