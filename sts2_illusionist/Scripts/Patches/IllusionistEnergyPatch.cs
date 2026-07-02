using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Replace the borrowed Necrobinder energy orb with the Illusionist's own (<c>illusionist_energy_icon.webp</c>).
/// The pool now declares <c>Text/BigEnergyIconPath</c> (RitsuLib applies those to descriptions and
/// tooltips); these two patches cover the remaining spots: the card cost orb (kept as a belt-and-braces
/// override until Phase 2 confirms RitsuLib covers raw <see cref="CardModel"/>s) and the in-combat
/// energy counter scene, which is still Necrobinder's via the placeholder profile.
/// </summary>
public static class IllusionistEnergy
{
    private const string OrbPath = "res://illusionist/art/illusionist_energy_icon.webp";

    private static Texture2D? _orb;
    private static bool _tried;

    public static Texture2D? Orb()
    {
        if (_tried)
        {
            if (_orb != null && GodotObject.IsInstanceValid(_orb))
            {
                return _orb;
            }
            if (_orb == null)
            {
                return null;
            }
            _orb = null;
        }
        _tried = true;
        _orb = ResourceLoader.Load<Texture2D>(OrbPath);
        if (_orb == null)
        {
            Log.Error($"[illusionist] Energy: orb texture not found: {OrbPath}");
        }
        return _orb;
    }
}

/// <summary>Card cost orb (top-left of each card).</summary>
public sealed class IllusionistCardEnergyIconPatch : IPatchMethod
{
    public static string PatchId => "illusionist_card_energy_icon";

    public static string Description => "Use the Illusionist energy orb on Illusionist cards' cost orb";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(CardModel), nameof(CardModel.EnergyIcon), MethodType.Getter),
    };

    private static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance.Pool is not IllusionistCardPool)
        {
            return;
        }
        Texture2D? orb = IllusionistEnergy.Orb();
        if (orb != null)
        {
            __result = orb;
        }
    }
}

/// <summary>
/// In-combat energy counter (bottom-left). Sets the base orb layer to our icon and hides the borrowed
/// Necrobinder rotating layers + fire VFX, leaving the number label intact.
/// </summary>
public sealed class IllusionistEnergyCounterPatch : IPatchMethod
{
    public static string PatchId => "illusionist_energy_counter";

    public static string Description => "Reskin the borrowed Necrobinder combat energy counter";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(NEnergyCounter), "_Ready"),
    };

    private static void Postfix(NEnergyCounter __instance)
    {
        if (AccessTools.Field(typeof(NEnergyCounter), "_player").GetValue(__instance) is not Player player
            || player.Character is not global::Illusionist.Scripts.Characters.Illusionist)
        {
            return;
        }
        Texture2D? orb = IllusionistEnergy.Orb();
        if (orb == null)
        {
            return;
        }

        // %Layers holds the base orb TextureRect + RotationLayers (Control) + extra layer(s). Put our
        // orb on the first TextureRect, hide the rest so the Necrobinder layered orb doesn't show.
        if (__instance.GetNodeOrNull<Control>("%Layers") is Control layers)
        {
            bool baseSet = false;
            foreach (Node child in layers.GetChildren())
            {
                if (child is TextureRect rect)
                {
                    if (!baseSet)
                    {
                        rect.Texture = orb;
                        rect.Visible = true;
                        baseSet = true;
                    }
                    else
                    {
                        rect.Visible = false;
                    }
                }
                else if (child is CanvasItem ci) // RotationLayers and the like
                {
                    ci.Visible = false;
                }
            }
        }

        foreach (string vfx in new[] { "%EnergyVfxBack", "%EnergyVfxFront" })
        {
            if (__instance.GetNodeOrNull<Node2D>(vfx) is Node2D node)
            {
                node.Visible = false;
            }
        }
    }
}
