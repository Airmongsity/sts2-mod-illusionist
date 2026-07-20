using System;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Patching.Core;
using Illusionist.Scripts.Patches;
using Illusionist.Scripts.Powers;

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
            // Mirror Image uses a short-lived card capability during AutoPlay so RitsuLib's result-pile
            // hook can send the flight animation back to the Mirror pile. Initializing the capability
            // host here avoids runtime "persistence has not been registered" failures without forcing
            // that transient capability to be a ModelDb-backed registered model.
            ModelCapabilities.EnsureInitialized();
            Logger.Info($"[{ModId}] RitsuLib model capability host initialized.");

            // Register the Mirror pile programmatically (not via [RegisterOwnedCardPile]) so it can supply
            // a VisibleWhen predicate: the Mirror pile button only shows for the Illusionist, not every
            // character. Attribute-driven registration can't pass a VisibleWhen delegate.
            MirrorPile.Register();
            Logger.Info($"[{ModId}] Mirror pile registered (Illusionist-only visibility).");
        }
        catch (Exception ex)
        {
            Logger.Error($"[{ModId}] Mirror pile registration FAILED: {ex}");
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
            // Re-enabled with step logging (SpineBody.BuildData + this patch) to pinpoint the rest-site
            // spine hang (07/13: hangs loading illusionist_rest.skel after a mirror-heavy combat).
            patcher.RegisterPatch<IllusionistRestSitePatch>();
            patcher.RegisterPatch<IllusionistShopPatch>();

            // Every patch above is non-critical: failures are logged per patch and skipped.
            patcher.PatchAll();
        }
        catch (Exception ex)
        {
            Logger.Error($"[{ModId}] Patcher setup FAILED: {ex}");
        }

        try
        {
            // 积蓄 (Accrue) reads a combat-cumulative "Block gained by any creature" total. The hidden
            // CombatBlockGainedCount power is applied to each player at combat start so it is live from
            // turn 1 (its AfterBlockGained fires for all creatures, like Shield Tax). The Apply is
            // fire-and-forget: it resolves within combat start, well before any Block is gained.
            RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(OnCombatStarting);
            Logger.Info($"[{ModId}] Combat-start lifecycle hook registered (block-gain counter).");

            RitsuLibFramework.SubscribeLifecycle<CardMovedBetweenPilesEvent>(MirrorImagePower.OnCardMovedBetweenPiles);
            Logger.Info($"[{ModId}] Card-pile lifecycle hook registered (mirror exhaust-source tracking).");

            RitsuLibFramework.SubscribeLifecycle<CardAutoPlayingEvent>(MirrorImagePower.OnCardAutoPlaying);
            Logger.Info($"[{ModId}] Card-autoplay lifecycle hook registered (mirror autoplay-source tracking).");
        }
        catch (Exception ex)
        {
            Logger.Error($"[{ModId}] Lifecycle subscribe FAILED: {ex}");
        }
    }

    private static void OnCombatStarting(CombatStartingEvent evt)
    {
        var combat = evt.CombatState;
        if (combat == null)
        {
            return;
        }

        _ = ApplyBlockGainCountersAsync(combat);
    }

    private static async Task ApplyBlockGainCountersAsync(ICombatState combat)
    {
        foreach (Player player in combat.Players)
        {
            try
            {
                await PowerCmd.Apply<CombatBlockGainedCount>(
                    new ThrowingPlayerChoiceContext(), player.Creature, 1, player.Creature, null);
            }
            catch (Exception ex)
            {
                Logger.Error($"[{ModId}] BlockGainCounter apply failed: {ex}");
            }
        }
    }
}
