using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 重燃 (Rekindle) — 1 cost Skill, Common (upgraded: no longer gains Frail).
/// Gain 1 Frail (on yourself); at the start of your next turn, Copy 1. The cheap, Common, reliable
/// 0->1 restart: even if every mirror shatters this turn, the delayed copy lands next turn no matter
/// what. The self-Frail (block gained -25%) is a soft tax you can dodge by building your Block FIRST,
/// then playing Rekindle.
/// </summary>
public sealed class Rekindle : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    // Copy is our own mechanic (no engine tooltip), so attach the Copy / 复制品 tips explicitly,
    // plus Frail for the self-debuff (dropped from the hover set once upgraded removes it).
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsUpgraded
        ? new IHoverTip[] { IllusionHoverTips.Copy, IllusionHoverTips.CopyToken }
        : new IHoverTip[] { HoverTipFactory.FromPower<FrailPower>(), IllusionHoverTips.Copy, IllusionHoverTips.CopyToken };

    public Rekindle()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!IsUpgraded)
        {
            await PowerCmd.Apply<FrailPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
        }

        // Delayed Copy 1: fires at the start of your next turn, guaranteeing a mirror even if all
        // of this turn's mirrors shatter (see RekindlePower).
        await PowerCmd.Apply<RekindlePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }
}
