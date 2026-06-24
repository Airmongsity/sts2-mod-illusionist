using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Relics;

/// <summary>
/// 棱镜碎片 (Prism Shard) — Common. Whenever a mirror image is created beside you, gain 1 Block.
/// Hooks <c>AfterCreatureAddedToCombat</c> and filters to our own <see cref="MirrorClone"/> pets.
/// </summary>
public sealed class PrismShard : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    // Placeholder art (reuses a base Necrobinder relic sprite) until the Shard has its own.
    protected override string IconBaseName => "ivorytile";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { IllusionHoverTips.CopyToken };

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        try
        {
            if (creature.Monster is MirrorClone && creature.PetOwner == base.Owner)
            {
                await CreatureCmd.GainBlock(base.Owner.Creature, 1, ValueProp.Unpowered, null);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] PrismShard failed: {ex}");
        }
    }
}
