using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Replace the borrowed Necrobinder energy orb with the Illusionist's own (<c>illusionist_energy_icon.webp</c>)
/// in the two remaining spots: the card cost orb (top-left of each card) and the in-combat energy
/// counter (bottom-left). Both getters/nodes take a <see cref="Texture2D"/>, so we substitute the
/// runtime-loaded icon. The icon is shipped as an imported texture, so it loads via
/// <see cref="ResourceLoader"/>. Only the Illusionist's cards / player are affected.
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
            return _orb;
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
[HarmonyPatch(typeof(CardModel), nameof(CardModel.EnergyIcon), MethodType.Getter)]
public static class IllusionistCardEnergyIconPatch
{
    private static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance.Pool is not global::Illusionist.Scripts.IllusionistCardPool)
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
[HarmonyPatch(typeof(NEnergyCounter), "_Ready")]
public static class IllusionistEnergyCounterPatch
{
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
