using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 聚焦 (Focus) - 1 cost Attack, Rare (upgraded: 6 -> 9 damage). Deal 6 damage, then every loaded mirror
/// fires its stored card at THIS enemy - a directed volley that overrides the round-robin targeting. The
/// mirror pillar's directed burst: turn the random auto-fire battery into a focused strike on one target.
/// Mirrors are not consumed (a non-spent stored card returns to its slot). No mirrors = just the damage.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "FOCUS")]
public sealed class FocusIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.MirrorImage, IllusionistKeywords.Execute };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    public FocusIllusionist()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // Directed volley: every loaded mirror fires its stored card at THIS enemy (overrides round-robin).
        MirrorImagePower? mirror = base.Owner.Creature.GetPower<MirrorImagePower>();
        if (mirror != null)
        {
            await mirror.FireAllAtTarget(choiceContext, target);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m); // 6 -> 9
    }
}
