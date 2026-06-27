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
/// 致盲 (BlindIllusionist) — 1 cost Attack, Common.
/// Deal 5 damage; if the enemy intends to attack, apply 1 Weak and gain 5 Block — a reactive
/// attack that punishes (and partly defends against) an incoming swing. Upgraded: 8 damage.
/// </summary>
public sealed class BlindIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    // Weak power needs its hover-tip added explicitly (base-game Bash pattern).
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(5m, ValueProp.Move),
        new PowerVar<WeakPower>(1m),
        new BlockVar(5m, ValueProp.Move),
    };

    public BlindIllusionist()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // Only if the enemy telegraphs an attack: Weak it and brace with Block.
        if (IntendsToAttack(target))
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, target, base.DynamicVars.Weak.BaseValue, base.Owner.Creature, this);
            await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
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
