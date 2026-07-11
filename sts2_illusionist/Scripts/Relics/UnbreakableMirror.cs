using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Relics;

/// <summary>
/// 不碎之镜 (Unbreakable Mirror) — Uncommon. Whenever one of your mirrors dies (shattered by damage,
/// burst at the Copy cap, or actively consumed), deal <see cref="DamagePerDeath"/> damage to ALL
/// enemies. Reworked for the store/release mirror system: mirror deaths are now part of the engine,
/// so this turns every death into splash damage. Self-contained via the clone AfterDeath hook (the
/// same pattern 记忆/Memory uses), which fires once per mirror death.
/// </summary>
[RegisterRelic(typeof(IllusionistRelicPool))]
public sealed class UnbreakableMirror : IllusionistRelic
{
    private const int DamagePerDeath = 3;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // Placeholder art until Unbreakable Mirror has its own.
    protected override string IconBaseName => "funerarymask";

    protected override IEnumerable<string> RegisteredKeywordIds => new[] { IllusionistKeywords.MirrorImageId, IllusionistKeywords.ExecuteId };

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature.Monster is not MirrorClone || creature.PetOwner != base.Owner)
        {
            return;
        }

        try
        {
            ICombatState? combat = base.Owner.Creature.CombatState;
            List<Creature> enemies = combat?.HittableEnemies.Where(e => e.IsAlive).ToList() ?? new List<Creature>();
            if (enemies.Count == 0)
            {
                return;
            }

            Flash();
            await CreatureCmd.Damage(choiceContext, enemies, DamagePerDeath, ValueProp.Unpowered | ValueProp.SkipHurtAnim, base.Owner.Creature, null, null);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] UnbreakableMirror failed: {ex}");
        }
    }
}
