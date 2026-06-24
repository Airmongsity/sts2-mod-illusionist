using System.Collections.Generic;
using System.Linq;
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
using MegaCrit.Sts2.Core.Models.Powers;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 揭露 (Unveil) — 1 cost Skill, Common, Exhaust (upgraded: 0 cost).
/// Apply 2 Vulnerable to ALL enemies. (Renamed from "Expose" — that name collides with a
/// base-game card's model ID and crashes the game on startup.)
/// </summary>
public sealed class Unveil : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    // Mirrors base-game Bash: a card that applies a power adds that power's hover-tip explicitly.
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<VulnerablePower>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<VulnerablePower>(2m),
    };

    public Unveil()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat == null)
        {
            return;
        }

        foreach (Creature enemy in combat.Enemies.Where(e => e.IsAlive).ToList())
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, base.DynamicVars.Vulnerable.BaseValue, base.Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
