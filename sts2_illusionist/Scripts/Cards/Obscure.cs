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
/// 遮蔽 (ObscureIllusionist) — 1 cost Skill, Common.
/// Gain 10 Block and draw 2 cards; if the target enemy intends to attack, it gains 3 Block.
/// Upgraded: 10 -> 14 Block.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "OBSCURE")]
public sealed class ObscureIllusionist : IllusionistCard
{

    public override bool GainsBlock => true;

    // A second BlockVar of the same type MUST be given an explicit name or DynamicVarSet throws.
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(10m, ValueProp.Move),
        new CardsVar(2),
        new BlockVar("ExtraBlock", 3m, ValueProp.Move),
    };

    public ObscureIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);

        if (cardPlay.Target.Monster != null && cardPlay.Target.Monster.IntendsToAttack)
        {
            await CreatureCmd.GainBlock(cardPlay.Target, (BlockVar)base.DynamicVars["ExtraBlock"], cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(4m);
    }
}
