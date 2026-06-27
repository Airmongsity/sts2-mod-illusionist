using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 点灯 (KindleIllusionist) — 1 cost Skill, Uncommon (upgraded: 3 instead of 2).
/// Add 2 Dazed to your draw pile, then 幻化 each into a 暗淡油灯 (Dim Lamp). The Lamp reverts to the
/// Dazed at the end of the turn (transmute stack), so it's a strong but fleeting "draw + energy" you
/// must dig up and play this turn. Upgraded: add 3.
/// </summary>
public sealed class KindleIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
        HoverTipFactory.FromCard<DimLampIllusionist>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(2),
    };

    public KindleIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = base.DynamicVars.Cards.IntValue;

        // Add N Dazed to the draw pile, collecting the actual added instances.
        List<CardModel> dazes = new List<CardModel>();
        for (int i = 0; i < count; i++)
        {
            CardModel daze = base.CardScope!.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Dazed>(base.Owner);
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(daze, PileType.Draw, base.Owner);
            if (result.cardAdded != null)
            {
                dazes.Add(result.cardAdded);
            }
        }

        // 幻化 each Dazed into a Dim Lamp (reverts back to the Dazed at end of turn via the stack).
        await Transmutation.TransmuteCards(dazes, this, choiceContext,
            original => original.CardScope!.CreateCard<DimLampIllusionist>(original.Owner));
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
