using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Monsters;

/// <summary>
/// 镜像分身 (Mirror Clone) — the cosmetic 1-HP ally that stands beside the player, one per mirror. It is
/// purely decorative now: the count and the effects live in the three per-type powers
/// (<see cref="GuardMirrorPower"/> / <see cref="BladeMirrorPower"/> / <see cref="ThornMirrorPower"/>), so
/// the player can read how many of each kind they have. The clone never acts (a passive NOTHING_MOVE);
/// <see cref="CountAlive"/> reports the total from the powers, and the summon/despawn helpers keep the
/// clone army roughly in step for flavor.
///
/// Summon/despawn are best-effort (try/catch): the worst case is "no clone appears / a clone lingers",
/// never a crash that breaks the run.
/// </summary>
[RegisterMonster]
public sealed class MirrorClone : MonsterModel
{
    public override int MinInitialHp => 1;

    public override int MaxInitialHp => 1;

    // The base MonsterModel.Title looks up a "monsters" loc table we don't ship; point at a key we define.
    public override LocString Title => new LocString("cards", "ILLUSIONIST_CARD_MIRROR_IMAGE.title");

    // Reuse the reskinned player's own (Necrobinder) visuals so the clone looks like a copy of you.
    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/necrobinder");

    // It's flavor, not a real monster — keep it out of the bestiary/compendium.
    public override bool ShouldShowInCompendium => false;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // A do-nothing move that loops back to itself: the clone never takes an action.
        MoveState idle = new MoveState("NOTHING_MOVE", (IReadOnlyList<Creature> _) => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine(new MonsterState[] { idle }, idle);
    }

    /// <summary>
    /// 复制 N (Copy N): for each mirror, roll a random type (守/刃/棘), apply one stack of that type's
    /// power, and summon one cosmetic clone. The shared primitive behind every "Copy N" card and relic.
    /// </summary>
    public static async Task Copy(Player player, int count, PlayerChoiceContext choiceContext)
    {
        if (player == null || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            await MirrorRoster.ApplyRandom(player, choiceContext);
            await SummonClone(player);
        }
    }

    /// <summary>
    /// 相位循环 (Phase Shift): cycle the three COMMON mirror types — all Blade → Thorn, all Thorn → Guard,
    /// all Guard → Blade. Uncommon/Rare mirrors are left untouched. The total mirror count (and the clones)
    /// is unchanged; only the Common kinds rotate.
    /// </summary>
    public static async Task RotateTypes(Player? player, PlayerChoiceContext choiceContext)
    {
        if (player == null)
        {
            return;
        }

        Creature owner = player.Creature;
        int guard = (int)(owner.GetPower<GuardMirrorPower>()?.Amount ?? 0m);
        int blade = (int)(owner.GetPower<BladeMirrorPower>()?.Amount ?? 0m);
        int thorn = (int)(owner.GetPower<ThornMirrorPower>()?.Amount ?? 0m);
        if (guard == 0 && blade == 0 && thorn == 0)
        {
            return;
        }

        // Remove ONLY the three Common type powers; Uncommon/Rare mirrors (and their clones) stay put.
        await RemovePowerIfPresent(owner.GetPower<GuardMirrorPower>());
        await RemovePowerIfPresent(owner.GetPower<BladeMirrorPower>());
        await RemovePowerIfPresent(owner.GetPower<ThornMirrorPower>());

        // Blade → Thorn, Thorn → Guard, Guard → Blade.
        if (thorn > 0)
        {
            await PowerCmd.Apply<GuardMirrorPower>(choiceContext, owner, thorn, owner, null);
        }
        if (guard > 0)
        {
            await PowerCmd.Apply<BladeMirrorPower>(choiceContext, owner, guard, owner, null);
        }
        if (blade > 0)
        {
            await PowerCmd.Apply<ThornMirrorPower>(choiceContext, owner, blade, owner, null);
        }
    }

    /// <summary>Summon one cosmetic clone beside the player (best-effort).</summary>
    private static async Task SummonClone(Player player)
    {
        try
        {
            Creature clone = await PlayerCmd.AddPet<MirrorClone>(player);
            SyncFacingToPlayer(clone, player);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone summon failed: {ex}");
        }
    }

    /// <summary>
    /// Make a freshly-summoned clone face the same way the player currently faces (best-effort). In
    /// "surrounded" encounters SurroundedPower flips the player and its EXISTING pets; a clone summoned
    /// mid-combat starts at the default facing, so we mirror the sign of the player's body scale onto it.
    /// </summary>
    private static void SyncFacingToPlayer(Creature clone, Player player)
    {
        try
        {
            NCombatRoom? room = NCombatRoom.Instance;
            if (room == null)
            {
                return;
            }

            Node2D? playerBody = room.GetCreatureNode(player.Creature)?.Body;
            Node2D? cloneBody = room.GetCreatureNode(clone)?.Body;
            if (playerBody == null || cloneBody == null)
            {
                return;
            }

            if (Mathf.Sign(playerBody.Scale.X) != Mathf.Sign(cloneBody.Scale.X))
            {
                cloneBody.Scale *= new Vector2(-1f, 1f);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone facing sync failed: {ex}");
        }
    }

    /// <summary>
    /// How many mirrors the player has — the authoritative total, read from the three type powers (the
    /// count the payoff cards scale by). Non-destructive.
    /// </summary>
    public static int CountAlive(Player? player)
    {
        if (player == null)
        {
            return 0;
        }

        return player.Creature.Powers.OfType<MirrorTypePower>().Sum(p => (int)p.Amount);
    }

    /// <summary>Kill every cosmetic clone the player owns (visual cleanup; does NOT touch the powers).</summary>
    private static async Task KillAllClones(Player player)
    {
        ICombatState? combat = player.Creature.CombatState;
        if (combat == null)
        {
            return;
        }

        List<Creature> clones = combat.Allies
            .Where(c => c.Monster is MirrorClone && c.PetOwner == player && c.IsAlive)
            .ToList();
        if (clones.Count == 0)
        {
            return;
        }

        await CreatureCmd.Kill(clones, force: true);
    }

    /// <summary>Kill one cosmetic clone (visual only; the caller adjusts the powers).</summary>
    public static async Task ShatterOneClone(Player? player)
    {
        if (player == null)
        {
            return;
        }

        ICombatState? combat = player.Creature.CombatState;
        if (combat == null)
        {
            return;
        }

        Creature? clone = combat.Allies
            .FirstOrDefault(c => c.Monster is MirrorClone && c.PetOwner == player && c.IsAlive);
        if (clone == null)
        {
            return;
        }

        try
        {
            await CreatureCmd.Kill(clone, force: true);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone shatter-one failed: {ex}");
        }
    }

    /// <summary>Remove every mirror the player has (all clones + all three type powers).</summary>
    public static async Task ShatterAll(Player? player)
    {
        if (player == null)
        {
            return;
        }

        try
        {
            await KillAllClones(player);
            await RemoveAllPowers(player);
            Log.Info("[illusionist] MirrorClone: shattered all mirrors.");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone shatter failed: {ex}");
        }
    }

    /// <summary>
    /// Spend every mirror the player has: remove all three type powers AND kill all cosmetic clones,
    /// returning how many mirrors there were — the count the "复制品" payoff cards (引爆/汲取) scale by.
    /// </summary>
    public static async Task<int> ConsumeAll(Player? player)
    {
        if (player == null)
        {
            return 0;
        }

        int count = CountAlive(player);
        try
        {
            await KillAllClones(player);
            await RemoveAllPowers(player);
            Log.Info($"[illusionist] MirrorClone: consumed {count} mirror(s).");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone consume failed: {ex}");
        }

        return count;
    }

    /// <summary>
    /// Destroy ONE mirror: kill a single clone AND drop one stack of one type power, keeping the clones
    /// and the powers in lockstep. Returns 1 if a mirror was destroyed, else 0.
    /// </summary>
    public static async Task<int> ConsumeOne(Player? player)
    {
        if (player == null || CountAlive(player) <= 0)
        {
            return 0;
        }

        try
        {
            await ShatterOneClone(player);
            await DecrementOnePower(player);
            Log.Info("[illusionist] MirrorClone: consumed 1 mirror.");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone consume-one failed: {ex}");
        }

        return 1;
    }

    /// <summary>Remove every mirror type power (all 12 kinds) from the player.</summary>
    private static async Task RemoveAllPowers(Player player)
    {
        foreach (PowerModel power in player.Creature.Powers.OfType<MirrorTypePower>().ToList())
        {
            await PowerCmd.Remove(power);
        }
    }

    /// <summary>Remove a single power if it exists (no-op on null).</summary>
    private static async Task RemovePowerIfPresent(PowerModel? power)
    {
        if (power != null)
        {
            await PowerCmd.Remove(power);
        }
    }

    /// <summary>Drop one stack from the first non-empty mirror type power (Guard → Blade → Thorn).</summary>
    private static async Task DecrementOnePower(Player player)
    {
        PowerModel? power = player.Creature.Powers.OfType<MirrorTypePower>().FirstOrDefault(p => p.Amount > 0m);
        if (power != null)
        {
            if (power.Amount > 1m)
            {
                await PowerCmd.Decrement(power);
            }
            else
            {
                await PowerCmd.Remove(power);
            }
        }
    }
}
