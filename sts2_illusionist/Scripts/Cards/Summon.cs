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
/// 召唤 (SummonIllusionist) — 1 cost Skill, Uncommon (upgraded: Innate).
/// Choose a non-Power card in your draw pile; add a native Toxic to your hand, transmuted into a copy
/// of the chosen card (carrying its upgrades). A temporary copy you play this turn — it reverts to the
/// Toxic at end of turn (transmute stack), and a held Toxic costs 5 (blockable) HP. The chosen
/// draw-pile card is left untouched.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SUMMON")]
public sealed class SummonIllusionist : IllusionistCard
{

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
        HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Toxic>(),
    };

    public SummonIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;
        CardPile drawPile = PileType.Draw.GetPile(owner);
        if (!drawPile.Cards.Any(c => c.Type != CardType.Power))
        {
            return;
        }

        // 1) Choose a non-Power card from the draw pile.
        CardModel? chosen = (await CardSelectCmd.FromCombatPile(
            choiceContext, drawPile, owner,
            new CardSelectorPrefs(new LocString("cards", "ILLUSIONIST_CARD_SUMMON.selectionScreenPrompt"), 1),
            c => c.Type != CardType.Power)).FirstOrDefault();
        if (chosen == null)
        {
            return;
        }

        // 2) Add a native Toxic to hand and 幻化 it into a copy of the chosen card.
        CardModel toxic = base.CardScope!.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Toxic>(owner);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(toxic, PileType.Hand, owner);
        if (result.cardAdded == null)
        {
            return;
        }

        await Transmutation.TransmuteCards(new[] { result.cardAdded }, this, choiceContext, _ => chosen.CreateClone());
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
