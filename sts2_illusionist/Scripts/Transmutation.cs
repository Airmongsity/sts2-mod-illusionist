using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts.Afflictions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts.Cards;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts;

/// <summary>
/// Shared helpers for the 幻形 (TransmuteIllusionist) system: temporarily transform cards in hand (reverting at
/// end of turn if unplayed, via <see cref="TransmutePower"/>). Every transmute also pings
/// <see cref="FluxweavePower"/>, so "draw on transform" lives in one place.
/// </summary>
public static class Transmutation
{
    /// <summary>
    /// Inline-pick one transformable card in hand (the Armaments-style picker) and 幻化 it into the
    /// card produced by <paramref name="makeReplacement"/>, until end of turn. Reverts if unplayed.
    /// </summary>
    public static async Task TransmuteOneFromHand(CardModel source, PlayerChoiceContext choiceContext, Func<CardModel, CardModel> makeReplacement)
    {
        List<CardModel> selection = (await CardSelectCmd.FromHand(
            choiceContext, source.Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
            CanTransmute,
            source)).ToList();

        await TransmuteCards(selection, source, choiceContext, makeReplacement);
    }

    /// <summary>
    /// 幻化 a card in hand into a COPY of <paramref name="source"/> — preserving its upgrades,
    /// enchantments, and temporary effects (<see cref="CardModel.CreateClone"/>). The signature
    /// "幻化为自己的复制品" move used by 变幻之刃 / 拟形之盾.
    /// </summary>
    public static Task TransmuteToNonExhaustCopyOf(CardModel source, PlayerChoiceContext choiceContext)
    {
        return TransmuteOneFromHand(source, choiceContext, _ =>
        {
            CardModel replacement = source.CreateClone();
            replacement.RemoveKeyword(CardKeyword.Exhaust);
            return replacement;
        });
    }

    /// <summary>
    /// 幻化 a specific set of cards (until end of turn). Skips un-transformable cards, registers the
    /// revert, and triggers FluxweaveIllusionist per card. Used by 千面 / Myriad Faces to reshape a whole hand.
    /// </summary>
    public static async Task<int> TransmuteCards(IEnumerable<CardModel> originals, CardModel source, PlayerChoiceContext choiceContext, Func<CardModel, CardModel> makeReplacement)
    {
        // Snapshot up front: transforming (and the FluxweaveIllusionist draws it triggers) mutates the piles.
        List<CardModel> targets = originals.Where(CanTransmute).ToList();
        if (targets.Count == 0)
        {
            return 0;
        }

        Player owner = source.Owner;
        TransmutePower? revert = await EnsureRevertPower(owner, choiceContext, source);
        if (revert == null)
        {
            return 0;
        }

        int transformed = 0;
        foreach (CardModel original in targets)
        {
            // Recheck immediately before the transform. The selection snapshot may be followed by
            // awaited effects, and Frozen must remain an absolute guard at the execution choke point.
            if (!CanTransmute(original))
            {
                continue;
            }

            CardModel replacement = makeReplacement(original);

            CardPileAddResult? result = await CardCmd.Transform(original, replacement);
            if (result == null || result.Value.cardAdded == null)
            {
                continue;
            }

            CardModel added = result.Value.cardAdded;
            revert.RegisterTransmute(original, added);
            // A stored (in-mirror) card that gets transmuted must keep its mirror pointing at the new form.
            await MirrorImagePower.OnCardTransformed(owner, original, added);
            await NotifyTransformed(owner, choiceContext, added);
            transformed++;

            // 长明灯 (Everlit Lamp): once per turn, the first 熄灭油灯 (Extinguished Lamp) to APPEAR in
            // hand is 幻化-ed into a 暗淡油灯 (Dim Lamp) - a REAL two-step chain (X -> 熄灭油灯 -> 暗淡油灯),
            // not an intercepted swap. The 熄灭油灯 is genuinely created first, then 幻化-ed. This fires on the
            // forward-幻化 path only (Disillusion/Douse/Riposte/PhantomVenom turning a HAND card into an
            // 熄灭油灯); the recursive call below yields a 暗淡油灯 (not an 熄灭油灯), so it terminates. Drawn
            // lamps are handled in EverlitLampPower.AfterCardDrawn; reverts run through
            // TransmutePower.RevertOneLayer (not here), so they can't re-trigger and loop. Hand-only.
            if (added is ExtinguishedLampIllusionist
                && added.Pile?.Type == PileType.Hand
                && owner.Creature.GetPower<EverlitLampPower>() is { } everlit)
            {
                await everlit.TryTransmuteLampInHand(choiceContext, added);
            }
        }

        return transformed;
    }

    /// <summary>
    /// The authoritative forward-transmutation guard. Do not rely only on the patched
    /// CardModel.IsTransformable getter: small getters can be inlined by runtime call sites.
    /// </summary>
    private static bool CanTransmute(CardModel card)
    {
        return card.IsTransformable && !Frozen.IsAppliedTo(card);
    }

    /// <summary>
    /// Call whenever a card is transmuted (幻化), passing the resulting (transformed) card. The single
    /// choke point for transmute payoffs: FluxweaveIllusionist draws, and ImproviseIllusionist auto-plays the first transmute
    /// of the turn at a random enemy.
    /// </summary>
    public static async Task NotifyTransformed(Player player, PlayerChoiceContext choiceContext, CardModel transformedCard)
    {
        // Tally this transform toward "cards transformed this turn" — counting both the forward
        // transmutes you make and the turn-start reverts (both reach this choke point). Read by
        // finishers like 嬗变 / Metamorphosis. The counter is a hidden, lazily-created power.
        TransformCountPower? counter = player.Creature.GetPower<TransformCountPower>();
        if (counter == null)
        {
            counter = await PowerCmd.Apply<TransformCountPower>(choiceContext, player.Creature, 1, player.Creature, null);
        }
        counter?.Increment();

        CombatTransformCount? totalCounter = player.Creature.GetPower<CombatTransformCount>();
        if (totalCounter == null)
        {
            totalCounter = await PowerCmd.Apply<CombatTransformCount>(choiceContext, player.Creature, 1, player.Creature, null);
        }
        totalCounter?.Increment();

        // Each 流变 is its own instance and settles separately, so advance every live copy.
        foreach (FluxweavePower flux in player.Creature.GetPowerInstances<FluxweavePower>().ToList())
        {
            await flux.OnTransform(choiceContext);
        }

        // 障眼法 (Misdirection): every card change this turn grants Block. Reverts deliberately
        // travel through this same notification point, so each reverted layer triggers separately.
        MisdirectionPower? misdirection = player.Creature.GetPower<MisdirectionPower>();
        if (misdirection != null)
        {
            await misdirection.OnCardChanged();
        }

        ImprovisePower? improvise = player.Creature.GetPower<ImprovisePower>();
        if (improvise != null)
        {
            await improvise.OnTransmuted(choiceContext, transformedCard);
        }

        foreach (MomentumPower momentum in player.Creature.GetPowerInstances<MomentumPower>())
        {
            await momentum.OnTransform();
        }

        // 折光 (Refraction): every transform fires a bolt at a random enemy.
        RefractionPower? refraction = player.Creature.GetPower<RefractionPower>();
        if (refraction != null)
        {
            await refraction.OnTransform(choiceContext);
        }
    }

    /// <summary>
    /// Register that <paramref name="token"/> (a card already placed in a combat pile) reverts into
    /// <paramref name="revertTo"/> at the start of the owner's next turn. Use for "the played card
    /// turned into a token; bring its form back next turn" — where the played card has already left
    /// play (e.g. removed via a <see cref="PileType.None"/> result pile) so we DON'T transform the
    /// in-play card (which hangs — no base card self-transforms on play).
    /// </summary>
    public static async Task<bool> RegisterRevert(Player owner, PlayerChoiceContext choiceContext, CardModel source, CardModel revertTo, CardModel token)
    {
        TransmutePower? revert = await EnsureRevertPower(owner, choiceContext, source);
        if (revert == null)
        {
            return false;
        }

        revert.RegisterTransmute(revertTo, token);
        return true;
    }

    /// <summary>One shared revert power per turn; created lazily on the first transmute.</summary>
    private static async Task<TransmutePower?> EnsureRevertPower(Player owner, PlayerChoiceContext choiceContext, CardModel source)
    {
        TransmutePower? revert = owner.Creature.GetPower<TransmutePower>();
        if (revert == null)
        {
            revert = await PowerCmd.Apply<TransmutePower>(choiceContext, owner.Creature, 1, owner.Creature, source);
        }

        return revert;
    }
}
