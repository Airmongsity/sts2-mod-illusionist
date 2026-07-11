using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 虚张声势 (Bluff) — 1 cost Skill, Common (upgraded: 14 -> 18 Block). Gain 14 Block; the target
/// enemy ADDITIONALLY intends to gain <see cref="EnemyBlock"/> Block — its current move is wrapped
/// (original intents + behavior fully preserved via <see cref="MoveState.PerformMove"/>) with a
/// Defend telegraph and a block gain on its turn. The feed half of the intent flow's "fatten the
/// turtle, then crack it" line — 拆穿 / Call the Bluff is the punish half.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "BLUFF")]
public sealed class BluffIllusionist : IllusionistCard
{
    /// <summary>Block the enemy is fed (fixed; the upgrade only raises YOUR block).</summary>
    private const int EnemyBlock = 6;

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(14m, ValueProp.Move),
    };

    public BluffIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        if (target.Monster == null)
        {
            return;
        }

        try
        {
            // Wrap the enemy's current move: same intents plus a Defend telegraph; on its turn it
            // first gains the fed Block, then performs its ORIGINAL move unchanged (PerformMove).
            MoveState move = target.Monster.NextMove;
            List<AbstractIntent> intents = new List<AbstractIntent> { new DefendIntent() };
            intents.AddRange(move.Intents);

            MoveState wrapped = new MoveState(
                "BLUFF_TURTLE_ILLUSIONIST",
                async (IReadOnlyList<Creature> targets) =>
                {
                    await CreatureCmd.GainBlock(target, EnemyBlock, ValueProp.Unpowered, null);
                    await move.PerformMove(targets);
                },
                intents.ToArray())
            {
                // Preserve the enemy's sequence exactly: after this move it goes wherever the
                // original would have led, so no cross-turn intent is changed.
                FollowUpStateId = move.FollowUpStateId,
            };
            wrapped.FollowUpState = move.FollowUpState;

            target.Monster.SetMoveImmediate(wrapped, forceTransition: true);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Bluff: failed to add block intent: {ex}");
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(4m); // 14 -> 18
    }
}
