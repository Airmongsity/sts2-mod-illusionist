using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 一线生机 (Silver Lining) - 0 cost Skill, Common, targets an enemy (upgraded: gains Retain).
/// Gain 1 energy. Transfer the target's Block to you (remove it, gain that much Block) - the 养龟
/// cash-in: turn the Block you fed the enemy (虚张声势/假想敌) into your own defense. If the targeted
/// enemy still has a negative effect (a debuff, or any power with a negative amount), gain 1 more
/// energy. The stolen Block is granted Unpowered (exact transfer, no Dexterity double-dip).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SILVER_LINING")]
public sealed class SilverLiningIllusionist : IllusionistCard
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public SilverLiningIllusionist()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(1, base.Owner);

        Creature? target = cardPlay.Target;

        // Transfer the target's Block to you: strip it, then gain that much yourself (exact, Unpowered).
        if (target != null && target.Block > 0)
        {
            int block = target.Block;
            await CreatureCmd.LoseBlock(choiceContext, target, block, base.Owner.Creature);
            await CreatureCmd.GainBlock(base.Owner.Creature, block, ValueProp.Unpowered, null);
        }

        if (target != null && target.Powers.Any(p => p.Type == PowerType.Debuff || p.Amount < 0m))
        {
            await PlayerCmd.GainEnergy(1, base.Owner);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
