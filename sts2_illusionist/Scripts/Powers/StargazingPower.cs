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
/// 观星 (Stargazing) power - a per-turn mini-mulligan. At the start of each of your turns (AFTER the
/// draw - <see cref="IllusionistPower.AfterPlayerTurnStart"/> fires after the turn-start draw), choose up
/// to 3 cards in hand, put them on the BOTTOM of your draw pile, then draw that many. Net hand size is
/// unchanged, but dead draws cycle into fresh ones (the cycled cards sink to the bottom of the draw
/// pile). Stacks (<see cref="PowerStackType.Counter"/>): each 观星 played adds <see cref="MaxCycle"/>
/// to the per-turn cycle cap, so 2 copies = choose up to 6. The upgrade (Innate on the card) puts it
/// online from turn 1.
/// </summary>
[RegisterPower]
public sealed class StargazingPower : IllusionistPower
{
    public const int MaxCycle = 3;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Dynamic: the live cycle cap (MaxCycle × stacks), not the raw stack count, so 2 观星 shows 6.
    public override int DisplayAmount => MaxCycle * (int)base.Amount;

    // Dynamic hover-tip: substitute the live cycle cap into the description ({CycleCap}).
    public override LocString Description
    {
        get
        {
            LocString loc = base.Description;
            loc.Add("CycleCap", MaxCycle * (int)base.Amount);
            return loc;
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature)
        {
            return;
        }

        // After the turn-start draw: choose up to (MaxCycle * stacks) hand cards to cycle to the bottom
        // of the draw pile, then draw that many. The player may select 0 to skip.
        CardPile hand = PileType.Hand.GetPile(player);
        if (hand.Cards.Count == 0)
        {
            return;
        }

        int maxCycle = MaxCycle * (int)base.Amount;
        List<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            // min=0, max=maxCycle => "up to maxCycle" (player may pick 0 to skip, or just 1-2).
            // The 2-arg ctor forces min=max and auto-selects all when hand < maxCycle.
            new CardSelectorPrefs(new LocString("cards", "ILLUSIONIST_CARD_STARGAZING.selectionScreenPrompt"), 0, maxCycle),
            null,
            this)).ToList();

        if (selected.Count == 0)
        {
            return;
        }

        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Bottom);
        }

        await CardPileCmd.Draw(choiceContext, selected.Count, player);
    }
}
