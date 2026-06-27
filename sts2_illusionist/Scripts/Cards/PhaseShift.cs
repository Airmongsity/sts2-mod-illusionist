using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 虚实转换 (Phase Shift) — 1 cost Power, Common (upgraded: 0 cost).
/// Apply Phase Shift: the next time you would take unblocked damage, your mirror images are NOT
/// destroyed (the charge is consumed instead). Provides no Block.
/// </summary>
public sealed class PhaseShiftIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    // This card references mirror images (镜像) — surface that tip.
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { IllusionHoverTips.CopyToken };

    public PhaseShiftIllusionist()
        : base(1, CardType.Power, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PhaseShiftPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
