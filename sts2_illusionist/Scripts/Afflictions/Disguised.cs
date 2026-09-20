using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Illusionist.Scripts.Afflictions;

/// <summary>
/// 易容 (Disguised) — the first time an enemy gains Block each round, the afflicted card transforms
/// into a random card from a random OTHER playable character's unlocked pool.
///
/// <para>This is the 养龟 (fatten-the-turtle) line's interface to the borrowed-character content:
/// feeding an enemy Block is what spins the wheel, so feeding stops being a pure self-debuff while
/// you wait for a cash-in. Character selection reuses <see cref="OtherCharacterCardSelection"/>, so it
/// picks up MOD characters automatically.</para>
///
/// <para>Two hazards the implementation has to handle. (1) <see cref="CardCmd.Transform"/> swaps the
/// card model out and the affliction does NOT travel with it, so this re-applies itself to the
/// replacement — otherwise the wheel would spin exactly once. The once-per-round marker is copied
/// across with it, or the fresh instance would be free to fire again in the same round.
/// (2) Transforming cards inline inside <c>AfterBlockGained</c> mutates card piles while the hook is
/// still enumerating models — the same re-entrancy that made False Refuge throw "Collection was
/// modified" against Rampart / Kaiser Crab — so the work is deferred by one frame.</para>
/// </summary>
[RegisterAffliction]
public sealed class Disguised : ModAfflictionTemplate
{
    public override bool HasExtraCardText => true;

    /// <summary>Round this already fired in; -1 means it has not fired yet.</summary>
    private int _lastRound = -1;

    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (amount <= 0m || !base.HasCard)
        {
            return Task.CompletedTask;
        }

        CardModel card = base.Card;
        Player? owner = card.Owner;
        ICombatState? combat = owner?.Creature.CombatState;
        if (owner == null || combat == null)
        {
            return Task.CompletedTask;
        }

        // Enemies only: Imagined Foe and Obscure also hand Block to the player and to allies.
        if (creature.IsPlayer || !combat.HittableEnemies.Contains(creature))
        {
            return Task.CompletedTask;
        }

        if (_lastRound == combat.RoundNumber)
        {
            return Task.CompletedTask;
        }

        _lastRound = combat.RoundNumber;
        _ = DeferTransformAsync(card, owner, combat.RoundNumber);
        return Task.CompletedTask;
    }

    private static async Task DeferTransformAsync(CardModel card, Player owner, int round)
    {
        await Cmd.Wait(0.01f);

        if (card.Pile == null || card.Owner != owner || !card.IsTransformable)
        {
            return;
        }

        List<CharacterModel> characters = ModelDb.AllCharacters
            .Where(character => character.IsPlayable && character.Id != owner.Character.Id)
            .ToList();
        CharacterModel? character = owner.RunState.Rng.CombatCardSelection.NextItem(characters);
        if (character == null)
        {
            return;
        }

        List<CardModel> pool = OtherCharacterCardSelection.GetUnlockedPool(owner, character);
        CardModel? template = owner.RunState.Rng.CombatCardSelection.NextItem(pool);
        ICombatState? combat = owner.Creature.CombatState;
        if (template == null || combat == null)
        {
            return;
        }

        CardPileAddResult? result = await CardCmd.Transform(card, combat.CreateCard(template, owner));
        if (result is not { success: true, cardAdded: not null })
        {
            return;
        }

        // Re-apply to the replacement and carry the once-per-round marker, so the wheel keeps spinning
        // on later rounds but cannot spin twice in this one.
        Disguised? moved = await CardCmd.Afflict<Disguised>(result.Value.cardAdded, 1);
        if (moved != null)
        {
            moved._lastRound = round;
        }
    }
}
