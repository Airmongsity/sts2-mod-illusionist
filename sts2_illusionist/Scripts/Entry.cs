using System;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using Illusionist.Scripts.Cards;
using Illusionist.Scripts.Potions;
using Illusionist.Scripts.Relics;

namespace Illusionist.Scripts;

[ModInitializer(nameof(Init))]
public static class Entry
{
    public const string ModId = "illusionist";

    public static void Init()
    {
        // Apply Harmony patches FIRST and independently, so that a failure in pool
        // registration can never prevent the reskin patches (starting deck / relic) from
        // being applied. Each step logs its own outcome for diagnosis.
        var harmony = new Harmony("com.airmongsity." + ModId);
        try
        {
            harmony.PatchAll();
            Log.Info($"[{ModId}] Harmony PatchAll OK: {harmony.GetPatchedMethods().Count()} method(s) patched.");
        }
        catch (Exception ex)
        {
            Log.Error($"[{ModId}] Harmony PatchAll FAILED: {ex}");
        }

        try
        {
            ModHelper.AddModelToPool<NecrobinderCardPool, Riposte>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Disrupt>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Reversal>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Counter>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Blind>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Foresight>();
            ModHelper.AddModelToPool<NecrobinderCardPool, MirrorImage>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Obscure>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Detonate>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Siphon>();
            ModHelper.AddModelToPool<NecrobinderCardPool, SilverLining>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Illusion>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Memory>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Ambush>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Echo>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Dim>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Conscript>();
            ModHelper.AddModelToPool<NecrobinderCardPool, HeavySlash>();
            ModHelper.AddModelToPool<NecrobinderCardPool, FateLoom>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Reshape>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Flicker>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Daze>();
            ModHelper.AddModelToPool<NecrobinderCardPool, LastStand>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Disillusion>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Dazzle>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Siege>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Unveil>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Kaleidoscope>();
            ModHelper.AddModelToPool<NecrobinderCardPool, PhaseShift>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Aging>();
            ModHelper.AddModelToPool<NecrobinderCardPool, Crescendo>();
            // Ancient-rarity (excluded from normal rewards). Registered so Darv's DustyTome can find
            // a valid Ancient card for the Illusionist; without one its picker NRE'd and hung Darv.
            ModHelper.AddModelToPool<NecrobinderCardPool, PhantasmStorm>();
            // RelicModel.Pool is non-virtual and throws if the relic is in no pool, so the
            // Hallucinatory Lamp MUST be registered into a relic pool or the character-select
            // screen crashes when rendering its description (which made embark fall back to Ironclad).
            ModHelper.AddModelToPool<NecrobinderPotionPool, IllusionPotion>();
            ModHelper.AddModelToPool<NecrobinderPotionPool, ForesightDraught>();
            ModHelper.AddModelToPool<NecrobinderRelicPool, HallucinatoryLamp>();
            ModHelper.AddModelToPool<NecrobinderRelicPool, PrismShard>();
            ModHelper.AddModelToPool<NecrobinderRelicPool, HeadStart>();
            ModHelper.AddModelToPool<NecrobinderRelicPool, UnbreakableMirror>();
            ModHelper.AddModelToPool<NecrobinderRelicPool, PristineMirror>();
            // AncientLamp is only obtained via the Touch of Orobas transform, never a reward (Starter
            // rarity is excluded from reward rolls), but it MUST be in a pool or RelicModel.Pool (a
            // non-virtual .First() lookup) throws when its description renders.
            ModHelper.AddModelToPool<NecrobinderRelicPool, AncientLamp>();
            Log.Info($"[{ModId}] Registered Reversal + Counter (cards) and HallucinatoryLamp (relic) into Necrobinder pools.");
        }
        catch (Exception ex)
        {
            Log.Error($"[{ModId}] AddModelToPool FAILED: {ex}");
        }
    }
}
