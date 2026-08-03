using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Illusionist.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 替身 (Body Double) — 3 cost Rare Skill, Exhaust.
/// The target loses 6 HP (12 when upgraded), then receives a one-enemy-turn
/// version of Osty's Die For You effect.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "BODY_DOUBLE")]
public sealed class BodyDoubleIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move),
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<BodyDoublePower>(),
    };

    public BodyDoubleIllusionist()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            base.DynamicVars.Damage,
            this,
            cardPlay);

        if (cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<BodyDoublePower>(
                choiceContext,
                cardPlay.Target,
                1m,
                base.Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(6m);
    }
}
