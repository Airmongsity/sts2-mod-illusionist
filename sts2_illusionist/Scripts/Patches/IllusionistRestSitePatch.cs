using System;
using System.Collections;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>Replace the borrowed campsite body with a seated pose, anchored to the native seat.
/// Native hitboxes, thought bubbles, player slots and parent mirroring remain in charge.</summary>
public sealed class IllusionistRestSitePatch : IPatchMethod
{
    /// <summary>Vertical nudge (local px, + = down). Tune to taste.</summary>
    private const float YOffset = 0f;

    private const string NodeName = "IllusionistRestSpine";

    public static string PatchId => "illusionist_rest_site_body";

    public static string Description => "Seat the Illusionist on the native campsite log";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(NRestSiteCharacter), "_Ready"),
    };

    private static void Postfix(NRestSiteCharacter __instance)
    {
        try
        {
            Log.Info("[illusionist] RestSite: _Ready postfix start");
            if (__instance.Player?.Character is not global::Illusionist.Scripts.Characters.Illusionist)
            {
                return;
            }
            if (__instance.GetNodeOrNull(NodeName) != null)
            {
                return;
            }

            // Load before hiding the borrowed body, so a missing asset leaves a visible fallback.
            Log.Info("[illusionist] RestSite: Load seated campsite pose...");
            Node2D? sprite = IllusionistRestPose.Create(Vector2.Zero,Mathf.Abs(__instance.GlobalScale.Y));
            Log.Info($"[illusionist] RestSite: seated pose -> {(sprite != null ? "ok" : "missing resource")}");
            if (sprite == null)
            {
                return;
            }

            // Hide the borrowed Necrobinder rest body via the private spine-node enumerator, and the
            // pet / fire nodes by name. The character spine is our anchor for placement.
            Node2D? anchor = __instance.GetNodeOrNull<Node2D>("Necro");
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

            Log.Info("[illusionist] RestSite: AddChild");
            __instance.AddChild(sprite);
            Vector2 anchorLocal = (anchor != null) ? anchor.Position : Vector2.Zero;
            Log.Info("[illusionist] RestSite: Place");
            sprite.Position=anchorLocal+new Vector2(0,YOffset);
            Log.Info("[illusionist] RestSite: postfix done (render loop takes over)");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] RestSite: seated pose failed: {ex}");
        }
    }
}
