using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 镜像 (Mirror Image) power. While the owner has at least one mirror, the first card(s) they
/// play each turn are replayed — one extra play per mirror (the same engine mechanism Echo Form
/// uses, so the WHOLE card is replayed, effects and all). When the owner takes unblocked damage,
/// all mirrors shatter (the power is removed entirely).
/// </summary>
public sealed class MirrorImagePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// Replay the first card(s) of the turn. We count how many "first-in-series" (i.e. original,
    /// non-replayed) card plays have already happened this turn; while that count is below the
    /// number of mirrors, the next card gets one extra play. With 1 mirror this is exactly
    /// "replay the first card each turn"; with N mirrors it is the first N cards.
    /// </summary>
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner.Creature != base.Owner)
        {
            return playCount;
        }

        int alreadyEchoed = CombatManager.Instance.History.CardPlaysStarted.Count(
            (CardPlayStartedEntry e) => e.Actor == base.Owner && e.CardPlay.IsFirstInSeries && e.HappenedThisTurn(base.CombatState));
        if (alreadyEchoed >= base.Amount)
        {
            return playCount;
        }

        return playCount + 1;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // The owner taking real (unblocked) damage shatters all mirrors: remove the power and
        // despawn every cosmetic clone the player owns.
        if (target == base.Owner && result.UnblockedDamage > 0)
        {
            // 虚实转换 (Phase Shift): a charge lets the mirrors survive this hit. Consume one
            // charge instead of shattering. The charge is only spent when a shatter would actually
            // have happened (i.e. you have mirrors and took unblocked damage).
            PhaseShiftPower? guard = base.Owner.GetPower<PhaseShiftPower>();
            if (guard != null)
            {
                if (guard.Amount > 1m)
                {
                    await PowerCmd.Decrement(guard);
                }
                else
                {
                    await PowerCmd.Remove(guard);
                }
                Flash();
                return;
            }

            await PowerCmd.Remove(this);
            await MirrorClone.ShatterAll(base.Owner.Player);
        }
    }
}
