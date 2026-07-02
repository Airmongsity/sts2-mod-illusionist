using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 嬗变 (Metamorphosis) — 2 cost Attack, Uncommon. The 幻化 system's output port: deal 12 damage, plus 5
/// extra for EACH card you 变化 (transformed) this turn (upgraded: 16 + 7). "This turn's transforms"
/// counts BOTH halves of the system — the turn-start reverts of last turn's transmuted cards AND the
/// forward transmutes you make this turn — because both run through
/// <see cref="Illusionist.Scripts.Transmutation.NotifyTransformed"/>, which feeds
/// <see cref="TransformCountPower"/>.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "METAMORPHOSIS")]
public sealed class MetamorphosisIllusionist : IllusionistCard
{

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        Illusionist.Scripts.IllusionHoverTips.TransmuteIllusionist,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move),
        new DynamicVar("Bonus", 7m),
    };

    public MetamorphosisIllusionist()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        int transformedThisTurn = base.Owner.Creature.GetPower<TransformCountPower>()?.CountThisTurn ?? 0;
        decimal damage = base.DynamicVars.Damage.BaseValue + base.DynamicVars["Bonus"].BaseValue * transformedThisTurn;

        await DamageCmd.Attack(damage).FromCard(this).Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);   // 12 -> 16
        base.DynamicVars["Bonus"].UpgradeValueBy(4m); // 5 -> 7
    }
}
