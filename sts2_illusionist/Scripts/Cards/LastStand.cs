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
/// 绝境 (Last Stand) — 1 cost Skill, Common, Innate + Exhaust (a guaranteed turn-1 Mirror opener).
/// Gain 5 Block. If you have no mirror clones (复制品), gain 5 extra Block AND Copy 2 (so it doubles
/// as a Mirror opener). Upgraded: +3 to both Block values (5 -> 8, 5 -> 8).
/// </summary>
public sealed class LastStandIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Retain, CardKeyword.Exhaust };

    // Block tip comes from GainsBlock; add the Copy action + mirror-image (复制品) tips it references.
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Copy,
        IllusionHoverTips.CopyToken,
    };

    // Two BlockVars: the base block ("Block") and the conditional bonus. A second var of the same
    // type MUST be given an explicit name or DynamicVarSet throws on the duplicate "Block" key.
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(5m, ValueProp.Move),
        new BlockVar("ExtraBlock", 5m, ValueProp.Move),
    };

    public LastStandIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // If you have no mirror clones, gain the extra Block and Copy 2 (kickstart the Mirror system).
        if (MirrorClone.CountAlive(base.Owner) == 0)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, (BlockVar)base.DynamicVars["ExtraBlock"], cardPlay);
            await MirrorClone.Copy(base.Owner, 2, choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
        ((BlockVar)base.DynamicVars["ExtraBlock"]).UpgradeValueBy(3m);
    }
}
