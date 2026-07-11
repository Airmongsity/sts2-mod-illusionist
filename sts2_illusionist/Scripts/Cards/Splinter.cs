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
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 碎影 (Splinter) — 1 cost Attack, Uncommon (upgraded: 6 -> 7 damage per hit). Deal 6 damage twice; if you
/// have a mirror image, destroy one — a cheap way to trigger a shatter payoff (愈镜 heal, 裂镜 burst, 记忆
/// draw) on demand.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SPLINTER")]
public sealed class SplinterIllusionist : IllusionistCard
{
    private const int HitCount = 2;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.MirrorImage, IllusionistKeywords.Execute };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    public SplinterIllusionist()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitCount(HitCount)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // Shatter one of your own mirrors (fires its stored card / empty burst), if you have any.
        if (MirrorClone.CountAlive(base.Owner) > 0)
        {
            await MirrorClone.ConsumeOne(base.Owner, choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
