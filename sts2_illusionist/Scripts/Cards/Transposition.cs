using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 移花接木 (Transposition) — 1-cost Uncommon Skill with Exhaust. Select and Exhaust a card from
/// the draw pile, then add an Ethereal, Exhaust clone of its current combat state to hand.
/// Upgraded: Transposition itself no longer Exhausts; the generated clone still does.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "TRANSPOSITION")]
public sealed class TranspositionIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Exhaust,
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    };

    public TranspositionIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? selected = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Draw.GetPile(base.Owner),
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 1)))
            .FirstOrDefault();
        if (selected == null)
        {
            return;
        }

        // Snapshot before exhausting the real card so the clone preserves its exact upgrade,
        // cost, and combat-local state even if Exhaust hooks immediately move the source again.
        CardModel copy = selected.CreateClone();
        copy.AddKeyword(CardKeyword.Ethereal);
        copy.AddKeyword(CardKeyword.Exhaust);

        // Use the ordinary Exhaust pipeline deliberately: the source participates in mirror loading
        // and every other Exhaust payoff. Creating the clone is not a transform.
        await CardCmd.Exhaust(choiceContext, selected);
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
            copy,
            PileType.Hand,
            base.Owner);
        CardCmd.PreviewCardPileAdd(result, 1.8f);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
