using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using Illusionist.Scripts.Cards;
using Illusionist.Scripts.Relics;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Teach Touch of Orobas (the Ancient relic that upgrades your starter relic) about the
/// Illusionist's starter: map HallucinatoryLamp -> AncientLamp. Without this, the base
/// RefinementUpgrades map has no entry for the Lamp, so taking Touch of Orobas would replace it
/// with the useless Circlet fallback. The getter returns a fresh dictionary each call, so a postfix
/// can safely add our entry.
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), "RefinementUpgrades", MethodType.Getter)]
public static class TouchOfOrobasRefinementPatch
{
    private static void Postfix(ref Dictionary<ModelId, RelicModel> __result)
    {
        __result[ModelDb.Relic<HallucinatoryLamp>().Id] = ModelDb.Relic<AncientLamp>();
    }
}

/// <summary>
/// Teach Archaic Tooth (the Ancient relic that transcends a starter card) about the Illusionist:
/// map RiposteIllusionist -> SabotageIllusionist (破坏). Same private-static-getter postfix pattern as Touch of Orobas.
/// Without an entry the option simply isn't offered to the Illusionist (no trap), but we want it.
/// </summary>
[HarmonyPatch(typeof(ArchaicTooth), "TranscendenceUpgrades", MethodType.Getter)]
public static class ArchaicToothTranscendencePatch
{
    private static void Postfix(ref Dictionary<ModelId, CardModel> __result)
    {
        __result[ModelDb.Card<RiposteIllusionist>().Id] = ModelDb.Card<SabotageIllusionist>();
    }
}

/// <summary>
/// Base-game Darv ("the Hoarder") offers the Ancient relic DustyTome, whose SetupForPlayer picks a
/// random Ancient card from the player's pool. The Illusionist pool provides Ancient cards (Phantasm
/// Storm, SabotageIllusionist), so it should set up fine — but we keep this Finalizer as a safety net: if it
/// ever throws, swallow the exception so the call returns false and Darv simply doesn't offer
/// DustyTome instead of HANGING the ancient event.
/// </summary>
[HarmonyPatch(typeof(DustyTome), "SetupForPlayer")]
public static class DustyTomeSetupGuard
{
    private static System.Exception? Finalizer(System.Exception? __exception)
    {
        if (__exception != null)
        {
            MegaCrit.Sts2.Core.Logging.Log.Warn(
                "[illusionist] Suppressed DustyTome.SetupForPlayer exception so the Darv ancient event does not hang: " + __exception.Message);
        }

        return null;
    }
}
