using System.Collections.Generic;
using System.Threading.Tasks;
using Illusionist.Scripts.Afflictions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts;

/// <summary>Shared combat-start behavior for cards with the Severe Cold keyword.</summary>
public static class SevereCold
{
    public static IEnumerable<IHoverTip> FrozenReminderHoverTips(CardModel card)
    {
        // Once Frozen has actually been applied, CardModel already appends the Affliction's own
        // hover tip. The reminder is only needed before combat and avoids a duplicate in combat.
        return Frozen.IsAppliedTo(card)
            ? []
            : HoverTipFactory.FromAffliction<Frozen>();
    }

    public static async Task ApplyAtCombatStart(CardModel card)
    {
        // BeforeCombatStart runs for both the persistent deck card and its combat copy. Only the
        // combat copy should be Frozen; changing the deck card would persist beyond this combat.
        if (card.Pile == null || !card.Pile.IsCombatPile || Frozen.IsAppliedTo(card))
        {
            return;
        }

        // A card can hold only one Affliction. Severe Cold guarantees Frozen for the combat copy,
        // while leaving the persistent deck card and its Affliction untouched.
        if (card.Affliction != null)
        {
            CardCmd.ClearAffliction(card);
        }

        await CardCmd.Afflict<Frozen>(card, 1);
    }
}
