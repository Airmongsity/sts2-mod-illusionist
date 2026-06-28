using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 抗衡 (CounterIllusionist) — 2 cost Attack, Rare (upgraded: gains Retain). Deal 10 damage, then
/// permanently add the target's current attack-intent damage to THIS card's damage — so every enemy
/// telegraph you "counter" makes the card hit harder forever after (the Ironclad Thrash / Rampage
/// self-growing pattern). Reflecting a big boss swing snowballs it into a heavy repeatable hit.
/// </summary>
public sealed class CounterIllusionist : CardModel
{
    // Accumulated growth, tracked separately so it can be re-applied if the card's vars are rebuilt
    // (e.g. on downgrade), exactly like the base game's Thrash.
    private decimal _extraDamage;

    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
    };

    private decimal ExtraDamage
    {
        get => _extraDamage;
        set
        {
            AssertMutable();
            _extraDamage = value;
        }
    }

    public CounterIllusionist()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        // Snapshot the enemy's attack intent BEFORE dealing damage (so a lethal hit doesn't matter).
        int intentDamage = GetIncomingAttackDamage(target);

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // Permanently fold that telegraph into this card's damage.
        if (intentDamage > 0)
        {
            base.DynamicVars.Damage.BaseValue += intentDamage;
            ExtraDamage += intentDamage;
        }
    }

    protected override void OnUpgrade()
    {
        // Retain so you can hold it until a worthwhile attack intent is telegraphed.
        AddKeyword(CardKeyword.Retain);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        base.DynamicVars.Damage.BaseValue += ExtraDamage;
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
