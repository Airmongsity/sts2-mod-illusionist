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
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 镜像 (Mirror Image) — THE mirror power, one stack per mirror (replaces the old 12-type roster; see
/// next-mirror.md). Mirrors are uniform slots that store exhausted cards and fire them back on death:
///
/// <para><b>Store:</b> whenever a card of yours is exhausted and a FREE (empty) mirror exists, the card is
/// stored into that mirror (bookkeeping — the card physically stays in the exhaust pile; the mirror holds
/// a reference). No free mirror → the exhaust proceeds normally, never a penalty.</para>
///
/// <para><b>Death:</b> each instance of unblocked damage / HP loss / self-damage you take kills ONE mirror
/// (multi-hit attacks kill one per hit; 0-damage hits kill none). Death order is a LIFO stack: the mirror
/// that stored its card most recently dies first, and loaded mirrors always die before empty ones. Active
/// destruction (引爆/汲取/etc.) uses the same order and the same payoffs, one mirror at a time.</para>
///
/// <para><b>Payoff:</b> a dying LOADED mirror plays its stored card — at the enemy that broke it if there
/// is one, else at a random enemy — then puts the card (back) into the exhaust pile; a card played this
/// way is never re-stored by its own exhaust. A dying EMPTY mirror deals <see cref="EmptyDamage"/> to that
/// enemy and grants <see cref="EmptyBlock"/> Block instead.</para>
///
/// <para><b>Cap:</b> at <see cref="Cap"/> mirrors, Copy first bursts the newest-loaded mirror (same as a
/// damage death, random target) and then creates the fresh empty one.</para>
///
/// Stored cards keep participating in 幻化回退 — when a stored card is transformed (forward or revert),
/// <see cref="OnCardTransformed"/> re-points the mirror at the card's new form. A stored card that leaves
/// the exhaust pile by some other effect (fetched, replayed) hollows its mirror: that mirror falls back to
/// the empty-mirror payoff. Cosmetic clone pets (one per mirror) are kept in step by
/// <see cref="MirrorClone"/>; their deaths feed AfterDeath listeners (记忆, 不碎之镜).
/// </summary>
[RegisterPower]
public sealed class MirrorImagePower : IllusionistPower
{
    /// <summary>Max mirrors on the board; Copy at the cap bursts the newest-loaded mirror first.</summary>
    public const int Cap = 8;

    private const int EmptyDamage = 2;
    private const int EmptyBlock = 2;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private sealed class Data
    {
        /// <summary>Stored cards in store order — last = stored most recently = dies first.</summary>
        public readonly List<CardModel> Loaded = new();

        /// <summary>Mirrors with nothing stored yet. Invariant: Loaded.Count + Empty == Amount.</summary>
        public int Empty;
    }

    // Cards being fired out of a dying mirror right now: their (re-)exhaust must NOT re-store them.
    private readonly HashSet<CardModel> _noRestore = new();

    // Serialize deaths: a released card can deal self-damage and trigger another death mid-release.
    private readonly Queue<Creature?> _pendingDeaths = new();
    private bool _resolvingDeaths;

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
    /// Create ONE mirror for <paramref name="player"/> (the shared primitive behind Copy). At the cap,
    /// first bursts the newest-loaded mirror (per the cap rule), then adds the fresh empty one.
    /// Does NOT summon the cosmetic clone — <see cref="MirrorClone.Copy"/> handles visuals.
    /// </summary>
    internal static async Task CreateOne(Player player, PlayerChoiceContext choiceContext)
    {
        Creature owner = player.Creature;

        MirrorImagePower? power = owner.GetPower<MirrorImagePower>();
        if (power != null && power.TotalMirrors >= Cap)
        {
            await power.KillOne(choiceContext, null);
        }

        await PowerCmd.Apply<MirrorImagePower>(choiceContext, owner, 1, owner, null);
        power = owner.GetPower<MirrorImagePower>();
        if (power != null)
        {
            power.GetInternalData<Data>().Empty++;
        }
    }

    /// <summary>
    /// Store an exhausted card into a free mirror, if any. The card stays in the exhaust pile; the
    /// mirror keeps the reference (newest = top of the death stack).
    /// </summary>
    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        Player? player = base.Owner.Player;
        if (player == null || card.Owner != player)
        {
            return Task.CompletedTask;
        }

        // A card being fired out of a dying mirror goes straight to the exhaust pile — never back in.
        if (_noRestore.Contains(card))
        {
            return Task.CompletedTask;
        }

        Data data = GetInternalData<Data>();
        if (data.Empty <= 0 || data.Loaded.Contains(card))
        {
            return Task.CompletedTask;
        }

        data.Empty--;
        data.Loaded.Add(card);
        Flash();
        Log.Info($"[illusionist] MirrorImage: stored '{card.Title}' ({data.Loaded.Count} loaded, {data.Empty} empty).");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Every instance of unblocked damage / HP loss the owner takes kills one mirror. Multi-hit = one
    /// per hit; fully-blocked or 0-damage instances kill none. The dealer (if an enemy) becomes the
    /// release target.
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner || result.UnblockedDamage <= 0 || TotalMirrors <= 0)
        {
            return;
        }

        Creature? attacker = null;
        ICombatState? combat = base.Owner.CombatState;
        if (dealer != null && dealer != base.Owner && combat != null && combat.HittableEnemies.Contains(dealer))
        {
            attacker = dealer;
        }

        await KillOne(choiceContext, attacker);
    }

    /// <summary>
    /// Kill ONE mirror (newest-loaded first, then empty) and fire its payoff. Deaths are queued and
    /// resolved one at a time — a released card that damages the owner re-enters here safely.
    /// </summary>
    internal async Task KillOne(PlayerChoiceContext choiceContext, Creature? attacker)
    {
        _pendingDeaths.Enqueue(attacker);
        if (_resolvingDeaths)
        {
            return;
        }

        _resolvingDeaths = true;
        try
        {
            while (_pendingDeaths.Count > 0)
            {
                await KillOneCore(choiceContext, _pendingDeaths.Dequeue());
            }
        }
        finally
        {
            _resolvingDeaths = false;
        }
    }

    private async Task KillOneCore(PlayerChoiceContext choiceContext, Creature? attacker)
    {
        Player? player = base.Owner.Player;
        if (player == null)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        CardModel? stored = null;
        if (data.Loaded.Count > 0)
        {
            stored = data.Loaded[^1];
            data.Loaded.RemoveAt(data.Loaded.Count - 1);
        }
        else if (data.Empty > 0)
        {
            data.Empty--;
        }
        else
        {
            return;
        }

        Flash();

        // The cosmetic clone dies first — its AfterDeath feeds 记忆 (draw/energy) and 不碎之镜 (AoE).
        await MirrorClone.ShatterOneClone(player);

        if (base.Amount > 1m)
        {
            await PowerCmd.Decrement(this);
        }
        else
        {
            await PowerCmd.Remove(this);
        }

        if (stored != null)
        {
            await Release(choiceContext, player, stored, attacker);
        }
        else
        {
            await EmptyBurst(choiceContext, player, attacker);
        }
    }

    /// <summary>
    /// Fire a stored card out of a dying mirror: play it at the attacker (else random enemy), then
    /// make sure it ends up in the exhaust pile. If the card left the exhaust pile through some other
    /// effect, the mirror is hollow — fall back to the empty-mirror payoff.
    /// </summary>
    private async Task Release(PlayerChoiceContext choiceContext, Player player, CardModel card, Creature? attacker)
    {
        if (card.Pile == null || card.Pile.Type != PileType.Exhaust)
        {
            await EmptyBurst(choiceContext, player, attacker);
            return;
        }

        _noRestore.Add(card);
        try
        {
            Creature? target = (attacker != null && attacker.IsAlive) ? attacker : null; // null → AutoPlay randomizes
            Log.Info($"[illusionist] MirrorImage: releasing '{card.Title}'.");
            await CardCmd.AutoPlay(choiceContext, card, target);

            // "打出存储的牌并将其放入消耗牌堆" — if the play didn't already exhaust it, exhaust it now.
            if (card.Pile != null && card.Pile.Type != PileType.Exhaust)
            {
                await CardCmd.Exhaust(choiceContext, card);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorImage: release failed: {ex}");
        }
        finally
        {
            _noRestore.Remove(card);
        }
    }

    /// <summary>Empty-mirror death: 2 damage at the breaker (else a random enemy) + 2 Block.</summary>
    private async Task EmptyBurst(PlayerChoiceContext choiceContext, Player player, Creature? attacker)
    {
        try
        {
            Creature? target = (attacker != null && attacker.IsAlive) ? attacker : null;
            if (target == null)
            {
                ICombatState? combat = player.Creature.CombatState;
                List<Creature> enemies = combat?.HittableEnemies.Where(e => e.IsAlive).ToList() ?? new List<Creature>();
                if (enemies.Count > 0)
                {
                    target = enemies[IllusionistRng.RunRng(player).NextInt(enemies.Count)];
                }
            }

            if (target != null)
            {
                await CreatureCmd.Damage(choiceContext, target, EmptyDamage, ValueProp.Unpowered | ValueProp.SkipHurtAnim, player.Creature, null, null);
            }

            await CreatureCmd.GainBlock(player.Creature, EmptyBlock, ValueProp.Unpowered, null);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorImage: empty-mirror burst failed: {ex}");
        }
    }

    /// <summary>
    /// A card was transformed (forward 幻化 or turn-start revert). If a mirror stored the old form,
    /// re-point it at the new form — stored cards participate fully in 幻化回退.
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
