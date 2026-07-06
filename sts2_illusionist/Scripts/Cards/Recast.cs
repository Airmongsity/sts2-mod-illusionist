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
/// 重铸 (Recast) — 1 cost Attack, Uncommon (upgraded: 13 -> 16 damage). Deal 13 damage, destroy all your
/// mirror images, then [gold]Copy[/gold] the same number back — shattering the whole board (triggering
/// every shatter payoff: 愈镜 heal, 裂镜 burst, 记忆 draw) and rerolling it into fresh random types at the
/// same count.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "RECAST")]
public sealed class RecastIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Copy, IllusionistKeywords.MirrorImage };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(13m, ValueProp.Move),
    };

    public RecastIllusionist()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // Destroy every mirror (fires shatter payoffs) and remake the same number with fresh types.
        int destroyed = await MirrorClone.ConsumeAll(base.Owner);
        if (destroyed > 0)
        {
            await MirrorClone.Copy(base.Owner, destroyed, choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
