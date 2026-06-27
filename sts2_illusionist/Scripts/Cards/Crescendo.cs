using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 渐强 (Crescendo) — 1 cost Power, Uncommon (upgraded: 0 cost).
/// Lose 4 Strength immediately, then gain 2 Strength at the start of each turn (net positive after
/// 2 turns). Pairs with the Illusionist's other Strength manipulation.
/// </summary>
public sealed class Crescendo : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    // References Strength — surface its tip (base-game Bash pattern).
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<StrengthPower>() };

    public Crescendo()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Pay 4 Strength up front...
        await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner.Creature, -4, base.Owner.Creature, this);
        // ...then regain 2 Strength at the start of each turn (CrescendoPower handles the recurring gain).
        await PowerCmd.Apply<CrescendoPower>(choiceContext, base.Owner.Creature, 2, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
