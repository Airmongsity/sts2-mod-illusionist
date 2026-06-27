using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts;

/// <summary>
/// Shared helpers for the 幻化 (Transmute) system: temporarily transform cards in hand (reverting at
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
            c => c.IsTransformable,
            source)).ToList();

        await TransmuteCards(selection, source, choiceContext, makeReplacement);
    }

    /// <summary>
    /// 幻化 a card in hand into a COPY of <paramref name="source"/> — preserving its upgrades,
    /// enchantments, and temporary effects (<see cref="CardModel.CreateClone"/>). The signature
    /// "幻化为自己的复制品" move used by 变幻之刃 / 拟形之盾.
    /// </summary>
    public static Task TransmuteToCopyOf(CardModel source, PlayerChoiceContext choiceContext)
    {
        return TransmuteOneFromHand(source, choiceContext, _ => source.CreateClone());
    }

    /// <summary>
    /// 幻化 a specific set of cards (until end of turn). Skips un-transformable cards, registers the
    /// revert, and triggers Fluxweave per card. Used by 千面 / Myriad Faces to reshape a whole hand.
    /// </summary>
    public static async Task TransmuteCards(IEnumerable<CardModel> originals, CardModel source, PlayerChoiceContext choiceContext, Func<CardModel, CardModel> makeReplacement)
    {
        // Snapshot up front: transforming (and the Fluxweave draws it triggers) mutates the piles.
        List<CardModel> targets = originals.Where(c => c.IsTransformable).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        Player owner = source.Owner;
        TransmutePower revert = await EnsureRevertPower(owner, choiceContext, source);

        foreach (CardModel original in targets)
        {
            CardModel replacement = makeReplacement(original);
            CardPileAddResult? result = await CardCmd.Transform(original, replacement);
            if (result == null || result.Value.cardAdded == null)
            {
                continue;
            }

            revert.RegisterTransmute(original, result.Value.cardAdded);
            await NotifyTransformed(owner, choiceContext, result.Value.cardAdded);
        }
    }

    /// <summary>
    /// Call whenever a card is transmuted (幻化), passing the resulting (transformed) card. The single
    /// choke point for transmute payoffs: Fluxweave draws, and Improvise auto-plays the first transmute
    /// of the turn at a random enemy.
    /// </summary>
    public static async Task NotifyTransformed(Player player, PlayerChoiceContext choiceContext, CardModel transformedCard)
    {
        if (player.Creature.GetPower<FluxweavePower>() != null)
        {
            await CardPileCmd.Draw(choiceContext, 1m, player);
        }

        ImprovisePower? improvise = player.Creature.GetPower<ImprovisePower>();
        if (improvise != null)
        {
            await improvise.OnTransmuted(choiceContext, transformedCard);
        }
    }

    /// <summary>
    /// Register that <paramref name="token"/> (a card already placed in a combat pile) reverts into
    /// <paramref name="revertTo"/> at the start of the owner's next turn. Use for "the played card
    /// turned into a token; bring its form back next turn" — where the played card has already left
    /// play (e.g. removed via a <see cref="PileType.None"/> result pile) so we DON'T transform the
    /// in-play card (which hangs — no base card self-transforms on play).
    /// </summary>
    public static async Task RegisterRevert(Player owner, PlayerChoiceContext choiceContext, CardModel source, CardModel revertTo, CardModel token)
    {
        TransmutePower revert = await EnsureRevertPower(owner, choiceContext, source);
        revert.RegisterTransmute(revertTo, token);
    }

    /// <summary>One shared revert power per turn; created lazily on the first transmute.</summary>
    private static async Task<TransmutePower> EnsureRevertPower(Player owner, PlayerChoiceContext choiceContext, CardModel source)
    {
        TransmutePower? revert = owner.Creature.GetPower<TransmutePower>();
        if (revert == null)
        {
            await PowerCmd.Apply<TransmutePower>(choiceContext, owner.Creature, 1, owner.Creature, source);
            revert = owner.Creature.GetPower<TransmutePower>();
        }

        return revert!;
    }
}
