using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
/// 明镜 (Pristine Mirror) — Rare. At the end of your turn, if you took no unblocked damage that
/// turn, Copy 1. Rewards the careful blocking the mirror engine already demands. A per-turn flag
/// (reset at turn start, set when unblocked damage lands on the owner) drives the check.
/// </summary>
[RegisterRelic(typeof(IllusionistRelicPool))]
public sealed class PristineMirror : IllusionistRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    // Placeholder art until Pristine Mirror has its own.
    protected override string IconBaseName => "undyingsigil";

    private bool _tookUnblockedDamageThisTurn;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Copy,
        IllusionHoverTips.CopyToken,
    };

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner == player)
        {
            _tookUnblockedDamageThisTurn = false;
        }
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == base.Owner.Creature && result.UnblockedDamage > 0)
        {
            _tookUnblockedDamageThisTurn = true;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || _tookUnblockedDamageThisTurn)
        {
            return;
        }

        try
        {
            await MirrorClone.Copy(base.Owner, 1, choiceContext);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] PristineMirror failed: {ex}");
        }
    }
}
