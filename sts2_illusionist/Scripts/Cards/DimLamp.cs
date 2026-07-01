using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 暗淡油灯 (Dim Lamp) — a 0 cost Token, Exhaust, created by 点灯 (KindleIllusionist), which transmutes a
/// 熄灭油灯 (Extinguished Lamp) into it. Gain 1 energy and draw 1 card. Because it's made via 幻化, it
/// reverts to a 熄灭油灯 at the end of the turn if you don't play it — so use it now.
/// </summary>
public sealed class DimLampIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public DimLampIllusionist()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(1m, base.Owner);
        await CardPileCmd.Draw(choiceContext, 2m, base.Owner);
    }
}
