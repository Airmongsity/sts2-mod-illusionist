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
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// Silver Lining - gain 7 Block and transfer all Block from the target enemy. Upgraded: Retain.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SILVER_LINING")]
public sealed class SilverLiningIllusionist : IllusionistCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new BlockVar(7m, ValueProp.Move) };

    public SilverLiningIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        if (cardPlay.Target.Block <= 0)
        {
            return;
        }

        int transferredBlock = cardPlay.Target.Block;
        await CreatureCmd.LoseBlock(
            choiceContext,
            cardPlay.Target,
            transferredBlock,
            base.Owner.Creature);
        await CreatureCmd.GainBlock(
            base.Owner.Creature,
            transferredBlock,
            ValueProp.Unpowered,
            null);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
