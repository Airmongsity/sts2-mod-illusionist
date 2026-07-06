using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 回镜 (Echo Mirror) — Rare mirror type. The first card you play each turn adds one 0-cost, Ethereal,
/// Exhaust copy of itself to your hand per stack (the mod's original Mirror Image mechanic, now Rare-
/// gated and copies-to-hand — you choose to play them, so it can't auto-spiral). Copies are made AFTER
/// the card resolves, so they carry its in-play state.
/// </summary>
[RegisterPower]
public sealed class EchoMirrorPower : MirrorTypePower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (base.Amount <= 0)
        {
            return;
        }

        // Only the owner's own original plays, and only the turn's FIRST card.
        if (cardPlay.Card.Owner.Creature != base.Owner || !cardPlay.IsFirstInSeries)
        {
            return;
        }

        // Identify the turn's first card by reference (immune to nested plays that resolve during the
        // first card's own OnPlay — Improvise auto-play, transmute-driven plays, etc.).
        CardPlayStartedEntry? firstPlay = CombatManager.Instance.History.CardPlaysStarted.FirstOrDefault(
            e => e.Actor == base.Owner && e.CardPlay.IsFirstInSeries && e.HappenedThisTurn(base.CombatState));
        if (firstPlay == null || firstPlay.CardPlay != cardPlay)
        {
            return;
        }

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
        Log.Info($"[illusionist] EchoMirror: first card {source.Id.Entry} → {copies.Count} 0-cost Ethereal/Exhaust copies.");
    }
}
