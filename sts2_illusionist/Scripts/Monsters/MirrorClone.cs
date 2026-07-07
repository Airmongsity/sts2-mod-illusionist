using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
/// purely decorative: the mirror count and ALL behavior (storing exhausted cards, LIFO deaths, releasing
/// stored cards) live in <see cref="MirrorImagePower"/>; this class provides the public Copy/Consume API
/// the cards call, plus the summon/despawn helpers that keep the clone army in step for flavor. Clone
/// deaths DO matter to AfterDeath listeners (记忆 draws, 不碎之镜 AoE), which is why
/// <see cref="MirrorImagePower"/> shatters one clone per mirror death.
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
    /// 复制 N (Copy N): create N empty mirrors (one <see cref="MirrorImagePower"/> stack + one cosmetic
    /// clone each). At the cap (<see cref="MirrorImagePower.Cap"/>) extra copies simply do nothing.
    /// The shared primitive behind every "Copy N" card/relic.
    /// </summary>
    public static async Task Copy(Player player, int count, PlayerChoiceContext choiceContext)
    {
        if (player == null || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (!await MirrorImagePower.CreateOne(player, choiceContext))
            {
                break; // at the cap — no mirror, no clone
            }

            await SummonClone(player);
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
    /// How many mirrors the player has (loaded + empty) — the authoritative total, read from the
    /// <see cref="MirrorImagePower"/> stack count (the count the payoff cards scale by). Non-destructive.
    /// </summary>
    public static int CountAlive(Player? player)
    {
        if (player == null)
        {
            return 0;
        }

        return (int)(player.Creature.GetPower<MirrorImagePower>()?.Amount ?? 0m);
    }

    /// <summary>Kill one cosmetic clone (visual only; <see cref="MirrorImagePower"/> adjusts the data).</summary>
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

    /// <summary>
    /// Destroy every mirror the player has, ONE AT A TIME. Active destruction is "先打出再摧毁": each
    /// loaded mirror fires its stored card before shattering (empty mirrors just shatter). Returns how
    /// many mirrors died — the count the payoff cards (引爆/汲取) scale by.
    /// </summary>
    public static async Task<int> ConsumeAll(Player? player, PlayerChoiceContext choiceContext)
    {
        if (player == null)
        {
            return 0;
        }

        int count = 0;
        try
        {
            MirrorImagePower? power = player.Creature.GetPower<MirrorImagePower>();
            while (power != null && power.TotalMirrors > 0)
            {
                count += await power.ConsumeOne(choiceContext);
                power = player.Creature.GetPower<MirrorImagePower>();
            }

            Log.Info($"[illusionist] MirrorClone: consumed {count} mirror(s).");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone consume failed: {ex}");
        }

        return count;
    }

    /// <summary>
    /// Destroy ONE mirror ("先打出再摧毁": a loaded mirror fires its stored card before shattering;
    /// empty mirrors die first, per the universal death order). Returns 1 if a mirror was destroyed.
    /// </summary>
    public static async Task<int> ConsumeOne(Player? player, PlayerChoiceContext choiceContext)
    {
        if (player == null)
        {
            return 0;
        }

        MirrorImagePower? power = player.Creature.GetPower<MirrorImagePower>();
        if (power == null || power.TotalMirrors <= 0)
        {
            return 0;
        }

        try
        {
            int destroyed = await power.ConsumeOne(choiceContext);
            Log.Info($"[illusionist] MirrorClone: consumed {destroyed} mirror(s).");
            return destroyed;
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone consume-one failed: {ex}");
            return 0;
        }
    }

    /// <summary>
    /// Remove every mirror the player has. Under the store/release system every mirror death fires its
    /// payoff, so this is exactly <see cref="ConsumeAll"/> (kept as a named alias for call-site intent).
    /// </summary>
    public static Task ShatterAll(Player? player, PlayerChoiceContext choiceContext)
    {
        return ConsumeAll(player, choiceContext);
    }
}
