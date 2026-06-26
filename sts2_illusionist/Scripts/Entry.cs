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
            ModHelper.AddModelToPool<IllusionistCardPool, Riposte>();
            ModHelper.AddModelToPool<IllusionistCardPool, Disrupt>();
            ModHelper.AddModelToPool<IllusionistCardPool, Reversal>();
            ModHelper.AddModelToPool<IllusionistCardPool, Counter>();
            ModHelper.AddModelToPool<IllusionistCardPool, Blind>();
            ModHelper.AddModelToPool<IllusionistCardPool, Foresight>();
            ModHelper.AddModelToPool<IllusionistCardPool, MirrorImage>();
            ModHelper.AddModelToPool<IllusionistCardPool, Obscure>();
            ModHelper.AddModelToPool<IllusionistCardPool, Detonate>();
            ModHelper.AddModelToPool<IllusionistCardPool, Siphon>();
            ModHelper.AddModelToPool<IllusionistCardPool, SilverLining>();
            ModHelper.AddModelToPool<IllusionistCardPool, Illusion>();
            ModHelper.AddModelToPool<IllusionistCardPool, Memory>();
            ModHelper.AddModelToPool<IllusionistCardPool, Ambush>();
            ModHelper.AddModelToPool<IllusionistCardPool, Echo>();
            ModHelper.AddModelToPool<IllusionistCardPool, Dim>();
            ModHelper.AddModelToPool<IllusionistCardPool, Conscript>();
            ModHelper.AddModelToPool<IllusionistCardPool, HeavySlash>();
            ModHelper.AddModelToPool<IllusionistCardPool, FateLoom>();
            ModHelper.AddModelToPool<IllusionistCardPool, Reshape>();
            ModHelper.AddModelToPool<IllusionistCardPool, Flicker>();
            ModHelper.AddModelToPool<IllusionistCardPool, Daze>();
            ModHelper.AddModelToPool<IllusionistCardPool, LastStand>();
            ModHelper.AddModelToPool<IllusionistCardPool, Disillusion>();
            ModHelper.AddModelToPool<IllusionistCardPool, Dazzle>();
            ModHelper.AddModelToPool<IllusionistCardPool, Siege>();
            ModHelper.AddModelToPool<IllusionistCardPool, Unveil>();
            ModHelper.AddModelToPool<IllusionistCardPool, Kaleidoscope>();
            ModHelper.AddModelToPool<IllusionistCardPool, PhaseShift>();
            ModHelper.AddModelToPool<IllusionistCardPool, Aging>();
            ModHelper.AddModelToPool<IllusionistCardPool, Crescendo>();
            ModHelper.AddModelToPool<IllusionistCardPool, Provoke>();
            ModHelper.AddModelToPool<IllusionistCardPool, Rekindle>();
            ModHelper.AddModelToPool<IllusionistCardPool, Encore>();
            ModHelper.AddModelToPool<IllusionistCardPool, Transmute>();
            ModHelper.AddModelToPool<IllusionistCardPool, Fluxweave>();
            ModHelper.AddModelToPool<IllusionistCardPool, ShiftingBlade>();
            ModHelper.AddModelToPool<IllusionistCardPool, MirrorWard>();
            ModHelper.AddModelToPool<IllusionistCardPool, PhantomBlast>();
            ModHelper.AddModelToPool<IllusionistCardPool, MyriadFaces>();
            ModHelper.AddModelToPool<IllusionistCardPool, Kindle>();
            ModHelper.AddModelToPool<IllusionistCardPool, Summon>();
            ModHelper.AddModelToPool<IllusionistCardPool, Improvise>();
            // Ancient-rarity (excluded from normal rewards). Registered so Darv's DustyTome can find
            // a valid Ancient card for the Illusionist; without one its picker NRE'd and hung Darv.
            ModHelper.AddModelToPool<IllusionistCardPool, PhantasmStorm>();
            // RelicModel.Pool is non-virtual and throws if the relic is in no pool, so the
            // Hallucinatory Lamp MUST be registered into a relic pool or the character-select
            // screen crashes when rendering its description (which made embark fall back to Ironclad).
            ModHelper.AddModelToPool<IllusionistPotionPool, IllusionPotion>();
            ModHelper.AddModelToPool<IllusionistPotionPool, ForesightDraught>();
            ModHelper.AddModelToPool<IllusionistRelicPool, HallucinatoryLamp>();
            ModHelper.AddModelToPool<IllusionistRelicPool, PrismShard>();
            ModHelper.AddModelToPool<IllusionistRelicPool, HeadStart>();
            ModHelper.AddModelToPool<IllusionistRelicPool, UnbreakableMirror>();
            ModHelper.AddModelToPool<IllusionistRelicPool, PristineMirror>();
            // AncientLamp is only obtained via the Touch of Orobas transform, never a reward (Starter
            // rarity is excluded from reward rolls), but it MUST be in a pool or RelicModel.Pool (a
            // non-virtual .First() lookup) throws when its description renders.
            ModHelper.AddModelToPool<IllusionistRelicPool, AncientLamp>();
            Log.Info($"[{ModId}] Registered Reversal + Counter (cards) and HallucinatoryLamp (relic) into Necrobinder pools.");
        }
        catch (Exception ex)
        {
            Log.Error($"[{ModId}] AddModelToPool FAILED: {ex}");
        }
    }
}
