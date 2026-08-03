using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 大假面舞会 (Grand Masquerade) — 1-cost Rare Skill (upgraded: 0 cost).
/// Each other playable character contributes one signature card from the end of their starting deck
/// to a selection grid. The selected starter card identifies that character; every card remaining in
/// the Illusionist's hand is then transmuted into an independently random unlocked card from that
/// character's card pool.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "GRAND_MASQUERADE")]
public sealed class GrandMasqueradeIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        IllusionistKeywords.Transmute,
    };

    public GrandMasqueradeIllusionist()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;
        CharacterModel? selectedCharacter = await OtherCharacterCardSelection.Choose(
            choiceContext,
            owner,
            new LocString("cards", "ILLUSIONIST_CARD_GRAND_MASQUERADE.selectionScreenPrompt"));
        if (selectedCharacter == null)
        {
            return;
        }

        List<CardModel> randomPool = OtherCharacterCardSelection.GetUnlockedPool(owner, selectedCharacter);
        if (randomPool.Count == 0)
        {
            return;
        }

        // Snapshot only the cards currently in hand. Grand Masquerade itself is already in the play
        // pile, and cards drawn by transmute-triggered effects are intentionally not swept in later.
        List<CardModel> hand = PileType.Hand.GetPile(owner).Cards.ToList();
        await Transmutation.TransmuteCards(hand, this, choiceContext, _ =>
        {
            CardModel template = owner.RunState.Rng.CombatCardSelection.NextItem(randomPool)
                ?? randomPool[0];
            return base.CardScope!.CreateCard(template, owner);
        });
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}

/// <summary>
/// Shared deterministic character picker and unlocked-pool resolver for Grand Masquerade and Cameo.
/// The final distinct starter card represents each other playable character in the selection grid.
/// </summary>
internal static class OtherCharacterCardSelection
{
    internal static async Task<CharacterModel?> Choose(
        PlayerChoiceContext choiceContext,
        Player owner,
        LocString prompt)
    {
        List<(CharacterModel Character, CardModel Starter)> characterChoices = ModelDb.AllCharacters
            .Where(character => character.IsPlayable && character.Id != owner.Character.Id)
            .Select(character =>
            {
                CardModel? starter = character.StartingDeck
                    .DistinctBy(card => card.Id)
                    .LastOrDefault();
                return (Character: character, Starter: starter);
            })
            .Where(choice => choice.Starter != null)
            .Select(choice => (choice.Character, choice.Starter!))
            .ToList();

        if (characterChoices.Count == 0)
        {
            return null;
        }

        CardModel? selectedStarter = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            characterChoices.Select(choice => choice.Starter).ToList(),
            owner,
            new CardSelectorPrefs(prompt, 1))).FirstOrDefault();

        return selectedStarter == null
            ? null
            : characterChoices
                .FirstOrDefault(choice => choice.Starter.Id == selectedStarter.Id)
                .Character;
    }

    internal static List<CardModel> GetUnlockedPool(Player owner, CharacterModel character)
    {
        return character.CardPool
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
            .ToList();
    }
}
