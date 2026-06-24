using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Relics;

/// <summary>
/// 先古迷幻灯 (Lamp Unbound) — the Ancient (Orobas) upgrade of <see cref="HallucinatoryLamp"/>.
/// At the start of each combat, Copy 3. At the start of your turn, if you have no mirror images,
/// Copy 1 (re-seeds the engine after a shatter without restoring the whole stack).
///
/// Obtained ONLY by transforming the Lamp via Touch of Orobas (see the Harmony patch that injects
/// HallucinatoryLamp -> AncientLamp into TouchOfOrobas.RefinementUpgrades). It is registered into
/// the relic pool (Entry.cs) — Starter rarity keeps it out of reward rolls, but a pool membership
/// is required or the non-virtual RelicModel.Pool lookup throws when its description renders.
/// </summary>
public sealed class AncientLamp : RelicModel
{
    // Starter rarity, like the base ancient upgrade PhylacteryUnbound: it replaces the starter relic.
    public override RelicRarity Rarity => RelicRarity.Starter;

    // Placeholder art until Lamp Unbound has its own.
    protected override string IconBaseName => "boundphylactery";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Copy,
        IllusionHoverTips.CopyToken,
    };

    public override async Task BeforeCombatStart()
    {
        try
        {
            await MirrorClone.Copy(base.Owner, 3, new ThrowingPlayerChoiceContext());
            Log.Info("[illusionist] AncientLamp: combat start — Copy 3.");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] AncientLamp combat-start failed: {ex}");
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player)
        {
            return;
        }

        try
        {
            if (MirrorClone.CountAlive(player) == 0)
            {
                await MirrorClone.Copy(player, 1, choiceContext);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] AncientLamp reseed failed: {ex}");
        }
    }
}
