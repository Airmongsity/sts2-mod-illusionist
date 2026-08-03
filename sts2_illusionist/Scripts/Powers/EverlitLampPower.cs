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
/// Three entry points share one per-turn charge (<see cref="TryConsumeThisTurn"/>):
/// <list type="bullet">
/// <item>The 幻化-into-熄灭油灯 path (Disillusion/Douse/Riposte/PhantomVenom): handled in
/// <see cref="Illusionist.Scripts.Transmutation.TransmuteCards"/> AFTER the 熄灭油灯 is created - it is
/// 幻化-ed into a 暗淡油灯 as a real two-step chain (X -> 熄灭油灯 -> 暗淡油灯, 2-layer revert), not an
/// intercepted swap.</item>
/// <item>The DRAW path: an 熄灭油灯 drawn from the pile (e.g. Myriad Faces dumps lamps into the draw pile)
/// is 幻化-ed here in <see cref="AfterCardDrawn"/> (熄灭油灯 -> 暗淡油灯, 1-layer revert). Previously drawn
/// lamps were ignored - only the transmute path fired.</item>
/// <item>The generated-final-form path: cards such as 突袭 add an 熄灭油灯 directly to hand and then
/// register its earlier form. They call <see cref="TryTransmuteLampInHand"/> after that registration,
/// preserving the full earlier form -> 熄灭油灯 -> 暗淡油灯 chain.</item>
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
        // A drawn 熄灭油灯 counts as appearing in hand. Use the same conversion entry point as
        // forward transmutation and direct generated-card delivery.
        await TryTransmuteLampInHand(choiceContext, card);
    }

    /// <summary>
    /// If <paramref name="card"/> is the owner's 熄灭油灯 currently in hand and this turn's charge
    /// remains, consume the charge and 幻化 it into a 暗淡油灯.
    /// </summary>
    public async Task<bool> TryTransmuteLampInHand(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card is not ExtinguishedLampIllusionist
            || card.Owner?.Creature != base.Owner
            || card.Pile?.Type != PileType.Hand
            || !TryConsumeThisTurn())
        {
            return false;
        }

        int transformed = await Transmutation.TransmuteCards(new[] { card }, card, choiceContext,
            original => original.CardScope!.CreateCard<DimLampIllusionist>(original.Owner));
        return transformed > 0;
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
