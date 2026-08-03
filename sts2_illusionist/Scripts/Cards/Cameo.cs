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
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 客串 (Cameo) — 0-cost Uncommon Skill. Choose another playable character, then transmute one
/// transformable hand card into a seeded-random unlocked card from that character. The hand card is
/// random before upgrade and player-selected after upgrade.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "CAMEO")]
public sealed class CameoIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        IllusionistKeywords.Transmute,
    };

    public CameoIllusionist()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;
        if (!PileType.Hand.GetPile(owner).Cards.Any(card => card.IsTransformable))
        {
            return;
        }

        CharacterModel? selectedCharacter = await OtherCharacterCardSelection.Choose(
            choiceContext,
            owner,
            new LocString("cards", "ILLUSIONIST_CARD_CAMEO.characterSelectionScreenPrompt"));
        if (selectedCharacter == null)
        {
            return;
        }

        List<CardModel> randomPool = OtherCharacterCardSelection.GetUnlockedPool(owner, selectedCharacter);
        if (randomPool.Count == 0)
        {
            return;
        }

        CardModel? chosen;
        if (base.IsUpgraded)
        {
            chosen = (await CardSelectCmd.FromHand(
                choiceContext,
                owner,
                new CardSelectorPrefs(
                    new LocString("cards", "ILLUSIONIST_CARD_CAMEO.cardSelectionScreenPrompt"),
                    1),
                card => card.IsTransformable,
                this)).FirstOrDefault();
        }
        else
        {
            List<CardModel> eligible = PileType.Hand.GetPile(owner).Cards
                .Where(card => card.IsTransformable)
                .ToList();
            chosen = owner.RunState.Rng.CombatCardSelection.NextItem(eligible);
        }

        if (chosen == null)
        {
            return;
        }

        CardModel? template = owner.RunState.Rng.CombatCardSelection.NextItem(randomPool);
        if (template == null)
        {
            return;
        }

        await Transmutation.TransmuteCards(
            new[] { chosen },
            this,
            choiceContext,
            _ => base.CardScope!.CreateCard(template, owner));
    }

    protected override void OnUpgrade()
    {
        // Upgrade changes the hand-card target from seeded random to player-selected.
    }
}
