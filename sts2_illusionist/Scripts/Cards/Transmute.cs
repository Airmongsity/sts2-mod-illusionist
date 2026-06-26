using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻化 (Transmute) — 1 cost Skill, Uncommon, Exhaust (upgraded: 0 cost).
/// Choose a card from your exhaust pile, then 幻化 a card in your hand into a copy of it (carrying
/// that card's upgrades/enchantments). Pairs with the exhaust-revert mechanic: exhausted cards
/// become a menu you can pull copies back from. Exhausts itself (feeding future Transmutes).
/// </summary>
public sealed class Transmute : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Transmute,
    };

    public Transmute()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;

        CardPile exhaustPile = PileType.Exhaust.GetPile(owner);
        if (!exhaustPile.Cards.Any())
        {
            return;
        }

        // 1) Choose a card from the exhaust pile.
        CardModel? chosen = (await CardSelectCmd.FromCombatPile(
            choiceContext, exhaustPile, owner,
            new CardSelectorPrefs(new LocString("cards", "TRANSMUTE.selectionScreenPrompt"), 1))).FirstOrDefault();
        if (chosen == null)
        {
            return;
        }

        // 2) Choose a hand card and 幻化 it into a copy of the chosen exhausted card.
        await Transmutation.TransmuteOneFromHand(this, choiceContext, _ => chosen.CreateClone());
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
