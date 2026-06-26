using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 即兴 (Improvise) power — the first time you transmute a card each turn, that transmuted card is
/// auto-played for free at a random enemy (<see cref="CardCmd.AutoPlay"/> with a null target, the
/// same call Hellraiser uses). Risk/reward: you don't pick the target, but it costs nothing (so it
/// cheats out expensive transmuted cards) and, as your first card played, it gets replayed by your
/// mirror images. Only the FIRST transmute each turn triggers (a per-turn flag, set before the
/// auto-play so a transmute inside the auto-played card can't re-trigger it).
/// </summary>
public sealed class ImprovisePower : PowerModel
{
    private sealed class Data
    {
        public bool TriggeredThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player.Creature)
        {
            GetInternalData<Data>().TriggeredThisTurn = false;
        }

        return Task.CompletedTask;
    }

    /// <summary>Called by <see cref="Illusionist.Scripts.Transmutation"/> on each transmute.</summary>
    public async Task OnTransmuted(PlayerChoiceContext choiceContext, CardModel transformedCard)
    {
        Data data = GetInternalData<Data>();
        if (data.TriggeredThisTurn)
        {
            return;
        }

        // Set before auto-playing so a transmute triggered by the auto-played card can't re-fire this.
        data.TriggeredThisTurn = true;
        Flash();
        await CardCmd.AutoPlay(choiceContext, transformedCard, null);
    }
}
