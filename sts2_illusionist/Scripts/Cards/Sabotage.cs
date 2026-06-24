using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 破坏 (Sabotage) — 1 cost Attack, Ancient. The transcended (Archaic Tooth) form of 反击/Riposte.
/// Deal 12 damage; if the enemy intends to attack, deal 6 extra damage and apply 3 Weak.
/// Obtained only by transforming Riposte via Archaic Tooth — never in reward pools (Pool is
/// overridden so the lookup never throws; Ancient rarity is excluded from reward rolls anyway).
/// </summary>
public sealed class Sabotage : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move),
        new DynamicVar("Bonus", 6m),
        new PowerVar<WeakPower>(3m),
    };

    public Sabotage()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        bool intendsToAttack = IntendsToAttack(target);
        decimal damage = base.DynamicVars.Damage.BaseValue;
        if (intendsToAttack)
        {
            damage += base.DynamicVars["Bonus"].BaseValue;
        }

        await DamageCmd.Attack(damage).FromCard(this).Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // The extra damage and the Weak both punish an incoming attack.
        if (intendsToAttack)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, target, base.DynamicVars.Weak.BaseValue, base.Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
        base.DynamicVars.Weak.UpgradeValueBy(1m);
    }

    private static bool IntendsToAttack(Creature target)
    {
        if (target.Monster == null)
        {
            return false;
        }
        return target.Monster.NextMove.Intents.Any(i => i.IntentType == IntentType.Attack);
    }
}
