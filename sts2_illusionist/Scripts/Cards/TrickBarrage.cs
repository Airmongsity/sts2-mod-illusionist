using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 连环戏法 (Trick Barrage) — 2 cost Rare Attack. Return every non-X, 0-cost card from the discard
/// pile to the hand, then deal 4 damage once for each card in hand. Upgraded: 6 damage per hit.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "TRICK_BARRAGE")]
public sealed class TrickBarrageIllusionist : IllusionistCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(4m, ValueProp.Move),
    };

    public TrickBarrageIllusionist()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        List<CardModel> zeroCostCards = PileType.Discard.GetPile(base.Owner).Cards
            .Where(card => !card.EnergyCost.CostsX
                && card.EnergyCost.GetWithModifiers(CostModifiers.All) == 0)
            .ToList();

        foreach (CardModel card in zeroCostCards)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }

        int hitCount = PileType.Hand.GetPile(base.Owner).Cards.Count;
        if (hitCount == 0)
        {
            return;
        }

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(hitCount)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
