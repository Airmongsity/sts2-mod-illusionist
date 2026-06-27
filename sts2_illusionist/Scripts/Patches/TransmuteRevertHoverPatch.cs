using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Show players what a transmuted card reverts to. A card in a 幻化 chain displays no hint of its
/// underlying form, so we append the revert target's card hover-tip to <see cref="CardModel.HoverTips"/>
/// — the same way 点灯 / 召唤 (KindleIllusionist/SummonIllusionist) surface their Dim Lamp / Beckon via a card hover.
///
/// Only fires for a card that <see cref="TransmutePower"/> tracks; for every other card (and other
/// characters) <c>GetPower</c> returns null and this is a no-op.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
public static class TransmuteRevertHoverPatch
{
    private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        // Canonical (template) cards — e.g. a card previewed inside a relic's hover (CursedPearl) —
        // throw CanonicalModelException on .Owner. They're never transmuted, so skip them.
        if (__instance.IsCanonical)
        {
            return;
        }

        Player? owner = __instance.Owner;
        if (owner?.Creature == null)
        {
            return;
        }

        TransmutePower? power = owner.Creature.GetPower<TransmutePower>();
        CardModel? target = power?.GetRevertTarget(__instance);
        if (target == null)
        {
            return;
        }

        __result = __result.Concat(new IHoverTip[] { HoverTipFactory.FromCard(target) });
    }
}
