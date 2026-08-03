using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 遮蔽 (ObscureIllusionist) — 1 cost Skill, Common.
/// Every living creature gains 5 Block, then you gain 6 additional Block and draw 2 cards.
/// Mirror clones are excluded. Upgraded: personal Block 6 -> 8.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "OBSCURE")]
public sealed class ObscureIllusionist : IllusionistCard
{

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar("SharedBlock", 5m, ValueProp.Unpowered),
        new BlockVar(6m, ValueProp.Move),
        new CardsVar(2),
    };

    public ObscureIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat == null)
        {
            return;
        }

        // "Creature" means every living combat creature that can participate in effects. Mirror
        // clones are cosmetic implementations of mirror images, so exclude them explicitly.
        HashSet<Creature> creatures = new HashSet<Creature> { base.Owner.Creature };
        foreach (Creature ally in combat.Allies.Where(creature => creature.Monster is not MirrorClone))
        {
            creatures.Add(ally);
        }
        foreach (Creature enemy in combat.HittableEnemies)
        {
            creatures.Add(enemy);
        }

        decimal sharedBlock = base.DynamicVars["SharedBlock"].BaseValue;
        foreach (Creature creature in creatures.Where(creature => creature.CurrentHp != 0))
        {
            await CreatureCmd.GainBlock(creature, sharedBlock, ValueProp.Unpowered, cardPlay);
        }

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
    }
}
