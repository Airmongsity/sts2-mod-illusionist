using System;
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
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻灭 (Disillusion) — 1 cost Skill, Common (upgraded: 3 cards instead of 1).
/// Choose 3 cards from your draw pile and 幻化 (transmute) them into 暗淡油灯 (Dim Lamps), then
/// transmute THIS card into a Dim Lamp too (added to your hand). The Lamps revert to their originals
/// at the start of your next turn — a burst of "draw + energy" you cash in this turn.
/// </summary>
public sealed class Disillusion : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Transmute,
        HoverTipFactory.FromCard<DimLamp>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(1),
    };

    public Disillusion()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // This card turns into a Dim Lamp on play, so it must NOT also go to the discard pile.
    protected override PileType GetResultPileTypeForCardPlay() => PileType.None;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;

        // 1) Transmute up to N draw-pile cards into Dim Lamps (the same FromSimpleGrid picker 幻化 uses).
        List<CardModel> drawCards = PileType.Draw.GetPile(owner).Cards.ToList();
        if (drawCards.Count > 0)
        {
            int count = base.DynamicVars.Cards.IntValue;
            List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext, drawCards, owner,
                new CardSelectorPrefs(base.SelectionScreenPrompt, count))).ToList();

            await Transmutation.TransmuteCards(selected, this, choiceContext,
                original => original.CardScope!.CreateCard<DimLamp>(original.Owner));
        }

        // 2) Transmute THIS card into a Dim Lamp too. It can't safely transform itself mid-play
        //    (hangs), so — like 淬毒 — it removes itself via the None result pile, drops a Dim Lamp in
        //    hand, and registers it to revert into a clone of this card next turn.
        try
        {
            CardModel revertTo = CreateClone();
            CardModel lamp = base.CardScope!.CreateCard<DimLamp>(owner);
            await CardPileCmd.Add(lamp, PileType.Hand);
            await Transmutation.RegisterRevert(owner, choiceContext, this, revertTo, lamp);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Disillusion: self-transmute failed: {ex}");
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(2m);
    }
}
