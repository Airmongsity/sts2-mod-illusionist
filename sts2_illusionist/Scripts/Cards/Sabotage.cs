using System;
using System.Collections.Generic;
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
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 破坏 (Sabotage) — 1 cost Attack, Ancient. The transcended (Archaic Tooth) form of 反击/Riposte and
/// Orobas's reward — the strongest of the Illusionist's ancient cards. Deal 16 damage and apply 2
/// Weak; First Move: instead of discarding, put this card on top of your draw pile (so leading with
/// it each turn recurs it — same top-deck mechanism as Regent's Shining Strike). Upgraded: 22 / 3.
/// </summary>
public sealed class Sabotage : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<WeakPower>(),
        IllusionHoverTips.FirstMove,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(16m, ValueProp.Move),
        new PowerVar<WeakPower>(2m),
    };

    public Sabotage()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await PowerCmd.Apply<WeakPower>(choiceContext, target, base.DynamicVars.Weak.BaseValue, base.Owner.Creature, this);

        // 先机: leading with this card recurs it — put it on top of your draw pile instead of
        // discarding (the same top-deck call Regent's Shining Strike uses). Skip if it would Exhaust.
        if (FirstMove.IsActive(base.Owner.Creature) && !base.Keywords.Contains(CardKeyword.Exhaust) && !base.ExhaustOnNextPlay)
        {
            await CardPileCmd.Add(this, PileType.Draw, CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(6m);
        base.DynamicVars.Weak.UpgradeValueBy(1m);
    }
}
