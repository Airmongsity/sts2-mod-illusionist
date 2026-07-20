using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 共鸣 (ResonanceIllusionist) power. Each stack (one per 共鸣 played) grants one per-turn energy
/// discount: the first <see cref="PowerModel.Amount"/> times each turn you play a hand card that shares a
/// name with a card stored in your mirrors, it costs 1 less. Matching reads the mirror live, so a card can
/// match a sibling's own imprint (e.g. the 2nd 共鸣 matches the 1st's imprint and discounts itself).
/// <para>The per-card <see cref="Data.Discounted"/> marker is set by the cost hook only when a discount is
/// actually applied, and a charge is spent in <see cref="AfterCardPlayed"/> only for a marked card. The
/// 共鸣 that creates this power never carries the marker (its cost was computed before the power existed),
/// so it cannot spend a charge on its own imprint.</para>
/// <para>Stacks (<see cref="PowerStackType.Counter"/>): each 共鸣 adds one per-turn charge. The icon shows
/// charges remaining this turn via <see cref="DisplayAmount"/>, refreshed with
/// <see cref="InvokeDisplayAmountChanged"/>.</para>
/// </summary>
[RegisterPower]
public sealed class ResonancePower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Charges remaining this turn = total stacks minus charges already spent. Shown on the icon so the
    // player sees how many discounts are left, not just how many 共鸣 they own.
    public override int DisplayAmount => base.Amount - GetInternalData<Data>().UsesConsumedThisTurn;

    private sealed class Data
    {
        public int UsesConsumedThisTurn;
        public readonly HashSet<CardModel> Discounted = new();
    }

    protected override object InitInternalData() => new Data();

    private int Remaining => base.Amount - GetInternalData<Data>().UsesConsumedThisTurn;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player.Creature)
        {
            Data data = GetInternalData<Data>();
            data.UsesConsumedThisTurn = 0;
            data.Discounted.Clear();
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }

    private bool MatchesStored(CardModel card)
    {
        MirrorImagePower? mirror = base.Owner.GetPower<MirrorImagePower>();
        if (mirror == null) return false;
        Type t = card.GetType();
        foreach (CardModel stored in mirror.StoredCards)
        {
            if (stored.GetType() == t) return true;
        }
        return false;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        Data data = GetInternalData<Data>();
        bool applies = Remaining > 0
            && originalCost > 0m
            && card.Owner?.Creature == base.Owner
            && MatchesStored(card);
        if (applies)
        {
            data.Discounted.Add(card);
            modifiedCost = originalCost - 1m;
            return true;
        }
        data.Discounted.Remove(card);
        return false;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Data data = GetInternalData<Data>();
        if (cardPlay.IsAutoPlay || cardPlay.Card.Owner?.Creature != base.Owner) return Task.CompletedTask;
        if (data.Discounted.Contains(cardPlay.Card))
        {
            data.UsesConsumedThisTurn++;
            data.Discounted.Remove(cardPlay.Card);
            Flash();
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }
}
