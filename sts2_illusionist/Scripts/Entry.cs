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
            ModHelper.AddModelToPool<IllusionistCardPool, IllusionistStrikeIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, IllusionistDefendIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, RiposteIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, PhantomVenomIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, FeintIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, DisruptIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ReversalIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, CounterIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, BlindIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ForesightIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, MirrorImageIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ObscureIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, DetonateIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, SiphonIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, SilverLiningIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, MemoryIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, AmbushIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, EchoIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ConscriptIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, HeavySlashIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, FateLoomIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ReshapeIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, FlickerIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, DazeIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, LastStandIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, DisillusionIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, DazzleIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, SiegeIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, UnveilIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, KaleidoscopeIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, PhaseShiftIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, AgingIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, CrescendoIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ProvokeIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, RekindleIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, EncoreIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, TransmuteIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, FluxweaveIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ShiftingBladeIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, MirrorWardIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, MyriadFacesIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, KindleIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, SummonIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ImproviseIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, MetamorphosisIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ChannelIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ReckoningIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, MomentumIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ForewarnIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, AccrueIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ShiftingWaveIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, SacrificeIllusionist>();
            ModHelper.AddModelToPool<IllusionistCardPool, ExtractIllusionist>();
            // Ancient-rarity (excluded from normal rewards). Registered so Darv's DustyTome can find
            // a valid Ancient card for the Illusionist; without one its picker NRE'd and hung Darv.
            ModHelper.AddModelToPool<IllusionistCardPool, PhantasmStormIllusionist>();
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
            Log.Info($"[{ModId}] Registered ReversalIllusionist + CounterIllusionist (cards) and HallucinatoryLamp (relic) into Necrobinder pools.");
        }
        catch (Exception ex)
        {
            Log.Error($"[{ModId}] AddModelToPool FAILED: {ex}");
        }
    }
}
