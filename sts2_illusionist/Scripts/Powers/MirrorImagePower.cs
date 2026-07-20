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
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;
using STS2RitsuLib;
using STS2RitsuLib.Models.Capabilities;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 镜像 (Mirror Image) — THE mirror power, one stack per mirror. Fifth design (see next-mirror.md):
/// mirrors are a fragile auto-firing battery of stored cards.
///
/// <para><b>Store:</b> whenever a card from your hand is exhausted during a manual card play (and a
/// FREE mirror exists), it is pulled out of combat and stored into that mirror - including curses/statuses
/// (unplayable cards are consumed when fired rather than jamming the slot). Additionally, the first card YOU
/// (manually — auto-plays don't count) play each turn, if it wasn't exhausted by its play, imprints an
/// Exhaust-keyword COPY of itself into a free mirror — the engine's built-in fuel line
/// (<see cref="CanImprintFirstCard"/> filters what may imprint).</para>
///
/// <para><b>Volley:</b> at the start of your turn, AFTER the transmute revert
/// (<see cref="TransmutePower.EnsureTurnStartRevert"/>), every LOADED mirror plays its stored card,
/// newest first. Attack/Skill/Status cards without Exhaust return to the same mirror after the
/// first shot and gain Exhaust, so the second shot spends them. Cards that already have Exhaust are
/// spent immediately. Power cards resolve like normal powers and disappear. Mirror-fired plays never
/// re-store themselves and never count as the turn's first card — no loops.</para>
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
    public const int Cap = 9;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private sealed class Data
    {
        /// <summary>Mirrors with nothing stored. Invariant: MirrorPile.Count + resolving mirrors + Empty == Amount.</summary>
        public int Empty;

        /// <summary>Round-robin volley cursor: the next single-target mirror fire hits enemies[Cursor % count].
        /// Persists across turns (the power lives the whole combat). Self/AoE fires don't advance it.</summary>
        public int VolleyCursor;
    }

    private List<CardModel> LoadedCards
    {
        get
        {
            Player? player = base.Owner.Player;
            return player == null ? new List<CardModel>() : MirrorPile.Get(player).Cards.ToList();
        }
    }

    /// <summary>The cards currently stored in mirrors (live view, no snapshot/alloc). Internal for
    /// 共鸣 (Resonance) to match hand cards against the mirror's contents by type.</summary>
    internal IEnumerable<CardModel> StoredCards
    {
        get
        {
            Player? player = base.Owner.Player;
            return player == null ? Enumerable.Empty<CardModel>() : MirrorPile.Get(player).Cards;
        }
    }

    private int MirrorCapacity => Math.Max(0, (int)base.Amount);

    private int LoadedCount => LoadedCards.Count;

    private int OccupiedMirrorCount => LoadedCount + FiringMirrorCount;

    private int FiringMirrorCount
    {
        get
        {
            List<CardModel> loaded = LoadedCards;
            return _firingOrder.Count(card =>
                !_destroyedWhileFiring.Contains(card) && !loaded.Contains(card));
        }
    }

    // Cards currently being fired out of a mirror: their (re-)exhaust must NOT re-store them.
    private readonly HashSet<CardModel> _noRestore = new();

    // Cards currently being fired out of a mirror must not be fired again by active mirror destruction.
    private readonly HashSet<CardModel> _firing = new();

    // Stack of mirror-fired cards currently resolving. While a card is temporarily outside the mirror pile,
    // active mirror destruction still needs to target that resolving mirror before older loaded mirrors.
    private readonly List<CardModel> _firingOrder = new();

    private readonly HashSet<CardModel> _destroyedWhileFiring = new();

    private int _mirrorPlayDepth;

    // The exact cards the player manually played from hand and that may still report Exhaust later in
    // the engine's play pipeline. AutoPlay, mirror volleys, exhaust-pile effects, and unrelated hand
    // cards should not reload mirrors.
    private readonly Dictionary<CardModel, PileType> _lastMoveSourcePile = new();

    private readonly Dictionary<CardModel, PileType> _autoPlaySourcePile = new();

    private readonly List<CardModel> _pendingManualExhausts = new();

    private bool _pendingManualExhaustFlushQueued;

    // Set once the turn's first manual card play has been seen (imprint considered exactly once/turn).
    private bool _firstCardSeenThisTurn;

    protected override object InitInternalData()
    {
        return new Data();
    }

    internal static void OnCardMovedBetweenPiles(CardMovedBetweenPilesEvent evt)
    {
        Player? player = evt.Card.Owner;
        if (player == null)
        {
            return;
        }

        MirrorImagePower? power = player.Creature.GetPower<MirrorImagePower>();
        if (power == null)
        {
            return;
        }

        power._lastMoveSourcePile[evt.Card] = evt.PreviousPile;
    }

    internal static void OnCardAutoPlaying(CardAutoPlayingEvent evt)
    {
        Player? player = evt.Card.Owner;
        if (player == null)
        {
            return;
        }

        MirrorImagePower? power = player.Creature.GetPower<MirrorImagePower>();
        if (power == null)
        {
            return;
        }

        CardPile? pile = evt.Card.Pile;
        if (pile != null)
        {
            power._autoPlaySourcePile[evt.Card] = pile.Type;
        }
    }

    private PileType? GetLastMoveSourcePile(CardModel card)
    {
        return _lastMoveSourcePile.TryGetValue(card, out PileType sourcePile)
            ? sourcePile
            : null;
    }

    private PileType? GetAutoPlaySourcePile(CardModel card)
    {
        return _autoPlaySourcePile.TryGetValue(card, out PileType sourcePile)
            ? sourcePile
            : null;
    }

    /// <summary>
    /// Cosmetic power-icon flash, hardened against the Godot Callable-handle fault that fires when
    /// <see cref="Flash"/> routes through NCreature.OnPowerFlashed -> Godot.Callable.CallDeferred during
    /// mass mirror destruction. The known trigger: a mirror fires a mirror-destroying card (萃取/Extract),
    /// which re-enters <see cref="MirrorClone.ConsumeAll"/> mid-volley; on the threadpool continuation the
    /// deferred delegate handle can be invalid, so DelegateHash throws "Handle is not initialized". Godot
    /// logs but re-throws, faulting the async command chain and hanging combat (Cmd.Wait never resumes).
    /// The flash is purely visual, so a failure here must never break storing/firing/death - log and carry on.
    /// </summary>
    private void SafeFlash()
    {
        try
        {
            Flash();
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorImage: power-icon flash ignored: {ex.Message}");
        }
    }

    private void NotifyMirrorPileAdd()
    {
        NotifyMirrorPileAdd(base.Owner.Player);
    }

    private void NotifyMirrorPileRemove()
    {
        NotifyMirrorPileRemove(base.Owner.Player);
    }

    private void NotifyMirrorPileChanged()
    {
        NotifyMirrorPileChanged(base.Owner.Player);
    }

    private static void NotifyMirrorPileAdd(Player? player)
    {
        CardPile? mirrorPile = player == null ? null : MirrorPile.Get(player);
        if (mirrorPile == null)
        {
            return;
        }

        mirrorPile.InvokeCardAddFinished();
        mirrorPile.InvokeContentsChanged();
    }

    private static void NotifyMirrorPileRemove(Player? player)
    {
        CardPile? mirrorPile = player == null ? null : MirrorPile.Get(player);
        if (mirrorPile == null)
        {
            return;
        }

        mirrorPile.InvokeCardRemoveFinished();
        mirrorPile.InvokeContentsChanged();
    }

    private static void NotifyMirrorPileChanged(Player? player)
    {
        CardPile? mirrorPile = player == null ? null : MirrorPile.Get(player);
        if (mirrorPile == null)
        {
            return;
        }

        mirrorPile.InvokeContentsChanged();
    }

    private int SyncEmptySlots(Data data)
    {
        int empty = Math.Max(0, MirrorCapacity - OccupiedMirrorCount);
        data.Empty = empty;
        return empty;
    }

    private async Task TrimOverflow(Data data)
    {
        List<CardModel> loaded = LoadedCards;
        while (loaded.Count > MirrorCapacity && loaded.Count > 0)
        {
            CardModel overflow = loaded[^1];
            Log.Warn($"[illusionist] MirrorImage: overflow corrected by ejecting '{overflow.Title}' ({loaded.Count} loaded, {MirrorCapacity} mirrors).");
            await DropToExhaustPile(overflow);
            loaded = LoadedCards;
        }

        SyncEmptySlots(data);
    }

    internal int TotalMirrors
    {
        get
        {
            return MirrorCapacity;
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
            List<CardModel> loaded = LoadedCards;
            if (loaded.Count == 0)
            {
                return Array.Empty<IHoverTip>();
            }

            return Enumerable.Reverse(loaded).Select(c => HoverTipFactory.FromCard(c)).ToList();
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
        // 镜界 (MirrorCapUp): each stack widens the cap beyond the base 8, so Copy can fill past it.
        int cap = Cap + (int)(owner.GetPower<MirrorCapUpPower>()?.Amount ?? 0m);
        if (power != null && power.TotalMirrors >= cap)
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
    /// until fired. Curses, statuses, and other normally-unplayable cards DO enter mirrors now; when fired
    /// they are consumed (sent to the exhaust pile) rather than played, so they free the slot instead of
    /// jamming it.
    /// </summary>
    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        Player? player = base.Owner.Player;
        if (player == null || card.Owner != player)
        {
            return Task.CompletedTask;
        }

        // A card fired out of a mirror goes to the exhaust pile for real — never back in (no loops).
        if (_noRestore.Contains(card))
        {
            return Task.CompletedTask;
        }

        PileType? sourcePile = GetLastMoveSourcePile(card);
        PileType? autoPlaySourcePile = GetAutoPlaySourcePile(card);
        _lastMoveSourcePile.Remove(card);
        _autoPlaySourcePile.Remove(card);
        if (sourcePile == PileType.Exhaust
            || sourcePile == MirrorPile.Type
            || autoPlaySourcePile == PileType.Exhaust
            || autoPlaySourcePile == MirrorPile.Type)
        {
            return Task.CompletedTask;
        }

        if (!_pendingManualExhausts.Contains(card))
        {
            _pendingManualExhausts.Add(card);
        }

        QueuePendingManualExhaustFlush();

        return Task.CompletedTask;
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
        if (SyncEmptySlots(data) <= 0)
        {
            return;
        }

        // Clone AFTER the play resolved (carries in-play state), stamp Exhaust so the copy is spent
        // after its single mirror shot, register it with combat, then pull it into the mirror.
        CardModel copy = source.CreateClone();
        CardCmd.ApplyKeyword(copy, CardKeyword.Exhaust);
        await CardPileCmd.AddGeneratedCardToCombat(copy, MirrorPile.Type, player);
        await StoreCard(data, copy, removeFromCombat: false);
        Log.Info($"[illusionist] MirrorImage: imprinted first-card copy of '{source.Title}'.");
    }

    private void QueuePendingManualExhaustFlush()
    {
        if (_pendingManualExhaustFlushQueued)
        {
            return;
        }

        _pendingManualExhaustFlushQueued = true;
        _ = FlushPendingManualExhaustsAfterPlay();
    }

    private async Task FlushPendingManualExhaustsAfterPlay()
    {
        try
        {
            await Cmd.Wait(0.05f);
            await StorePendingManualExhausts();
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorImage: pending manual exhaust flush failed: {ex}");
        }
        finally
        {
            _pendingManualExhaustFlushQueued = false;
            if (_pendingManualExhausts.Count > 0)
            {
                QueuePendingManualExhaustFlush();
            }
        }
    }

    private async Task StorePendingManualExhausts()
    {
        Data data = GetInternalData<Data>();
        foreach (CardModel card in _pendingManualExhausts.ToList())
        {
            if (SyncEmptySlots(data) <= 0)
            {
                break;
            }

            if (LoadedCards.Contains(card) || card.Pile == null || card.Pile.Type != PileType.Exhaust)
            {
                continue;
            }

            await StoreCard(data, card, removeFromCombat: true);
        }

        _pendingManualExhausts.Clear();
    }

    private async Task StoreCard(Data data, CardModel card, bool removeFromCombat)
    {
        bool alreadyInMirror = MirrorPile.IsMirrorPile(card.Pile);
        if (!alreadyInMirror && SyncEmptySlots(data) <= 0)
        {
            return;
        }

        if (removeFromCombat)
        {
            try
            {
                // Pull the card out of its pile, then place it in the dedicated mirror pile.
                await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
            }
            catch (Exception ex)
            {
                Log.Error($"[illusionist] MirrorImage: store failed for '{card.Title}': {ex}");
                return;
            }
        }

        card.HasBeenRemovedFromState = false;
        if (MirrorPile.IsMirrorPile(card.Pile))
        {
            if (OccupiedMirrorCount > MirrorCapacity)
            {
                Log.Warn($"[illusionist] MirrorImage: store refused for '{card.Title}' because mirrors are full ({LoadedCount} loaded, {FiringMirrorCount} firing, {MirrorCapacity} mirrors).");
                await DropToExhaustPile(card);
                SyncEmptySlots(data);
                return;
            }

            SyncEmptySlots(data);
            NotifyMirrorPileAdd();
            SafeFlash();
            Log.Info($"[illusionist] MirrorImage: stored '{card.Title}' ({LoadedCount} loaded, {data.Empty} empty).");
            return;
        }

        try
        {
            await CardPileCmd.Add(card, MirrorPile.Type, CardPilePosition.Bottom, null, skipVisuals: true);
        }
        catch (Exception ex)
        {
            SyncEmptySlots(data);
            Log.Error($"[illusionist] MirrorImage: store failed for '{card.Title}': {ex}");
            return;
        }

        if (OccupiedMirrorCount > MirrorCapacity)
        {
            Log.Warn($"[illusionist] MirrorImage: store overflow corrected for '{card.Title}' ({LoadedCount} loaded, {FiringMirrorCount} firing, {MirrorCapacity} mirrors).");
            await DropToExhaustPile(card);
            SyncEmptySlots(data);
            return;
        }

        SyncEmptySlots(data);
        NotifyMirrorPileAdd();
        SafeFlash();
        Log.Info($"[illusionist] MirrorImage: stored '{card.Title}' ({LoadedCount} loaded, {data.Empty} empty).");
    }

    // ------------------------------------------------------------------ turn cycle

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player.Creature)
        {
            _firstCardSeenThisTurn = false;
            _pendingManualExhausts.Clear();
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
        await TrimOverflow(data);
        List<CardModel> loaded = LoadedCards;
        if (loaded.Count == 0)
        {
            return;
        }

        // Snapshot: cards stored DURING the volley (exhausts of fired non-token cards, etc.) wait for
        // next turn; cards that die out from under us are skipped via the Contains check.
        List<CardModel> snapshot = loaded;
        for (int i = snapshot.Count - 1; i >= 0; i--)
        {
            CardModel card = snapshot[i];
            if (!LoadedCards.Contains(card))
            {
                continue;
            }

            await FireOne(choiceContext, data, card);
        }
    }

    /// <summary>
    /// Fire one mirror's stored card. Attack/Skill/Status cards without Exhaust return once and gain Exhaust,
    /// so a non-Exhaust card consumed into a mirror fires across the next two turns instead of forever.
    /// Cards that already had Exhaust are spent immediately. Power cards resolve and disappear, matching
    /// normal player-played powers.
    /// </summary>
    private async Task FireOne(PlayerChoiceContext choiceContext, Data data, CardModel card, Creature? forcedTarget = null)
    {
        SafeFlash();
        _noRestore.Add(card);
        _firing.Add(card);
        _firingOrder.Add(card);
        _mirrorPlayDepth++;
        try
        {
            int slot = LoadedCards.IndexOf(card);
            if (slot < 0)
            {
                return;
            }

            // Keep the stored card in the mirror pile while AutoPlay starts, so the source/flight target stays
            // anchored to the mirror UI instead of the vanilla exhaust pile. Legacy pile-less cards are first
            // revived into the mirror pile.
            if (card.Pile == null)
            {
                card.HasBeenRemovedFromState = false;
                await InsertStoredAt(card, slot);
            }

            Log.Info($"[illusionist] MirrorImage: firing '{card.Title}'.");
            Creature? volleyTarget = forcedTarget != null
                ? (card.TargetType == TargetType.AnyEnemy ? forcedTarget : null)
                : PickVolleyTarget(card, data);

            bool isAttackSkillOrStatus = card.Type == CardType.Attack
                || card.Type == CardType.Skill
                || card.Type == CardType.Status;
            bool isPower = card.Type == CardType.Power;
            bool hadExhaustAtFire = card.Keywords.Contains(CardKeyword.Exhaust);
            bool canFireFromMirror = isAttackSkillOrStatus || isPower || hadExhaustAtFire;
            bool unplayable = card.Keywords.Contains(CardKeyword.Unplayable);
            bool shouldReturnToMirrorAfterPlay = isAttackSkillOrStatus && !hadExhaustAtFire && !unplayable;
            MirrorPlayResultPileCapability? mirrorResultPile = null;

            if (canFireFromMirror && !unplayable)
            {
                if (shouldReturnToMirrorAfterPlay)
                {
                    mirrorResultPile = new MirrorPlayResultPileCapability();
                    card.Capabilities().Insert(0, mirrorResultPile);
                }

                try
                {
                    await CardCmd.AutoPlay(choiceContext, card, volleyTarget);
                }
                finally
                {
                    if (mirrorResultPile != null)
                    {
                        card.Capabilities().Remove(mirrorResultPile);
                    }
                }
            }

            bool mirrorDestroyedWhileFiring = _destroyedWhileFiring.Contains(card);
            if (mirrorDestroyedWhileFiring)
            {
                await DropToExhaustPile(card);
            }
            else if (isPower && canFireFromMirror && !unplayable)
            {
                if (card.Pile != null)
                {
                    bool removedFromMirror = MirrorPile.IsMirrorPile(card.Pile);
                    await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
                    if (removedFromMirror)
                    {
                        NotifyMirrorPileRemove();
                    }
                }

                SyncEmptySlots(data);
            }
            else if (hadExhaustAtFire || card.Pile?.Type == PileType.Exhaust)
            {
                // The card entered this volley with Exhaust, so this shot spends it. If AutoPlay did
                // not already put it in the exhaust pile, move it there without letting mirror restore
                // hooks recapture it.
                if (unplayable)
                {
                    await DropToExhaustPile(card);
                }
                else if (card.Pile != null && card.Pile.Type != PileType.Exhaust)
                {
                    await CardCmd.Exhaust(choiceContext, card);
                    NotifyMirrorPileRemove();
                }
                else if (card.Pile == null)
                {
                    await DropToExhaustPile(card);
                }
                else
                {
                    NotifyMirrorPileRemove();
                }

                SyncEmptySlots(data);
            }
            else if (isAttackSkillOrStatus && canFireFromMirror && !unplayable)
            {
                CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
                if (card.Pile == null)
                {
                    card.HasBeenRemovedFromState = false;
                }

                // First shot of a non-Exhaust Attack/Skill/Status: keep it loaded, but the next mirror shot
                // will see Exhaust and spend it.
                await KeepStoredAt(card, slot);
                SyncEmptySlots(data);
            }
            else if (card.Pile == null)
            {
                card.HasBeenRemovedFromState = false;
                await InsertStoredAt(card, slot);
                SyncEmptySlots(data);
            }
            // Other non-Exhaust cards stay in the mirror without firing. This keeps unknown future
            // card types from becoming free repeat effects unless they opt in via Exhaust.
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorImage: fire failed for '{card.Title}': {ex}");
        }
        finally
        {
            _mirrorPlayDepth--;
            _firingOrder.Remove(card);
            _destroyedWhileFiring.Remove(card);
            _firing.Remove(card);
            _noRestore.Remove(card);
        }
    }

    /// <summary>
    /// Fire every loaded mirror's stored card at a single chosen enemy (聚焦 / Focus Fire's directed
    /// volley). Unlike the turn-start volley (round-robin), every single-target card fires at this enemy;
    /// self/AoE cards fire without a target as usual. Mirrors are NOT consumed (a non-spent stored card
    /// returns to its slot) - this is a directed volley, not a detonation.
    /// </summary>
    internal async Task FireAllAtTarget(PlayerChoiceContext choiceContext, Creature target)
    {
        Data data = GetInternalData<Data>();
        List<CardModel> snapshot = LoadedCards;
        if (snapshot.Count == 0)
        {
            return;
        }

        for (int i = snapshot.Count - 1; i >= 0; i--)
        {
            CardModel card = snapshot[i];
            if (!LoadedCards.Contains(card))
            {
                continue;
            }

            await FireOne(choiceContext, data, card, forcedTarget: target);
        }
    }

    /// <summary>
    /// Round-robin volley target for a single-target (AnyEnemy) stored card. Returns null (no target, no
    /// cursor advance) for Self/AllEnemies cards - those need no target. The cursor persists across turns:
    /// with N enemies, single-target fires hit 1,2,...,N,1,2,... continuing across volleys (3 enemies, 5
    /// mirrors: turn 1 hits 1,2,3,1,2; turn 2 hits 3,1,2,3,1). If enemies die mid-volley the modulo adapts
    /// to the new count. Replaces the old pure-random target (null) so the volley is deterministic.
    /// </summary>
    private Creature? PickVolleyTarget(CardModel card, Data data)
    {
        if (card.TargetType != TargetType.AnyEnemy)
        {
            return null;
        }

        ICombatState? combat = base.Owner.CombatState;
        if (combat == null)
        {
            return null;
        }

        List<Creature> enemies = combat.HittableEnemies.ToList();
        if (enemies.Count == 0)
        {
            return null;
        }

        Creature target = enemies[data.VolleyCursor % enemies.Count];
        data.VolleyCursor++;
        return target;
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

        // 虚实转换 (Phase Shift): the next unblocked hit is absorbed without losing a mirror -
        // consume one stack as the first responder, before DieOne kills one.
        PhaseShiftPower? phase = base.Owner.GetPower<PhaseShiftPower>();
        if (phase != null)
        {
            await ConsumePhaseShift(phase);
            return;
        }

        await DieOne(choiceContext);
    }

    /// <summary>
    /// Spend one 虚实转换 (PhaseShift) stack so a mirror survives the current unblocked hit. The power
    /// is a pure counter consumed by <see cref="AfterDamageReceived"/>; it has no hook of its own.
    /// </summary>
    private async Task ConsumePhaseShift(PhaseShiftPower phase)
    {
        SafeFlash();
        if (phase.Amount > 1m)
        {
            await PowerCmd.Decrement(phase);
        }
        else
        {
            await PowerCmd.Remove(phase);
        }
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
        SyncEmptySlots(data);
        if (data.Empty > 0)
        {
            data.Empty--;
        }
        else if (_firingOrder.Count > 0)
        {
            CardModel card = _firingOrder[^1];
            _destroyedWhileFiring.Add(card);
            await DropToExhaustPile(card);
        }
        else
        {
            CardModel card = LoadedCards[^1];
            await DropToExhaustPile(card);
            SyncEmptySlots(data);
        }

        SafeFlash();
        await MirrorClone.ShatterOneClone(player);
        await RemoveOneStack();
        if (base.Owner.GetPower<MirrorImagePower>() == this)
        {
            SyncEmptySlots(data);
        }
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
        SyncEmptySlots(data);
        if (data.Empty > 0)
        {
            data.Empty--;
        }
        else
        {
            CardModel card = _firingOrder.Count > 0 ? _firingOrder[^1] : LoadedCards[^1];
            bool destroyedWhileFiring = false;
            if (_firing.Contains(card))
            {
                _destroyedWhileFiring.Add(card);
                await DropToExhaustPile(card);
                destroyedWhileFiring = true;
            }
            else
            {
                await FireOne(choiceContext, data, card);
            }

            if (destroyedWhileFiring)
            {
                // Already resolving from this mirror; shatter it without re-firing the card.
                SyncEmptySlots(data);
            }
            else if (LoadedCards.Contains(card))
            {
                // The card survived the shot (non-Exhaust, re-stored) but its mirror is being
                // destroyed — evict it to the exhaust pile.
                await DropToExhaustPile(card);
                SyncEmptySlots(data);
            }
            else
            {
                // The shot spent the card; FireOne already converted the slot to an empty.
                SyncEmptySlots(data);
            }
        }

        SafeFlash();
        await MirrorClone.ShatterOneClone(player);
        await RemoveOneStack();
        if (base.Owner.GetPower<MirrorImagePower>() == this)
        {
            SyncEmptySlots(data);
        }

        return 1;
    }

    private async Task DropToExhaustPile(CardModel card)
    {
        try
        {
            Data data = GetInternalData<Data>();
            card.HasBeenRemovedFromState = false;
            if (card.Pile == null || MirrorPile.IsMirrorPile(card.Pile))
            {
                // Silent pile add — NOT CardCmd.Exhaust, so no exhaust hooks fire and nothing re-stores.
                // skipVisuals=true also silently removes from the source pile (no ContentsChanged /
                // CardRemoved event fires on the mirror pile), so the mod pile button — which only
                // refreshes its count label on those events — would otherwise show a stale count.
                // Nudge the mirror pile's events manually so the button decrements correctly.
                await CardPileCmd.Add(card, PileType.Exhaust, CardPilePosition.Bottom, null, skipVisuals: true);
                Player? player = base.Owner.Player;
                NotifyMirrorPileRemove(player);
            }

            SyncEmptySlots(data);
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

        int index = power.LoadedCards.IndexOf(card);
        if (index < 0)
        {
            return -1;
        }

        card.HasBeenRemovedFromState = false;
        await CardPileCmd.Add(card, PileType.Exhaust, CardPilePosition.Bottom, null, skipVisuals: true);

        // MirrorPile was the source pile of this silent Add; manual refresh (see DropToExhaustPile).
        NotifyMirrorPileRemove(player);
        Data data = power.GetInternalData<Data>();
        power.SyncEmptySlots(data);

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
        if (power.LoadedCards.Contains(card) || power.SyncEmptySlots(data) <= 0)
        {
            return;
        }

        if (card.Pile == null || card.Pile.Type != PileType.Exhaust)
        {
            return;
        }

        await power.InsertStoredAt(card, slot);
        power.SyncEmptySlots(data);
    }

    private async Task KeepStoredAt(CardModel card, int slot)
    {
        if (MirrorPile.IsMirrorPile(card.Pile) && LoadedCards.IndexOf(card) == slot)
        {
            NotifyMirrorPileChanged();
            return;
        }

        if (card.Pile == null)
        {
            card.HasBeenRemovedFromState = false;
        }

        await InsertStoredAt(card, slot);
    }

    private async Task InsertStoredAt(CardModel card, int slot)
    {
        Data data = GetInternalData<Data>();
        List<CardModel> loaded = LoadedCards.Where(loadedCard => !ReferenceEquals(loadedCard, card)).ToList();
        int insertAt = Math.Min(Math.Max(slot, 0), loaded.Count);
        List<CardModel> suffix = loaded.Skip(insertAt).ToList();

        bool removedFromMirror = false;
        if (MirrorPile.IsMirrorPile(card.Pile))
        {
            await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
            card.HasBeenRemovedFromState = false;
            removedFromMirror = true;
        }
        else if (card.Pile != null)
        {
            await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
            card.HasBeenRemovedFromState = false;
        }

        foreach (CardModel displaced in suffix)
        {
            if (displaced.Pile != null)
            {
                await CardPileCmd.RemoveFromCombat(displaced, skipVisuals: true);
                removedFromMirror = true;
            }

            displaced.HasBeenRemovedFromState = false;
        }

        await CardPileCmd.Add(card, MirrorPile.Type, CardPilePosition.Bottom, null, skipVisuals: true);
        foreach (CardModel displaced in suffix)
        {
            await CardPileCmd.Add(displaced, MirrorPile.Type, CardPilePosition.Bottom, null, skipVisuals: true);
        }

        if (removedFromMirror)
        {
            NotifyMirrorPileRemove();
        }

        NotifyMirrorPileAdd();
        SyncEmptySlots(data);
    }

    /// <summary>
    /// A card was transformed (forward 幻化). If a mirror stored the old form, re-point it at the new
    /// form — stored cards participate fully in 幻化.
    /// </summary>
    internal static async Task OnCardTransformed(Player player, CardModel from, CardModel to)
    {
        MirrorImagePower? power = player.Creature.GetPower<MirrorImagePower>();
        if (power == null || MirrorPile.IsMirrorPile(to.Pile))
        {
            return;
        }

        int index = power.LoadedCards.IndexOf(from);
        if (index >= 0)
        {
            await power.InsertStoredAt(to, index);
        }
    }
}

public sealed class MirrorPlayResultPileCapability :
    IModelCapability<CardModel>,
    IModelCapabilityCloneHandler,
    ICardPlayResultContributor
{
    public string CapabilityId => "ILLUSIONIST_RUNTIME_MIRROR_PLAY_RESULT_PILE";

    public CardModel? Owner { get; private set; }

    AbstractModel? IModelCapability.Owner => Owner;

    public void Attach(AbstractModel owner, bool isInternal = false)
    {
        Owner = owner as CardModel ?? throw new ArgumentException(
            "Mirror play result-pile capability can only be attached to cards.",
            nameof(owner));
    }

    public void Detach(bool isInternal = false)
    {
        Owner = null;
    }

    public IModelCapability CloneFor(AbstractModel clonedOwner)
    {
        MirrorPlayResultPileCapability clone = new();
        clone.Attach(clonedOwner, true);
        return clone;
    }

    public PileType? GetResultPileTypeForCardPlay(CardModel card)
    {
        return MirrorPile.Type;
    }
}
