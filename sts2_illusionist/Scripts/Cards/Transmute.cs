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
/// 幻化 (TransmuteIllusionist) — 1 cost Skill, Uncommon, Exhaust (upgraded: pick from 5 instead of 3).
/// Look at 3 random cards from your exhaust pile, choose one, then 幻化 a card in your hand into a
/// copy of it (carrying that card's upgrades/enchantments). Upgraded: pick from 5. Exhausts itself.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "TRANSMUTE_ILLUSIONIST")]
public sealed class TransmuteIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
    };

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
        owner.RunState.Rng.CombatCardSelection.Shuffle(pool);
        List<CardModel> choices = pool.Take(base.DynamicVars.Cards.IntValue).ToList();

        CardModel? chosen = (await CardSelectCmd.FromSimpleGrid(
            choiceContext, choices, owner,
            new CardSelectorPrefs(new LocString("cards", "TRANSMUTE_ILLUSIONIST.selectionScreenPrompt"), 1))).FirstOrDefault();
        if (chosen == null)
        {
            return;
        }

        // 2) Choose a hand card and 幻化 it into a copy of the chosen exhaust-pile card.
        await Transmutation.TransmuteOneFromHand(this, choiceContext, _ => chosen.CreateClone());
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(10m);
    }
}
