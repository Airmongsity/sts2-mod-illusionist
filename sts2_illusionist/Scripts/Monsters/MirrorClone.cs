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
/// 镜像分身 (Mirror Clone) — a purely COSMETIC 1-HP ally summoned by "Copy 1". It stands beside
/// the player looking like a second copy of the Illusionist (it reuses the Necrobinder/player
/// visuals) and never acts on its own (a passive NOTHING_MOVE, just like Osty). All gameplay —
/// replaying the first card each turn and shattering on unblocked damage — lives in
/// <see cref="Illusionist.Scripts.Powers.MirrorImagePower"/>; this entity is only the visual.
///
/// SummonIllusionist/despawn are best-effort (try/catch): if the engine can't spawn or kill the clone, the
/// worst case is "no clone appears / a clone lingers", never a crash that breaks the run.
/// </summary>
[RegisterMonster]
public sealed class MirrorClone : MonsterModel
{
    public override int MinInitialHp => 1;

    public override int MaxInitialHp => 1;

    // The base MonsterModel.Title looks up "MIRROR_CLONE.name" in the "monsters" loc table, which
    // we don't ship — that threw a LocException on every name lookup. Point at a key we do define.
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

    /// <summary>SummonIllusionist one mirror clone beside the player (best-effort).</summary>
    public static async Task SummonIllusionist(Player player)
    {
        try
        {
            Creature clone = await PlayerCmd.AddPet<MirrorClone>(player);
            SyncFacingToPlayer(clone, player);
            Log.Info("[illusionist] MirrorClone: summoned a clone.");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone summon failed: {ex}");
        }
    }

    /// <summary>
    /// 复制 N (Copy N): apply N stacks of <see cref="MirrorImagePower"/> and summon N cosmetic
    /// clones beside the player. The shared primitive used by every "Copy N" card and relic.
    /// </summary>
    public static async Task Copy(Player player, int count, PlayerChoiceContext choiceContext)
    {
        if (player == null || count <= 0)
        {
            return;
        }

        await PowerCmd.Apply<MirrorImagePower>(choiceContext, player.Creature, count, player.Creature, null);
        for (int i = 0; i < count; i++)
        {
            await SummonIllusionist(player);
        }
    }

    /// <summary>
    /// Make a freshly-summoned clone face the same way the player currently faces (best-effort).
    /// In "surrounded" encounters (帝皇蝎/Kaiser Crab) <c>SurroundedPower</c> flips the player and
    /// its EXISTING pets' body scale to face the back-attacking enemy. A clone summoned mid-combat
    /// starts at the default facing and is only re-synced if the facing later changes — so if the
    /// player keeps facing the same direction, the new clone would stay turned the wrong way. We
    /// mirror the sign of the player's body scale onto the new clone to keep them aligned. This is
    /// encounter-agnostic: in a normal fight both already face right, so nothing flips.
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

    /// <summary>How many mirror clones (复制品) the player currently has alive. Non-destructive.</summary>
    public static int CountAlive(Player? player)
    {
        if (player == null)
        {
            return 0;
        }

        ICombatState? combat = player.Creature.CombatState;
        if (combat == null)
        {
            return 0;
        }

        return combat.Allies.Count(c => c.Monster is MirrorClone && c.PetOwner == player && c.IsAlive);
    }

    /// <summary>Remove every mirror clone the player owns (called when the mirrors shatter).</summary>
    public static async Task ShatterAll(Player? player)
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

        List<Creature> clones = combat.Allies
            .Where(c => c.Monster is MirrorClone && c.PetOwner == player && c.IsAlive)
            .ToList();
        if (clones.Count == 0)
        {
            return;
        }

        try
        {
            await CreatureCmd.Kill(clones, force: true);
            Log.Info($"[illusionist] MirrorClone: shattered {clones.Count} clone(s).");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone shatter failed: {ex}");
        }
    }

    /// <summary>
    /// Spend every mirror the player has: kill all clone entities AND remove the Mirror Image
    /// power (so the replay buff ends too), returning how many clones were destroyed — the
    /// "复制品" count that payoff cards (引爆/汲取) scale their effect by.
    /// </summary>
    public static async Task<int> ConsumeAll(Player? player)
    {
        if (player == null)
        {
            return 0;
        }

        ICombatState? combat = player.Creature.CombatState;
        if (combat == null)
        {
            return 0;
        }

        List<Creature> clones = combat.Allies
            .Where(c => c.Monster is MirrorClone && c.PetOwner == player && c.IsAlive)
            .ToList();
        int count = clones.Count;

        try
        {
            if (clones.Count > 0)
            {
                await CreatureCmd.Kill(clones, force: true);
            }

            MirrorImagePower? power = player.Creature.GetPower<MirrorImagePower>();
            if (power != null)
            {
                await PowerCmd.Remove(power);
            }

            Log.Info($"[illusionist] MirrorClone: consumed {count} clone(s).");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] MirrorClone consume failed: {ex}");
        }

        return count;
    }
}
