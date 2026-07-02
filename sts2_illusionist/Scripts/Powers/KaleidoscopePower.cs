using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 万花筒 (KaleidoscopeIllusionist) power. Counts the cards the owner plays; on every 5th card it triggers:
/// Copy 1 (create a mirror image) and deal 6 damage to ALL enemies. The running count is cumulative
/// across the whole combat (not reset per turn) and is shown as a counter on the power icon.
///
/// <para>Instanced (like <c>TheBombPower</c>): each KaleidoscopeIllusionist you play — including replays — adds
/// its OWN counter that ticks independently, so the effect stacks. Mirrors <c>PanachePower</c>'s
/// "every N cards" counter pattern (CounterIllusionist stack type + a visible CardsPlayed DynamicVar).</para>
/// </summary>
[RegisterPower]
public sealed class KaleidoscopePower : IllusionistPower
{
    private const int Threshold = 5;

    private class Data
    {
        // Don't count the KaleidoscopeIllusionist card that applied this instance toward its own counter.
        public bool alreadyApplied;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // The icon shows how many cards have been played toward the next trigger (0..Threshold-1).
    public override int DisplayAmount => base.DynamicVars["CardsPlayed"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("CardsPlayed", 0m),
        new DamageVar(6m, ValueProp.Unpowered),
    };

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (base.Owner.Player == null || cardPlay.Card.Owner != base.Owner.Player)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.alreadyApplied)
        {
            base.DynamicVars["CardsPlayed"].BaseValue++;
            InvokeDisplayAmountChanged();

            if (base.DynamicVars["CardsPlayed"].IntValue >= Threshold)
            {
                base.DynamicVars["CardsPlayed"].BaseValue = 0m;
                InvokeDisplayAmountChanged();
                Flash();

                // Copy 1 (mirror power + cosmetic clone), then deal 6 to ALL enemies.
                await MirrorClone.Copy(base.Owner.Player, 1, choiceContext);

                ICombatState? combat = base.CombatState;
                if (combat != null && combat.HittableEnemies.Count > 0)
                {
                    await CreatureCmd.Damage(choiceContext, combat.HittableEnemies, base.DynamicVars.Damage, base.Owner);
                }
            }
        }

        data.alreadyApplied = true;
    }
}
