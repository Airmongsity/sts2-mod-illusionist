using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Relics;

/// <summary>
/// 不碎之镜 (Unbreakable Mirror) — Uncommon. The first time you would take unblocked damage each
/// combat, your mirror images survive. Implemented by granting one <see cref="PhaseShiftPower"/>
/// charge at combat start (the same charge 虚实转换/Phase Shift uses).
/// </summary>
public sealed class UnbreakableMirror : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // Placeholder art until Unbreakable Mirror has its own.
    protected override string IconBaseName => "funerarymask";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { IllusionHoverTips.CopyToken };

    public override async Task BeforeCombatStart()
    {
        try
        {
            await PowerCmd.Apply<PhaseShiftPower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature, 1, base.Owner.Creature, null);
            Log.Info("[illusionist] UnbreakableMirror: combat start — 1 Phase Shift charge.");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] UnbreakableMirror failed: {ex}");
        }
    }
}
