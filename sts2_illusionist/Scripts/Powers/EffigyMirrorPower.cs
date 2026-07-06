using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 傀影 (Effigy Mirror) — Rare mirror type. The first card you Transmute each turn, the mirror plays the
/// top card of your draw pile for free (one per stack). Fired from <see cref="Illusionist.Scripts.Transmutation.NotifyTransformed"/>,
/// the same choke point 即兴 (Improvise) uses; the per-turn flag is set BEFORE the auto-plays so a
/// transmute triggered by an auto-played card can't re-fire it. Auto-play (<see cref="CardCmd.AutoPlay"/>
/// with a null target) moves the card into the play pile and then discards/exhausts it, so a card is never
/// left in the draw pile or played twice.
/// </summary>
[RegisterPower]
public sealed class EffigyMirrorPower : MirrorTypePower
{
    private sealed class Data
    {
        public bool FiredThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player.Creature)
        {
            GetInternalData<Data>().FiredThisTurn = false;
        }
        return Task.CompletedTask;
    }

    /// <summary>Called by <see cref="Illusionist.Scripts.Transmutation.NotifyTransformed"/> on each transmute.</summary>
    public async Task OnTransmuted(PlayerChoiceContext choiceContext)
    {
        Data data = GetInternalData<Data>();
        if (data.FiredThisTurn || base.Amount <= 0m)
        {
            return;
        }

        Player? player = base.Owner.Player;
        if (player == null)
        {
            return;
        }

        // Set before auto-playing so a transmute triggered by an auto-played card can't re-fire Effigy.
        data.FiredThisTurn = true;
        Flash();

        int plays = (int)base.Amount;
        for (int i = 0; i < plays; i++)
        {
            if (base.CombatState == null || !base.CombatState.IsLiveCombat())
            {
                break;
            }

            // Top of the draw pile is index 0 (MoveToTopInternal inserts at 0; Draw takes from the top).
            CardModel? top = PileType.Draw.GetPile(player).Cards.FirstOrDefault();
            if (top == null)
            {
                break;
            }

            try
            {
                await CardCmd.AutoPlay(choiceContext, top, null);
            }
            catch (Exception ex)
            {
                Log.Error($"[illusionist] EffigyMirror auto-play failed: {ex}");
                break;
            }
        }
    }
}
