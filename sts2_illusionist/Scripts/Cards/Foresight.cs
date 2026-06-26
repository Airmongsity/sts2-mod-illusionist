using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 预见 (Foresight) — 2 cost Skill, Uncommon, Exhaust. Gain Block equal to the selected enemy's
/// current total attack-intent damage. Upgraded: gains Retain.
///
/// The First-Move gate was removed so Foresight slots into the intent-reflect combo: play 挑衅
/// (Provoke) to inflate an enemy's attack with temporary Strength, then Foresight blocks the
/// inflated swing while 抗衡 (Counter) reflects it. (Provoke is your first card those turns, which
/// would have disabled a First-Move version.)
/// </summary>
public sealed class Foresight : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public Foresight()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int block = GetIncomingAttackDamage(cardPlay.Target);
        if (block > 0)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, block, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        // Retain so the player can hold it until the enemy reveals (or you inflate) an attack intent.
        AddKeyword(CardKeyword.Retain);
    }

    /// <summary>Total damage this enemy's current attack intent would deal (0 if not attacking).</summary>
    private int GetIncomingAttackDamage(Creature target)
    {
        if (target.Monster == null)
        {
            return 0;
        }

        IReadOnlyList<Creature> playerTargets = new[] { base.Owner.Creature };
        int total = 0;
        foreach (AbstractIntent intent in target.Monster.NextMove.Intents)
        {
            if (intent is AttackIntent attack)
            {
                total += attack.GetTotalDamage(playerTargets, target);
            }
        }
        return total;
    }
}
