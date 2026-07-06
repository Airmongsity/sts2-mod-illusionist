using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 速镜 (Swift Mirror) — Rare mirror type. The first card you play each turn costs 0. Implemented exactly
/// like the game's VoidFormPower: <see cref="TryModifyEnergyCostInCombatLate"/> reports a modified cost of
/// 0 for the turn's first eligible card (owned by you, in Hand/Play), tracked by a per-turn counter that
/// increments in <see cref="AfterCardPlayed"/> and resets at the start of your turn. One free card per
/// turn regardless of stacks.
/// </summary>
[RegisterPower]
public sealed class SwiftMirrorPower : MirrorTypePower
{
    private const int FreeCardsPerTurn = 1;

    private sealed class Data
    {
        public int cardsPlayedThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (ShouldSkip(card))
        {
            return false;
        }

        modifiedCost = 0m;
        return true;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Count only the owner's own, non-auto, fully-resolved plays (matches VoidFormPower).
        if (cardPlay != null && cardPlay.Card.Owner.Creature == base.Owner && !cardPlay.IsAutoPlay && cardPlay.IsLastInSeries)
        {
            GetInternalData<Data>().cardsPlayedThisTurn++;
        }
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(base.Owner))
        {
            GetInternalData<Data>().cardsPlayedThisTurn = 0;
        }
        return Task.CompletedTask;
    }

    private bool ShouldSkip(CardModel card)
    {
        if (card.Owner.Creature != base.Owner)
        {
            return true;
        }

        if (card.Pile?.Type != PileType.Hand && card.Pile?.Type != PileType.Play)
        {
            return true;
        }

        return GetInternalData<Data>().cardsPlayedThisTurn >= FreeCardsPerTurn;
    }
}
