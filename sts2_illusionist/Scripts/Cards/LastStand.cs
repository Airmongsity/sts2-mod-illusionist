using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 绝境 (Last Stand) — 1 cost Skill, Common.
/// Gain 7 Block. If you have no mirror clones (复制品), gain 4 extra Block.
/// Upgraded: gains the Retain keyword.
/// </summary>
public sealed class LastStand : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    public override bool GainsBlock => true;

    // Block tip comes from GainsBlock; add the mirror-image (复制品) tip this card references.
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { IllusionHoverTips.CopyToken };

    // Two BlockVars: the base block ("Block") and the conditional bonus. A second var of the same
    // type MUST be given an explicit name or DynamicVarSet throws on the duplicate "Block" key.
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(7m, ValueProp.Move),
        new BlockVar("ExtraBlock", 4m, ValueProp.Move),
    };

    public LastStand()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // If you have no mirror clones, gain the extra Block.
        if (MirrorClone.CountAlive(base.Owner) == 0)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, (BlockVar)base.DynamicVars["ExtraBlock"], cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
