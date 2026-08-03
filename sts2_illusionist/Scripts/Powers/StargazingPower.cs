using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 候场 (keeps the StargazingPower model identity). After the normal turn-start hand draw, draw one
/// card per stack, then move the same number of chosen hand cards to the top of the draw pile.
/// </summary>
[RegisterPower]
public sealed class StargazingPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature)
        {
            return;
        }

        int amount = (int)base.Amount;
        if (amount <= 0)
        {
            return;
        }

        Flash();
        await CardPileCmd.Draw(choiceContext, amount, player);

        CardPile hand = PileType.Hand.GetPile(player);
        int putBackCount = System.Math.Min(amount, hand.Cards.Count);
        if (putBackCount == 0)
        {
            return;
        }

        List<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(
                new LocString("cards", "ILLUSIONIST_CARD_STARGAZING.selectionScreenPrompt"),
                putBackCount),
            null,
            this)).ToList();

        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        }
    }
}
