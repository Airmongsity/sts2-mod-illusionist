using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 镜像 (Mirror Image) power. The first card you play each turn casts an echo for every mirror you have:
/// for each mirror, a 0-cost, [gold]Ethereal[/gold], [gold]Exhaust[/gold] copy of that card is added to
/// your hand. With 5 mirrors, playing a Riposte leaves 5 free Riposte copies in your hand that turn (the
/// copies are made AFTER the card resolves, so they carry its in-play state, e.g. Riposte's +1). The
/// copies are 虚无消耗 like Necrobinder's Call of the Void: Ethereal so any you don't play vanish at end
/// of turn, Exhaust so a played one doesn't pile up in your discard. When you take unblocked damage, all
/// mirrors shatter (the power is removed entirely).
/// </summary>
[RegisterPower]
public sealed class MirrorImagePower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// When the owner plays their FIRST card of the turn, add one 0-cost Ethereal/Exhaust copy of it to
    /// hand per mirror.
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (base.Amount <= 0)
        {
            return;
        }

        // Only the owner's own original plays (in multiplayer this hook fires for every player's cards).
        if (cardPlay.Card.Owner.Creature != base.Owner || !cardPlay.IsFirstInSeries)
        {
            return;
        }

        // Fire only for the turn's FIRST card. CardPlaysStarted is chronological and stores the same
        // CardPlay instance we get here, so the earliest first-in-series entry this turn IS the player's
        // first card — identifying it by reference makes us immune to nested plays that resolve during
        // the first card's own OnPlay (Improvise auto-play, transmute-driven draws/plays, etc.), which a
        // simple count would miscount and skip on.
        CardPlayStartedEntry? firstPlay = CombatManager.Instance.History.CardPlaysStarted.FirstOrDefault(
            e => e.Actor == base.Owner && e.CardPlay.IsFirstInSeries && e.HappenedThisTurn(base.CombatState));
        if (firstPlay == null || firstPlay.CardPlay != cardPlay)
        {
            return;
        }

        // The played card is still in the Play pile here (a combat pile), so CreateClone is legal and
        // captures its current, post-OnPlay state.
        CardModel source = cardPlay.Card;
        if (!source.IsTransformable)
        {
            return;
        }

        List<CardModel> copies = new List<CardModel>();
        for (int i = 0; i < base.Amount; i++)
        {
            CardModel copy = source.CreateClone();
            copy.EnergyCost.SetThisCombat(0);
            CardCmd.ApplyKeyword(copy, CardKeyword.Ethereal, CardKeyword.Exhaust);
            copies.Add(copy);
        }

        Flash();
        await CardPileCmd.AddGeneratedCardsToCombat(copies, PileType.Hand, base.Owner.Player);
        Log.Info($"[illusionist] MirrorImage: first card {source.Id.Entry} → added {copies.Count} 0-cost Ethereal/Exhaust copies (mirrors={base.Amount}).");
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
