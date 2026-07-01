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

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻灭 (DisillusionIllusionist) — 1 cost Skill, Common (upgraded: choose 2 cards instead of 1).
/// Choose a card in your hand and 幻化 (transmute) it into a 暗淡油灯 (Dim Lamp) until end of turn. Cash
/// in the Lamp's energy/draw now; if unplayed it reverts to the original card at the start of your next
/// turn.
/// </summary>
public sealed class DisillusionIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
        HoverTipFactory.FromCard<DimLampIllusionist>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(1),
    };

    public DisillusionIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;
        int count = base.DynamicVars.Cards.IntValue;

        // Pick N (1, upgraded 2) hand cards and 幻化 each into a Dim Lamp (reverts next turn if unplayed).
        List<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext, owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, count),
            null,
            this)).ToList();

        await Transmutation.TransmuteCards(selected, this, choiceContext,
            original => original.CardScope!.CreateCard<DimLampIllusionist>(original.Owner));
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
