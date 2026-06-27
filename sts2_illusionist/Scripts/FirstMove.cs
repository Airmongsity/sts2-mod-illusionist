using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Illusionist.Scripts;

/// <summary>
/// 先机 (First Move) — a reusable card condition: the bonus only triggers if the card is the
/// FIRST card the player has played this turn. Similar in spirit to Silent's 奇巧/Sly tag, but
/// gated on "first card of the turn" rather than being a passive marker.
///
/// MUST be called from inside a card's <c>OnPlay</c>. By that point the engine has already logged
/// this card's <c>CardPlayStarted</c> history entry (CardModel.OnPlayWrapper records it immediately
/// before awaiting OnPlay), so the count of this turn's original (first-in-series) card plays is
/// exactly 1 when this is the first card. Replays (Mirror Image / EchoIllusionist Form) are first-in-series
/// = false and don't count, so they don't consume "first move".
/// </summary>
public static class FirstMove
{
    /// <summary>True when the calling card is the first card its owner has played this turn.</summary>
    public static bool IsActive(Creature owner)
    {
        ICombatState? combat = owner.CombatState;
        if (combat == null)
        {
            return false;
        }

        return CombatManager.Instance.History.CardPlaysStarted
            .Count(e => e.Actor == owner && e.CardPlay.IsFirstInSeries && e.HappenedThisTurn(combat)) == 1;
    }
}
