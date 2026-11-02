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
/// 蜃击 (Mirage Strike) — 1 cost Attack, Common. Deal 6 damage plus 2 per card transformed
/// this turn (upgraded: 9 plus 3). Uses the same counter as Metamorphosis, including reverts.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "MIRAGE_STRIKE")]
public sealed class MirageStrikeIllusionist : IllusionistCard
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromKeyword(IllusionistKeywords.Transmute),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("Bonus", 2m),
    };

    public MirageStrikeIllusionist()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int transformedThisTurn = base.Owner.Creature.GetPower<TransformCountPower>()?.CountThisTurn ?? 0;
        decimal damage = base.DynamicVars.Damage.BaseValue + base.DynamicVars["Bonus"].BaseValue * transformedThisTurn;

        await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
        base.DynamicVars["Bonus"].UpgradeValueBy(1m);
    }
}
