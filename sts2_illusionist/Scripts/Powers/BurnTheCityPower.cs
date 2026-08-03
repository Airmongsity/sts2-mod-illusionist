using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts.Afflictions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 焚城: after the normal turn-start draw, choose one eligible hand card and afflict it with
/// Scorching. Reuses the existing Phantasm Storm icon while this replacement is under test.
/// </summary>
[RegisterPower]
public sealed class BurnTheCityPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomIconPath =>
        IllusionistArtPaths.PowerIcon(typeof(PhantasmStormPower));

    public override string? CustomBigIconPath =>
        IllusionistArtPaths.PowerIcon(typeof(PhantasmStormPower));

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature)
        {
            return;
        }

        Scorching scorching = ModelDb.Affliction<Scorching>();
        CardPile hand = PileType.Hand.GetPile(player);
        int selectionCount = System.Math.Min(
            (int)base.Amount,
            hand.Cards.Count(scorching.CanAfflict));
        if (selectionCount <= 0)
        {
            return;
        }

        Flash();
        foreach (CardModel selected in await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(
                new LocString("cards", "ILLUSIONIST_CARD_PHANTASM_STORM.selectionScreenPrompt"),
                selectionCount),
            scorching.CanAfflict,
            this))
        {
            await CardCmd.Afflict<Scorching>(selected, 1);
        }
    }
}
