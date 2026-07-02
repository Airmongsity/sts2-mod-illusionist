using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 破坏 (SabotageIllusionist) — 1 cost Attack, Ancient. Orobas's reward, now an intent-system bridge:
/// deal 9 damage to ALL enemies and gain 15 Block; each enemy that intends to attack takes extra
/// damage equal to its attack intent; then 幻化 (transmute) a card in your hand into a copy of this
/// card (chain another swing this turn). Upgraded: 17 damage / 23 Block. Its description mentions
/// 意图, so it also fuels intent-flow outputs like 清算 (Reckoning).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "SABOTAGE_ILLUSIONIST")]
public sealed class SabotageIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(9m, ValueProp.Move),
        new BlockVar(15m, ValueProp.Move),
    };

    public SabotageIllusionist()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combat = base.CombatState;
        if (combat == null)
        {
            return;
        }

        decimal baseDamage = base.DynamicVars.Damage.BaseValue;
        IReadOnlyList<Creature> me = new[] { base.Owner.Creature };

        // Snapshot each enemy and its per-enemy damage (base + its attack-intent total) BEFORE dealing,
        // so a lethal hit on one enemy doesn't disturb the others' intent reads.
        List<(Creature enemy, decimal damage)> hits = new();
        foreach (Creature enemy in combat.HittableEnemies)
        {
            decimal bonus = 0m;
            if (enemy.Monster != null)
            {
                foreach (AbstractIntent intent in enemy.Monster.NextMove.Intents)
                {
                    if (intent is AttackIntent attack)
                    {
                        bonus += attack.GetTotalDamage(me, enemy);
                    }
                }
            }
            hits.Add((enemy, baseDamage + bonus));
        }

        foreach ((Creature enemy, decimal damage) in hits)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }
            await DamageCmd.Attack(damage).FromCard(this).Targeting(enemy)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // 幻化 a hand card into a copy of THIS card (preserving its upgrade state), this turn.
        await Transmutation.TransmuteToCopyOf(this, choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(8m); // 18 -> 26
        base.DynamicVars.Block.UpgradeValueBy(8m);  // 15 -> 23
    }
}
