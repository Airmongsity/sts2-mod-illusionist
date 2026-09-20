using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Illusionist.Scripts.Afflictions;

/// <summary>
/// At the start of its owner's turn, exhausts the afflicted card and transfers Scorching to a
/// seeded-random eligible card in the same pile. Playing the card removes Scorching instead.
/// </summary>
[RegisterAffliction]
public sealed class Scorching : ModAfflictionTemplate
{
    public override bool HasExtraCardText => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!HasCard || Card.Owner != player)
        {
            return;
        }

        CardModel source = Card;
        CardPile? sourcePile = source.Pile;
        if (sourcePile == null || sourcePile.Type == PileType.Exhaust)
        {
            return;
        }

        // The affliction transfers only once. Exhaust-pile cards still receive combat hooks, so it
        // must be cleared explicitly before exhausting the source.
        CardCmd.ClearAffliction(source);

        // Show the card before it burns: Scorching spreads inside whatever pile it is in, so the
        // card is usually in the draw or discard pile, where it has no on-screen node and the move
        // to the exhaust pile would be invisible.
        CardCmd.Preview(source);
        await CardCmd.Exhaust(choiceContext, source);

        Scorching scorching = ModelDb.Affliction<Scorching>();
        List<CardModel> candidates = sourcePile.Cards
            .Where(card => !ReferenceEquals(card, source) && scorching.CanAfflict(card))
            .ToList();
        CardModel? selected = player.RunState.Rng.CombatCardSelection.NextItem(candidates);
        if (selected != null)
        {
            // AfflictAndPreview, not Afflict: the fire jumping to a card the player cannot see is the
            // half that most needs to be shown.
            await CardCmd.AfflictAndPreview<Scorching>(new[] { selected }, 1);
        }
    }

    public override Task OnPlay(PlayerChoiceContext choiceContext, Creature? target)
    {
        CardCmd.ClearAffliction(Card);
        return Task.CompletedTask;
    }
}
