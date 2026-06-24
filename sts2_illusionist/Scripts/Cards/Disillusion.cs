using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻灭 (Disillusion) — 1 cost Skill, Common (upgraded: gains Retain).
/// Choose up to 2 cards in your hand and Exhaust them.
/// </summary>
public sealed class Disillusion : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    public Disillusion()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Choose up to 2 (min 0, max 2) cards from hand to exhaust.
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext, base.Owner, new CardSelectorPrefs(base.SelectionScreenPrompt, 0, 2), null, this);
        foreach (CardModel card in selected)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
