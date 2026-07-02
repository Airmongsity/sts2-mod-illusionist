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

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 遮蔽 (ObscureIllusionist) — 1 cost Skill, Common.
/// Gain 5 Block and draw 2 cards; if any enemy intends to attack, gain 7 extra Block. Upgraded:
/// +2 to both Block values (5 -> 7, 7 -> 9).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "OBSCURE_ILLUSIONIST")]
public sealed class ObscureIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    // A second BlockVar of the same type MUST be given an explicit name or DynamicVarSet throws.
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(5m, ValueProp.Move),
        new CardsVar(2),
        new BlockVar("ExtraBlock", 4m, ValueProp.Move),
    };

    public ObscureIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);

        // Gain extra Block if ANY enemy intends to attack this turn.
        ICombatState? combat = base.Owner.Creature.CombatState;
        bool anyEnemyAttacks = combat != null
            && combat.Enemies.Any(e => e.IsAlive && e.Monster != null && e.Monster.IntendsToAttack);
        if (anyEnemyAttacks)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, (BlockVar)base.DynamicVars["ExtraBlock"], cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
        ((BlockVar)base.DynamicVars["ExtraBlock"]).UpgradeValueBy(2m);
    }
}
