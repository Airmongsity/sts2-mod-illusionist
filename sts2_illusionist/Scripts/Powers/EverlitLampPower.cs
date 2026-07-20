using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Illusionist.Scripts;
using Illusionist.Scripts.Cards;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 长明灯 (Everlit Lamp) - persistent Power applied by the 长明灯 Rare card. The first time each turn an
/// 熄灭油灯 (Extinguished Lamp) APPEARS in the owner's hand, it is 幻化-ed into a 暗淡油灯 (Dim Lamp).
/// Two entry points share one per-turn charge (<see cref="TryConsumeThisTurn"/>):
/// <list type="bullet">
/// <item>The 幻化-into-熄灭油灯 path (Disillusion/Douse/Riposte/PhantomVenom): handled in
/// <see cref="Illusionist.Scripts.Transmutation.TransmuteCards"/> AFTER the 熄灭油灯 is created - it is
/// 幻化-ed into a 暗淡油灯 as a real two-step chain (X -> 熄灭油灯 -> 暗淡油灯, 2-layer revert), not an
/// intercepted swap.</item>
/// <item>The DRAW path: an 熄灭油灯 drawn from the pile (e.g. Myriad Faces dumps lamps into the draw pile)
/// is 幻化-ed here in <see cref="AfterCardDrawn"/> (熄灭油灯 -> 暗淡油灯, 1-layer revert). Previously drawn
/// lamps were ignored - only the transmute path fired.</item>
/// </list>
/// Hand-only. Single-stack (presence = active); one charge per turn, refreshed at turn start.
/// </summary>
[RegisterPower]
public sealed class EverlitLampPower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    private sealed class Data
    {
        public bool UsedThisTurn;
    }

    protected override object InitInternalData() => new Data();

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player.Creature)
        {
            GetInternalData<Data>().UsedThisTurn = false;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // A drawn 熄灭油灯 counts as "appearing in hand" - 幻化 it into a 暗淡油灯 (熄灭油灯 -> 暗淡油灯,
        // reverts next turn if unplayed). Only the owner's drawn cards, only Extinguished Lamps, only while
        // the charge lasts. The post-transform conversion in TransmuteCards won't re-fire here (this
        // transmute's result is a Dim Lamp, not an Extinguished Lamp), so the charge is consumed exactly
        // once by TryConsumeThisTurn below.
        if (card is not ExtinguishedLampIllusionist || card.Owner?.Creature != base.Owner)
        {
            return;
        }

        if (!TryConsumeThisTurn())
        {
            return;
        }

        await Transmutation.TransmuteCards(new[] { card }, card, choiceContext,
            original => original.CardScope!.CreateCard<DimLampIllusionist>(original.Owner));
    }

    /// <summary>Consume this turn's charge if still available. Returns true if consumed (caller 幻化-s the Lamp into a Dim Lamp).</summary>
    public bool TryConsumeThisTurn()
    {
        Data data = GetInternalData<Data>();
        if (data.UsedThisTurn)
        {
            return false;
        }

        data.UsedThisTurn = true;
        Flash();
        return true;
    }
}
