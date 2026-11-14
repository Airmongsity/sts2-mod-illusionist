using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Fake Merchant creates combat visuals and directly requests relaxed_loop. Use the character's
/// relaxed pose at event scale, without combat orbit effects.
/// Scope by the character factory's marker, not the inherited Necrobinder scene or player order.
/// </summary>
public sealed class IllusionistFakeMerchantPatch : IPatchMethod
{
    public static string PatchId => "illusionist_fake_merchant_animation";
    public static string Description => "Use an existing Illusionist animation in Fake Merchant";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(NFakeMerchant), "StartCharacterAnimation"),
    };

    private static bool Prefix(NCreatureVisuals visuals)
    {
        if (!visuals.HasMeta(SpineBody.CharacterVisualMeta))
        {
            return true;
        }
        // AddChildSafely may defer insertion. Wait for the existing Spine swap Ready handler first.
        if (!visuals.IsNodeReady())
        {
            visuals.Ready += () => Play(visuals);
            return false;
        }
        return !Play(visuals);
    }

    private static bool Play(NCreatureVisuals visuals)
    {
        try
        {
            return SpineBody.TryPlayRelaxed(visuals);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Fake Merchant animation failed: {ex}");
            return false;
        }
    }
}
