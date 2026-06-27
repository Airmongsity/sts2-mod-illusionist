using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 逆转 (Reversal) — 2 cost Skill, Uncommon (upgraded: Retain).
/// Apply 2 Vulnerable. If the target intends to attack, turn its attack against itself: it is stunned
/// (does not attack) and instead gains Block equal to the damage it would have dealt.
///
/// <para>Implemented with the engine's Stun (a clean, robust mechanic) plus a stun-turn move that
/// makes the enemy gain Block — so the intent icon shows "stunned" rather than a shield, but the
/// effect (no attack; the foe defends for that much instead) matches.</para>
/// </summary>
public sealed class Reversal : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public Reversal()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        await PowerCmd.Apply<VulnerablePower>(choiceContext, target, 2, base.Owner.Creature, this);

        if (target.Monster == null)
        {
            return;
        }

        // Only acts on an attack intent: change it into "gain Block equal to the intended damage".
        List<AttackIntent> attacks = target.Monster.NextMove.Intents.OfType<AttackIntent>().ToList();
        if (attacks.Count == 0)
        {
            return;
        }

        try
        {
            IReadOnlyList<Creature> me = new[] { base.Owner.Creature };
            int block = attacks.Sum(a => a.GetTotalDamage(me, target));
            if (block <= 0)
            {
                return;
            }

            // Cancel the attack (stun) and have the enemy gain that much Block on its turn instead.
            await CreatureCmd.Stun(target, async _ =>
            {
                await CreatureCmd.GainBlock(target, block, ValueProp.Unpowered, null);
            });
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Reversal: failed to convert attack to block: {ex}");
        }
    }
}
