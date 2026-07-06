using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 揭露 (UnveilIllusionist) — 1 cost Attack, Common (upgraded: 12 -> 16 damage). Deal 12 damage, then
/// force every transmuted card to revert one layer of 幻化 immediately (the same one-layer unwind that
/// normally waits for your next turn start). Each revert is a 变化, so it pours the transmute payoffs
/// (折光 bolts, 流变 draw, 嬗变/恍惚 counters …) through this one card. (Renamed from "Expose" — that name
/// collided with a base-game card's model ID.)
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "UNVEIL")]
public sealed class UnveilIllusionist : IllusionistCard
{

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromKeyword(IllusionistKeywords.Transmute),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move),
    };

    public UnveilIllusionist()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // Unwind every transmuted card one layer, right now.
        TransmutePower? transmute = base.Owner.Creature.GetPower<TransmutePower>();
        if (transmute != null)
        {
            await transmute.RevertOneLayer(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m); // 12 -> 16
    }
}
