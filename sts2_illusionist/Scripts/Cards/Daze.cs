using System.Collections.Generic;
using System.Threading.Tasks;
using Illusionist.Scripts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 恍惚 (DazeIllusionist) — 1 cost Skill, Common (upgraded: draw 3 instead of 2).
/// Draw 2 cards. 先机 (First Move): if this is the first card you play this turn, draw 1 extra card.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "DAZE_ILLUSIONIST")]
public sealed class DazeIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { IllusionHoverTips.FirstMove };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(2),
    };

    public DazeIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);

        // 先机: only the first card played this turn draws the extra card.
        if (FirstMove.IsActive(base.Owner.Creature))
        {
            await CardPileCmd.Draw(choiceContext, 1, base.Owner);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
