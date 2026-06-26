using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 重塑 (Reshape) — 2 cost Skill, Rare, Exhaust (upgraded: no longer Exhausts).
/// Give a card in your hand permanent <b>Replay</b> (+1 replay count) — it plays one extra time
/// every time you play it for the rest of combat.
///
/// <para>This grants Replay directly via <see cref="CardModel.BaseReplayCount"/> (the same way the
/// Necrobinder's Transfigure does) rather than applying an enchantment, so it never clobbers an
/// enchantment the chosen card may already carry.</para>
/// </summary>
public sealed class Reshape : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
    };

    public Reshape()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext, base.Owner, new CardSelectorPrefs(base.SelectionScreenPrompt, 1), null, this);
        foreach (CardModel card in selected)
        {
            // Grant Replay without touching any existing enchantment (Transfigure's approach).
            card.BaseReplayCount++;
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
