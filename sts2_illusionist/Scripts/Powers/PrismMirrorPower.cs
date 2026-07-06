using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 棱镜 (Prism Mirror) — Rare mirror type. When you play a single-target Attack, it also strikes every
/// OTHER enemy for that attack's (base) damage — turning single-target decks into board-wide pressure.
/// Only offered by Copy when there are 2+ enemies (see <see cref="MirrorRoster"/>), since it does nothing
/// in a duel. The spread is a flat, power-sourced echo of the card's damage number (it doesn't re-apply
/// the attack's Strength/Vulnerable scaling); it does not scale with stacks — it's a rule change, not a
/// number.
/// </summary>
[RegisterPower]
public sealed class PrismMirrorPower : MirrorTypePower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (base.Amount <= 0m || cardPlay.Card.Owner.Creature != base.Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        ICombatState? combat = base.CombatState;
        if (combat == null)
        {
            return;
        }

        // The card's damage number (throws via the typed accessor if the attack has no Damage var, so
        // read it defensively).
        if (!cardPlay.Card.DynamicVars.TryGetValue("Damage", out DynamicVar? dmgVar) || dmgVar == null)
        {
            return;
        }
        int dmg = (int)dmgVar.BaseValue;
        if (dmg <= 0)
        {
            return;
        }

        Creature? primary = cardPlay.Target;
        var others = combat.HittableEnemies.Where(e => e.IsAlive && e != primary).ToList();
        if (others.Count == 0)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in others)
        {
            await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered | ValueProp.SkipHurtAnim, base.Owner, null, null);
        }
    }
}
