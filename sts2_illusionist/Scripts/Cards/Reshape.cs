using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Enchantments;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 重塑 (Reshape) — 1 cost Skill, Rare, Exhaust (upgraded: no longer Exhausts).
/// Give a card in your hand the permanent enchantment "First Move: Replay 1" — that card is
/// played one extra time whenever it's the first card you play that turn.
/// </summary>
public sealed class Reshape : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { IllusionHoverTips.FirstMove };

    public Reshape()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext, base.Owner, new CardSelectorPrefs(base.SelectionScreenPrompt, 1), null, this);
        foreach (CardModel card in selected)
        {
            CardCmd.Enchant<FirstMoveReplay>(card, 1m);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
