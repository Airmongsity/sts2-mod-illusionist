using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Relics;

/// <summary>
/// 抢先 (Head Start) — Common. The first card you play each turn costs 1 less Energy.
/// Implemented via the cost-modify hook: while no first-in-series card has been played this turn,
/// every card in hand shows 1 cheaper (down to 0); once you play one, the discount ends.
/// </summary>
[RegisterRelic(typeof(IllusionistRelicPool))]
public sealed class HeadStart : IllusionistRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    // Placeholder art until Head Start has its own.
    protected override string IconBaseName => "bookmark";

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        var owner = base.Owner;
        ICombatState? combat = owner?.Creature.CombatState;
        if (owner == null || combat == null || originalCost <= 0m)
        {
            return false;
        }

        // In multiplayer this cost hook fires for EVERY player's cards, so only discount the boots
        // owner's own cards — otherwise the relic would cheapen teammates' first card too.
        if (card.Owner != owner)
        {
            return false;
        }

        // Count this turn's original (first-in-series) plays. While 0, the next card is "the first".
        int playedThisTurn = CombatManager.Instance.History.CardPlaysStarted.Count(
            e => e.Actor == owner.Creature && e.CardPlay.IsFirstInSeries && e.HappenedThisTurn(combat));
        if (playedThisTurn > 0)
        {
            return false;
        }

        modifiedCost = originalCost - 1m;
        return true;
    }
}
