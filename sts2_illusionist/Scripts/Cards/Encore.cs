using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 返场 (EncoreIllusionist) — 1 cost Skill, Uncommon (upgraded: 0 cost).
/// Choose a non-Power card in the discard pile, leave it there, and add an Extinguished Lamp to hand transmuted
/// into a copy of the chosen card.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "ENCORE")]
public sealed class EncoreIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Transmute };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<ExtinguishedLampIllusionist>(),
    };

    public EncoreIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;
        CardPile discardPile = PileType.Discard.GetPile(owner);
        if (!discardPile.Cards.Any(card => card.Type != CardType.Power))
        {
            return;
        }

        CardModel? chosen = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            discardPile,
            owner,
            new CardSelectorPrefs(new LocString("cards", "ILLUSIONIST_CARD_ENCORE.selectionScreenPrompt"), 1),
            card => card.Type != CardType.Power))
            .FirstOrDefault();
        if (chosen == null)
        {
            return;
        }

        CardModel lamp = base.CardScope!.CreateCard<ExtinguishedLampIllusionist>(owner);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(lamp, PileType.Hand, owner);
        if (result.cardAdded == null)
        {
            return;
        }

        await Transmutation.TransmuteCards(
            new[] { result.cardAdded },
            this,
            choiceContext,
            _ => chosen.CreateClone());
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
