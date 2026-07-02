using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using Illusionist.Scripts.Patches;

namespace Illusionist.Scripts;

/// <summary>
/// RitsuLib bootstrap. Content (character, cards, relics, potions, starter deck, Orobas/Tooth
/// mappings) is registered declaratively via <c>[Register*]</c> attributes on each class — discovered
/// through <see cref="ModTypeDiscoveryHub.RegisterModAssembly"/>. The handful of Harmony patches we
/// still need go through RitsuLib's <see cref="ModPatcher"/> (per-patch failure isolation; a broken
/// patch logs and is skipped instead of taking the whole mod down).
/// </summary>
[ModInitializer(nameof(Init))]
public static class Entry
{
    public const string ModId = "illusionist";

    public static Logger Logger { get; private set; } = null!;

    public static void Init()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        Logger = RitsuLibFramework.CreateLogger(ModId);

        try
        {
            // Required for [RegisterCharacter]/[RegisterCard]/... attribute auto-registration.
            ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
            Logger.Info($"[{ModId}] Assembly registered for RitsuLib auto-registration.");
        }
        catch (Exception ex)
        {
            Logger.Error($"[{ModId}] RegisterModAssembly FAILED: {ex}");
        }

        try
        {
            ModPatcher patcher = RitsuLibFramework.CreatePatcher(ModId, "illusionist-patches");

            // Gameplay / UX patches that no library feature covers.
            patcher.RegisterPatch<IllusionistMirrorVisualPatch>();
            patcher.RegisterPatch<IllusionistMirrorRingPatch>();
            patcher.RegisterPatch<TransmuteRevertHoverPatch>();
            patcher.RegisterPatch<ArchitectDialogueFallbackPatch>();
            patcher.RegisterPatch<RunWonAchievementGuardPatch>();

            // Borrowed-asset reskins that need a live scene (no asset-profile equivalent).
            patcher.RegisterPatch<IllusionistEnergyCounterPatch>();
            patcher.RegisterPatch<IllusionistRestSitePatch>();
            patcher.RegisterPatch<IllusionistShopPatch>();

            // Every patch above is non-critical: failures are logged per patch and skipped.
            patcher.PatchAll();
        }
        catch (Exception ex)
        {
            Logger.Error($"[{ModId}] Patcher setup FAILED: {ex}");
        }
    }
}
