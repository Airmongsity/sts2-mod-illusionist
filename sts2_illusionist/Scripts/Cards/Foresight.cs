using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 预见 (ForesightIllusionist) — 1 cost Skill, Uncommon (upgraded: Retain).
/// Choose up to 8 cards from your draw pile and place them on top in any order.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "FORESIGHT_ILLUSIONIST")]
public sealed class ForesightIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public ForesightIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardPile drawPile = PileType.Draw.GetPile(base.Owner);
        if (drawPile.Cards.Count == 0) return;

        int pickCount = System.Math.Min(8, drawPile.Cards.Count);

        List<CardModel> selected = (await CardSelectCmd.FromCombatPile(
            choiceContext, drawPile, base.Owner,
            new CardSelectorPrefs(
                new LocString("cards", "FORESIGHT_ILLUSIONIST.selectionScreenPrompt"),
                pickCount))).ToList();

        if (selected.Count == 0) return;

        // Place selected cards on top; reverse so the first-picked is on top.
        selected.Reverse();
        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        }

        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
