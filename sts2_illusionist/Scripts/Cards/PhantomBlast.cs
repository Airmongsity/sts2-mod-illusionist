using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻爆 (Phantom Blast) — 1 cost Attack, Common (upgraded: 4 -> 7 damage).
/// Deal 4 damage. <b>Costs 0 if it's a copy.</b> The 幻化 system's burst payoff: a copy (clone) is
/// free, so batch-幻化 your hand into Phantom Blasts (千面 / Myriad Faces) and dump them all in one
/// turn for a huge spike — and any mirror-replayed copy is free too.
///
/// The conditional cost is the card's own <see cref="TryModifyEnergyCostInCombat"/> override: every
/// card in a pile is a combat hook listener, so when the engine resolves this card's cost it asks
/// this very card, which drops the cost to 0 while it <see cref="CardModel.IsClone"/>.
/// </summary>
public sealed class PhantomBlast : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(4m, ValueProp.Move),
    };

    public PhantomBlast()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        // A copy of this card costs nothing — duplicate it (幻化 / mirror replay) to spam it.
        if (card == this && IsClone)
        {
            modifiedCost = 0m;
            return true;
        }

        modifiedCost = originalCost;
        return false;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
