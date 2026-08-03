using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 预见 (ForesightIllusionist) — 1-cost Uncommon Skill. Choose 3 cards from the draw and discard
/// piles and put them on top of the draw pile. At the start of next turn, gain 1 Energy.
/// Upgraded: choose 5 cards.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "FORESIGHT")]
public sealed class ForesightIllusionist : IllusionistCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(3),
    };

    public ForesightIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardPile drawPile = PileType.Draw.GetPile(base.Owner);
        CardPile discardPile = PileType.Discard.GetPile(base.Owner);
        List<CardModel> candidates = drawPile.Cards
            .Concat(discardPile.Cards)
            .ToList();

        int pickCount = System.Math.Min(base.DynamicVars.Cards.IntValue, candidates.Count);
        if (pickCount > 0)
        {
            List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                base.Owner,
                new CardSelectorPrefs(
                    new LocString("cards", "ILLUSIONIST_CARD_FORESIGHT.selectionScreenPrompt"),
                    pickCount))).ToList();

            // Reverse so the first selected card ends on top after repeated top insertions.
            selected.Reverse();
            foreach (CardModel card in selected)
            {
                await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
            }
        }

        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(2m);
    }
}
