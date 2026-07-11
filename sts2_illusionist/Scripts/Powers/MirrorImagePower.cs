using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 镜像 (Mirror Image) — THE mirror power, one stack per mirror. Fifth design (see next-mirror.md):
/// mirrors are a fragile auto-firing battery of stored cards.
///
/// <para><b>Store:</b> whenever a card of yours is exhausted (and a FREE mirror exists, and the card is
/// playable), it is pulled out of combat and stored into that mirror. Additionally, the first card YOU
/// (manually — auto-plays don't count) play each turn, if it wasn't exhausted by its play, imprints an
/// Exhaust-keyword COPY of itself into a free mirror — the engine's built-in fuel line
/// (<see cref="CanImprintFirstCard"/> filters what may imprint).</para>
///
/// <para><b>Volley:</b> at the start of your turn, AFTER the transmute revert
/// (<see cref="TransmutePower.EnsureTurnStartRevert"/>), every LOADED mirror plays its stored card at a
/// random enemy, newest first. A card with Exhaust (or a Power — a consumed power can't stay in a
/// mirror) is spent: it goes to the exhaust pile and the mirror becomes free; other cards return to the
/// same mirror and fire again next turn. Mirror-fired plays never re-store themselves and never count
/// as the turn's first card — no loops.</para>
///
/// <para><b>Death:</b> each instance of unblocked damage / HP loss you take kills ONE mirror — EMPTY
/// mirrors first (they are the armor), then loaded ones LIFO. Death is a PURE LOSS: no payoff, the
/// stored card just drops back into the exhaust pile. Active destruction by cards (引爆/碎影/…) is
/// kinder: a loaded mirror FIRES its stored card first, then shatters
/// (<see cref="ConsumeOne(PlayerChoiceContext)"/>).</para>
///
/// <para><b>Cap:</b> <see cref="Cap"/> mirrors; Copy at the cap simply does nothing.</para>
///
/// Stored cards keep participating in 幻化回退 — at the owner's turn start TransmutePower briefly
/// EJECTS a stored transmuted card back into the exhaust pile (<see cref="EjectForRevert"/>), reverts
/// it through the normal batch (standard preview animation), then RECAPTURES the reverted form into the
/// same stack slot (<see cref="RecaptureAfterRevert"/>) — all before the volley fires. Hovering the
/// power icon lists every stored card (SwipePower's stolen-card pattern). Cosmetic clone pets (one per
/// mirror) are kept in step by <see cref="MirrorClone"/>; their deaths feed AfterDeath listeners
/// (记忆, 不碎之镜).
/// </summary>
[RegisterPower]
public sealed class MirrorImagePower : IllusionistPower
{
    /// <summary>Max mirrors on the board; Copy at the cap is a no-op.</summary>
    public const int Cap = 8;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private sealed class Data
    {
        /// <summary>Stored cards in store order — last = stored most recently = fires first, dies first.</summary>
        public readonly List<CardModel> Loaded = new();

        /// <summary>Mirrors with nothing stored. Invariant: Loaded.Count + Empty == Amount.</summary>
        public int Empty;
    }

    // Cards currently being fired out of a mirror: their (re-)exhaust must NOT re-store them.
    private readonly HashSet<CardModel> _noRestore = new();

    // Cards currently being fired out of a mirror must not be fired again by active mirror destruction.
    private readonly HashSet<CardModel> _firing = new();

    private int _mirrorPlayDepth;

    // Set once the turn's first manual card play has been seen (imprint considered exactly once/turn).
    private bool _firstCardSeenThisTurn;

    protected override object InitInternalData()
    {
        return new Data();
    }

    internal int TotalMirrors
    {
        get
        {
            Data data = GetInternalData<Data>();
            return data.Loaded.Count + data.Empty;
        }
    }

    /// <summary>
    /// Hovering the power icon lists every stored card (newest first = fires first, dies first) — the
    /// same UI the base game uses for Thieving Hopper's stolen card (SwipePower.ExtraHoverTips; routed
    /// through RitsuLib's AdditionalHoverTips extension point).
    /// </summary>
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            Data data = GetInternalData<Data>();
            if (data.Loaded.Count == 0)
            {
                return Array.Empty<IHoverTip>();
            }

            return Enumerable.Reverse(data.Loaded).Select(c => HoverTipFactory.FromCard(c)).ToList();
        }
    }

    /// <summary>
    /// Create ONE empty mirror for <paramref name="player"/> (the primitive behind Copy). At the cap
    /// this does nothing. Does NOT summon the cosmetic clone — <see cref="MirrorClone.Copy"/> handles
    /// visuals (and skips the clone when this returns false).
    /// </summary>
    internal static async Task<bool> CreateOne(Player player, PlayerChoiceContext choiceContext)
    {
        Creature owner = player.Creature;

        MirrorImagePower? power = owner.GetPower<MirrorImagePower>();
        if (power != null && power.TotalMirrors >= Cap)
        {
            return false;
        }

        await PowerCmd.Apply<MirrorImagePower>(choiceContext, owner, 1, owner, null);
        power = owner.GetPower<MirrorImagePower>();
        if (power != null)
        {
            power.GetInternalData<Data>().Empty++;
        }

        return true;
    }

    // ------------------------------------------------------------------ storing

    /// <summary>
    /// Which cards may imprint a first-card copy into a mirror. Currently everything playable —
    /// INCLUDING Powers (a power led first thus effectively applies twice). If that warps power
    /// balance in testing, exclude them here: <c>if (card.Type == CardType.Power) return false;</c>.
    /// </summary>
    private static bool CanImprintFirstCard(CardModel card)
    {
        return !card.Keywords.Contains(CardKeyword.Unplayable);
    }

    /// <summary>
    /// Store an exhausted card into a free mirror, if any. The card is physically removed from combat
    /// (Thieving Hopper's steal move) — it vanishes from the exhaust pile and lives inside the mirror
    /// until fired. Unplayable cards (curses/statuses) never enter mirrors — they'd jam the slot.
    /// </summary>
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        Player? player = base.Owner.Player;
        if (player == null || card.Owner != player)
        {
            return;
        }

        // A card fired out of a mirror goes to the exhaust pile for real — never back in (no loops).
        if (_noRestore.Contains(card))
        {
            return;
        }

        if (card.Keywords.Contains(CardKeyword.Unplayable))
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.Empty <= 0 || data.Loaded.Contains(card))
        {
            return;
        }

        await StoreCard(data, card, removeFromCombat: true);
    }

    /// <summary>
    /// The turn's first card YOU play (manually — mirror volleys and other auto-plays don't count):
    /// if it wasn't exhausted by its play, imprint an Exhaust-keyword copy of it into a free mirror.
    /// A first card that DID exhaust already stored itself via <see cref="AfterCardExhausted"/> —
    /// it only ever fills one mirror.
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = base.Owner.Player;
        if (player == null || cardPlay.Card.Owner != player)
        {
            return;
        }

        if (_mirrorPlayDepth > 0 || cardPlay.IsAutoPlay || _firstCardSeenThisTurn)
        {
            return;
        }

        // This IS the turn's first manual play — consumed whether or not an imprint happens.
        _firstCardSeenThisTurn = true;

        CardModel source = cardPlay.Card;
        if (!CanImprintFirstCard(source))
        {
            return;
        }

        // "如果没有被消耗" — an exhausted first card already fed a mirror through the exhaust path.
        if (source.Keywords.Contains(CardKeyword.Exhaust)
            || cardPlay.ResultPile == PileType.Exhaust
            || source.Pile?.Type == PileType.Exhaust)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.Empty <= 0)
        {
            return;
        }

        // Clone AFTER the play resolved (carries in-play state), stamp Exhaust so the copy is spent
        // after its single mirror shot, register it with combat, then pull it into the mirror.
        CardModel copy = source.CreateClone();
        CardCmd.ApplyKeyword(copy, CardKeyword.Exhaust);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Exhaust, player);
        await StoreCard(data, copy, removeFromCombat: true);
        Log.Info($"[illusionist] MirrorImage: imprinted first-card copy of '{source.Title}'.");
    }

    private async Task StoreCard(Data data, CardModel card, bool removeFromCombat)
    {
        if (removeFromCombat)
        {
            try
            {
                // Pull the card out of its pile and into the mirror. Marks it removed-from-state;
                // firing revives it (clears the flag).
                await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
            }
            catch (Exception ex)
            {
                Log.Error($"[illusionist] MirrorImage: store failed for '{card.Title}': {ex}");
                return;
            }
        }

        data.Empty--;
        data.Loaded.Add(card);
        Flash();
        Log.Info($"[illusionist] MirrorImage: stored '{card.Title}' ({data.Loaded.Count} loaded, {data.Empty} empty).");
    }

    // ------------------------------------------------------------------ turn cycle

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player.Creature)
        {
            _firstCardSeenThisTurn = false;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// The volley: after the transmute revert (forced first, so stored transmuted cards fire in their
    /// reverted form), every loaded mirror plays its card at a random enemy, newest first.
    /// </summary>
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature)
        {
            return;
        }

        // The revert must land before the volley regardless of power-hook order (idempotent per turn).
        TransmutePower? transmute = base.Owner.GetPower<TransmutePower>();
        if (transmute != null)
        {
            await transmute.EnsureTurnStartRevert(choiceContext);
        }

        Data data = GetInternalData<Data>();
        if (data.Loaded.Count == 0)
        {
            return;
        }

        // Snapshot: cards stored DURING the volley (exhausts of fired non-token cards, etc.) wait for
        // next turn; cards that die out from under us are skipped via the Contains check.
        List<CardModel> snapshot = data.Loaded.ToList();
        for (int i = snapshot.Count - 1; i >= 0; i--)
        {
            CardModel card = snapshot[i];
            if (!data.Loaded.Contains(card))
            {
                continue;
            }

            await FireOne(choiceContext, data, card);
        }
    }

    /// <summary>
    /// Fire one mirror's stored card at a random enemy. Spent cards (Exhaust keyword, or Powers —
    /// a consumed power can't stay in a mirror) leave for the exhaust pile and free the mirror;
    /// anything else is pulled back into the same mirror (same stack position) to fire again.
    /// </summary>
    private async Task FireOne(PlayerChoiceContext choiceContext, Data data, CardModel card)
    {
        Flash();
        _noRestore.Add(card);
        _firing.Add(card);
        _mirrorPlayDepth++;
        try
        {
            // Revive: the stored card was removed-from-state; a ghost card no-ops every pile add.
            card.HasBeenRemovedFromState = false;
            if (card.Pile == null)
            {
                await CardPileCmd.Add(card, PileType.Exhaust, CardPilePosition.Bottom, null, skipVisuals: true);
            }

            Log.Info($"[illusionist] MirrorImage: firing '{card.Title}'.");
            await CardCmd.AutoPlay(choiceContext, card, null); // null target → randomized

            bool mirrorStillLoaded = data.Loaded.Contains(card);
            bool spent = card.Keywords.Contains(CardKeyword.Exhaust)
                || card.Type == CardType.Power
                || card.Pile?.Type == PileType.Exhaust;
            if (!mirrorStillLoaded)
            {
                if (card.Pile != null && card.Pile.Type != PileType.Exhaust)
                {
                    await CardCmd.Exhaust(choiceContext, card);
                }
                else if (card.Pile == null)
                {
                    await DropToExhaustPile(card);
                }

                return;
            }

            if (spent)
            {
                int index = data.Loaded.IndexOf(card);
                if (index >= 0)
                {
                    data.Loaded.RemoveAt(index);
                    data.Empty++;
                }

                // "打出后放入消耗牌堆" — if the play didn't already leave it there, move it now.
                if (card.Pile != null && card.Pile.Type != PileType.Exhaust)
                {
                    await CardCmd.Exhaust(choiceContext, card);
                }
            }
            else if (card.Pile != null)
            {
                // Not spent: back into the same mirror (Loaded position untouched → same slot).
                await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorImage: fire failed for '{card.Title}': {ex}");
        }
        finally
        {
            _mirrorPlayDepth--;
            _firing.Remove(card);
            _noRestore.Remove(card);
        }
    }

    // ------------------------------------------------------------------ deaths

    /// <summary>
    /// Every instance of unblocked damage / HP loss the owner takes kills one mirror — a PURE loss
    /// (empty mirrors first, then newest-loaded; the stored card just drops back to the exhaust pile).
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner || result.UnblockedDamage <= 0 || TotalMirrors <= 0)
        {
            return;
        }

        await DieOne(choiceContext);
    }

    /// <summary>Passive death: empty first, then newest loaded; no payoff.</summary>
    internal async Task DieOne(PlayerChoiceContext choiceContext)
    {
        Player? player = base.Owner.Player;
        if (player == null || TotalMirrors <= 0)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.Empty > 0)
        {
            data.Empty--;
        }
        else
        {
            CardModel card = data.Loaded[^1];
            data.Loaded.RemoveAt(data.Loaded.Count - 1);
            await DropToExhaustPile(card);
        }

        Flash();
        await MirrorClone.ShatterOneClone(player);
        await RemoveOneStack();
    }

    /// <summary>
    /// Active destruction by a card ("先打出再摧毁"): same death order (empty first, then newest
    /// loaded), but a loaded mirror FIRES its stored card before shattering. Returns 1 if a mirror
    /// was destroyed.
    /// </summary>
    internal async Task<int> ConsumeOne(PlayerChoiceContext choiceContext)
    {
        Player? player = base.Owner.Player;
        if (player == null || TotalMirrors <= 0)
        {
            return 0;
        }

        Data data = GetInternalData<Data>();
        if (data.Empty > 0)
        {
            data.Empty--;
        }
        else
        {
            CardModel card = data.Loaded[^1];
            bool destroyedWhileFiring = false;
            if (_firing.Contains(card))
            {
                data.Loaded.RemoveAt(data.Loaded.Count - 1);
                destroyedWhileFiring = true;
            }
            else
            {
                await FireOne(choiceContext, data, card);
            }

            if (destroyedWhileFiring)
            {
                // Already resolving from this mirror; shatter it without re-firing the card.
            }
            else if (data.Loaded.Remove(card))
            {
                // The card survived the shot (non-Exhaust, re-stored) but its mirror is being
                // destroyed — evict it to the exhaust pile.
                await DropToExhaustPile(card);
            }
            else
            {
                // The shot spent the card; FireOne already converted the slot to an empty.
                data.Empty--;
            }
        }

        Flash();
        await MirrorClone.ShatterOneClone(player);
        await RemoveOneStack();
        return 1;
    }

    private async Task DropToExhaustPile(CardModel card)
    {
        try
        {
            card.HasBeenRemovedFromState = false;
            if (card.Pile == null)
            {
                // Silent pile add — NOT CardCmd.Exhaust, so no exhaust hooks fire and nothing re-stores.
                await CardPileCmd.Add(card, PileType.Exhaust, CardPilePosition.Bottom, null, skipVisuals: true);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorImage: dropping '{card.Title}' to exhaust pile failed: {ex}");
        }
    }

    private async Task RemoveOneStack()
    {
        if (base.Amount > 1m)
        {
            await PowerCmd.Decrement(this);
        }
        else
        {
            await PowerCmd.Remove(this);
        }
    }

    // ------------------------------------------------------------------ transmute integration

    /// <summary>
    /// Temporarily eject a stored card back into the exhaust pile so the turn-start 幻化回退 can
    /// transform it through the NORMAL batch path (standard preview animation). Frees the slot;
    /// returns the card's stack index for <see cref="RecaptureAfterRevert"/>, or -1 if not stored.
    /// </summary>
    internal static async Task<int> EjectForRevert(Player player, CardModel card)
    {
        MirrorImagePower? power = player.Creature.GetPower<MirrorImagePower>();
        if (power == null)
        {
            return -1;
        }

        Data data = power.GetInternalData<Data>();
        int index = data.Loaded.IndexOf(card);
        if (index < 0)
        {
            return -1;
        }

        data.Loaded.RemoveAt(index);
        data.Empty++;
        card.HasBeenRemovedFromState = false;
        if (card.Pile == null)
        {
            await CardPileCmd.Add(card, PileType.Exhaust, CardPilePosition.Bottom, null, skipVisuals: true);
        }

        return index;
    }

    /// <summary>
    /// Pull a reverted card back into its mirror after the turn-start unwind (the closing half of
    /// <see cref="EjectForRevert"/>), re-inserting at its old stack position so stack order is
    /// preserved. Skips — leaving the card wherever it is and the mirror empty — if the card was
    /// played away, already re-stored by its own exhaust, or the freed slot got taken.
    /// </summary>
    internal static async Task RecaptureAfterRevert(Player player, CardModel card, int slot)
    {
        MirrorImagePower? power = player.Creature.GetPower<MirrorImagePower>();
        if (power == null)
        {
            return;
        }

        Data data = power.GetInternalData<Data>();
        if (data.Loaded.Contains(card) || data.Empty <= 0)
        {
            return;
        }

        if (card.Pile == null || card.Pile.Type != PileType.Exhaust)
        {
            return;
        }

        try
        {
            await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorImage: recapture failed for '{card.Title}': {ex}");
            return;
        }

        data.Empty--;
        data.Loaded.Insert(Math.Min(slot, data.Loaded.Count), card);
    }

    /// <summary>
    /// A card was transformed (forward 幻化). If a mirror stored the old form, re-point it at the new
    /// form — stored cards participate fully in 幻化.
    /// </summary>
    internal static void OnCardTransformed(Player player, CardModel from, CardModel to)
    {
        MirrorImagePower? power = player.Creature.GetPower<MirrorImagePower>();
        if (power == null)
        {
            return;
        }

        Data data = power.GetInternalData<Data>();
        int index = data.Loaded.IndexOf(from);
        if (index >= 0)
        {
            data.Loaded[index] = to;
        }
    }
}
