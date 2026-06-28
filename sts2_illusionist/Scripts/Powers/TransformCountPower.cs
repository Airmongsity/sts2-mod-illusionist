using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// Hidden per-turn tally of how many cards were 变化 (transformed) this turn — counting BOTH the
/// forward 幻化 you do during the turn AND the turn-start reverts of last turn's transmuted cards
/// (both flow through <see cref="Illusionist.Scripts.Transmutation.NotifyTransformed"/>). Read by
/// finishers that scale with "cards transformed this turn" (嬗变 / Metamorphosis).
///
/// <para>Invisible (<see cref="IsVisibleInternal"/> = false): a behind-the-scenes counter, not a
/// player-facing buff. The count lives in internal data (not <see cref="PowerModel.Amount"/>, which
/// stays 1 so the power is never auto-removed) and resets at the START of each of the owner's turns —
/// in the EARLY phase, BEFORE <see cref="TransmutePower"/>'s LATE-phase revert runs, so those reverts
/// correctly tally toward the new turn.</para>
/// </summary>
public sealed class TransformCountPower : PowerModel
{
    private sealed class Data
    {
        public int CountThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    // Behind-the-scenes tally; never shown on the status bar.
    protected override bool IsVisibleInternal => false;

    protected override object InitInternalData() => new Data();

    /// <summary>Cards transformed so far this turn (forward transmutes + this turn's start reverts).</summary>
    public int CountThisTurn => GetInternalData<Data>().CountThisTurn;

    /// <summary>Count one more transformed card. Called once per transform from NotifyTransformed.</summary>
    public void Increment() => GetInternalData<Data>().CountThisTurn++;

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        // Reset before the LATE-phase transmute-revert runs, so reverts tally toward the new turn.
        if (player.Creature == base.Owner)
        {
            GetInternalData<Data>().CountThisTurn = 0;
        }
        return Task.CompletedTask;
    }
}
