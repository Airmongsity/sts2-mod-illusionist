using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
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

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// Feint - 3 cost Skill, Rare. Gain 1 Intangible; all enemies gain 30 Block.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "FEINT")]
public sealed class FeintIllusionist : IllusionistCard
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<IntangiblePower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(30m, ValueProp.Move),
    };

    public FeintIllusionist()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<IntangiblePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);

        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat == null)
        {
            return;
        }

        foreach (Creature enemy in combat.HittableEnemies.Where(e => e.IsAlive).ToList())
        {
            await CreatureCmd.GainBlock(enemy, base.DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1); // 3 -> 2
    }
}
