using System.Collections.Generic;
using System.Threading.Tasks;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 恍惚 (DazeIllusionist) — 1 cost Skill, Common. Draw 2 cards; if you've 变化 (transformed) a card
/// this turn, draw 1 more (upgraded: 2 more). A 幻化-system payoff — best played after a transmute.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "DAZE")]
public sealed class DazeIllusionist : IllusionistCard
{

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(2),
        new DynamicVar("Bonus", 1m),
    };

    public DazeIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);

        // Extra draw if you've transformed at least one card this turn — a forward 幻化 OR a turn-start
        // revert, both of which feed TransformCountPower.
        int transformedThisTurn = base.Owner.Creature.GetPower<TransformCountPower>()?.CountThisTurn ?? 0;
        if (transformedThisTurn > 0)
        {
            await CardPileCmd.Draw(choiceContext, base.DynamicVars["Bonus"].BaseValue, base.Owner);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["Bonus"].UpgradeValueBy(1m); // 1 -> 2 extra
    }
}
