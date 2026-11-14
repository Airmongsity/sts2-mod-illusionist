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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻形 (Shapeshift) — 1 cost Skill, Uncommon, Exhaust. Look at random cards from your exhaust pile,
/// choose one, then 幻化 a card in your hand into a copy of it (carrying that card's
/// upgrades/enchantments). Upgraded: no longer Exhausts.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "TRANSMUTE")]
public sealed class TransmuteIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust, IllusionistKeywords.Transmute };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(10),
    };

    public TransmuteIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;

        List<CardModel> pool = PileType.Exhaust.GetPile(owner).Cards.ToList();
        if (pool.Count == 0)
        {
            return;
        }

        // 1) Offer N random cards from the exhaust pile (seeded shuffle), and pick one.
        // Rng.Shuffle was removed in 0.113. Preserve its Fisher-Yates order
        // and consume the same selection RNG stream once per swap.
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = owner.RunState.Rng.CombatCardSelection.NextInt(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        List<CardModel> choices = pool.Take(base.DynamicVars.Cards.IntValue).ToList();

        CardModel? chosen = (await CardSelectCmd.FromSimpleGrid(
            choiceContext, choices, owner,
            new CardSelectorPrefs(new LocString("cards", "ILLUSIONIST_CARD_TRANSMUTE.selectionScreenPrompt"), 1))).FirstOrDefault();
        if (chosen == null)
        {
            return;
        }

        // 2) Choose a hand card and 幻化 it into a copy of the chosen exhaust-pile card.
        await Transmutation.TransmuteOneFromHand(this, choiceContext, _ => chosen.CreateClone());
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
