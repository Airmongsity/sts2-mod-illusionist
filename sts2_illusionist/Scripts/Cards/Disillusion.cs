using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻灭 (DisillusionIllusionist) — 1 cost Skill, Uncommon (upgraded: 0 cost).
/// Choose a card in your hand and 幻化 (transmute) it into an 熄灭油灯 (Extinguished Lamp) until end of
/// turn; if unplayed it reverts to the original card at the start of your next turn.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "DISILLUSION")]
public sealed class DisillusionIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Transmute };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<ExtinguishedLampIllusionist>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(1),
    };

    public DisillusionIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;
        int count = base.DynamicVars.Cards.IntValue;

        // Pick N (1) hand cards and 幻化 each into an Extinguished Lamp (reverts next turn if unplayed).
        List<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext, owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, count),
            null,
            this)).ToList();

        await Transmutation.TransmuteCards(selected, this, choiceContext,
            original => original.CardScope!.CreateCard<ExtinguishedLampIllusionist>(original.Owner));
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
