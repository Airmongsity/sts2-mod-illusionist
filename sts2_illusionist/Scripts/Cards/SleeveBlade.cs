using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 埋伏 (Underhand Strike) — 1-cost Common Attack. Deal 10 damage; if the target has any Block, deal
/// 6 more. Exhaust. Upgraded: each number +2, so 12 / +8.
/// Its efficient, single-use damage still gives the mirror system deliberate ammunition, while the
/// conditional half is the intent flow's Common offensive payoff for 虚张声势 / Bluff's fed Block —
/// the only other Common outlets are 一线生机 (absorb it) and 致盲 (delete it), neither of which
/// kills with it. Deliberately a flat threshold, not a per-Block scale: 拆穿 / Call the Bluff owns
/// the scaling harvest, and a gate keeps a freely-scaling resource from producing a runaway hit.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SLEEVE_BLADE")]
public sealed class SleeveBladeIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Exhaust,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
        new ExtraDamageVar(6m),
    };

    public SleeveBladeIllusionist()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // Threshold, not a scale: any Block at all pays the full bonus. Read before the hit lands,
        // so the Block the target is actually holding when you strike is what counts.
        decimal damage = base.DynamicVars.Damage.BaseValue;
        if (cardPlay.Target.Block > 0)
        {
            damage += base.DynamicVars.ExtraDamage.BaseValue;
        }

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars.ExtraDamage.UpgradeValueBy(2m);
    }
}
