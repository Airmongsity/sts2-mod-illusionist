using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

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
public sealed class ImprovisePower : PowerModel
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
