using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 即兴 (ImproviseIllusionist) power — each turn, the first N cards you transmute are each auto-played
/// for free at a random enemy, where N is the number of Improvise stacks (<see cref="CardCmd.AutoPlay"/>
/// with a null target, the same call Hellraiser uses). Stacks via a Counter so the icon shows how many
/// auto-plays you get; playing Improvise again adds one. Risk/reward: you don't pick the target, but
/// it's free (cheats out expensive transmuted cards) and, as your first card played, the auto-play gets
/// copied by your mirror images. The per-turn count is bumped BEFORE the auto-play so a transmute
/// triggered by an auto-played card consumes a charge (and can chain) rather than looping unbounded.
/// </summary>
[RegisterPower]
public sealed class ImprovisePower : IllusionistPower
{
    private sealed class Data
    {
        public int TriggeredThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player.Creature)
        {
            GetInternalData<Data>().TriggeredThisTurn = 0;
        }

        return Task.CompletedTask;
    }

    /// <summary>Called by <see cref="Illusionist.Scripts.Transmutation"/> on each transmute.</summary>
    public async Task OnTransmuted(PlayerChoiceContext choiceContext, CardModel transformedCard)
    {
        // Safety net only: mirror-stored cards are EJECTED into the exhaust pile before their revert
        // notifies (so Improvise plays them normally); a pile-less card should never reach here, but
        // if one does, skip without burning a charge — AutoPlay can't play a card that isn't in combat.
        if (transformedCard.Pile == null)
        {
            return;
        }

        // Skip unplayable cards (curses, unplayable statuses). AutoPlay "plays" them by moving them
        // straight to their result pile (MoveToResultPileWithoutPlaying); a reverted curse sitting in a
        // mirror pile would be yanked out WITHOUT going through FireOne's slot management, leaving a ghost
        // loaded card and corrupting the mirror state (the next volley then hangs on the ghost). Mirrors
        // the 镜像 volley's own skip-AutoPlay for unplayable cards. Don't burn a charge on them.
        if (transformedCard.Keywords.Contains(CardKeyword.Unplayable))
        {
            return;
        }

        // Skip cards still sitting in a mirror pile. Revert transforms stored cards IN PLACE (the
        // reverted form lands back in the mirror), so auto-playing one would yank it out without FireOne's
        // slot management and corrupt the mirror state (ghost loaded card -> next volley hangs). Stored
        // reverted cards stay stored and fire next volley instead. Forward transmutes land in hand, so
        // they're unaffected.
        if (MirrorPile.IsMirrorPile(transformedCard.Pile))
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.TriggeredThisTurn >= base.Amount)
        {
            return;
        }

        // Bump before auto-playing so a transmute triggered by the auto-played card consumes its own
        // charge instead of re-firing unbounded — the first N transmutes each turn auto-play, no more.
        data.TriggeredThisTurn++;
        Flash();
        await CardCmd.AutoPlay(choiceContext, transformedCard, null);
    }
}
