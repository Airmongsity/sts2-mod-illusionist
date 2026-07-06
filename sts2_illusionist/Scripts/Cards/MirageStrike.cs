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
/// 蜃击 (Mirage Strike) — 1 cost Attack, Common (upgraded: each number +3, so 12 / +12). Deal 9 damage; if
/// this exact card is a 幻化品 (a live transmuted form, per <see cref="TransmutePower"/>'s revert chains),
/// deal 9 more. Rewards playing the transmuted copies the 幻化 system hands you.
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
        new DamageVar(9m, ValueProp.Move),
        new ExtraDamageVar(9m),
    };

    public MirageStrikeIllusionist()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        decimal damage = base.DynamicVars.Damage.BaseValue;
        if (IsTransmuteProduct())
        {
            damage += base.DynamicVars.ExtraDamage.BaseValue;
        }

        await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    /// <summary>
    /// "幻化品": this exact instance is the live form of a transmute chain (some card was 幻化'd into it),
    /// tracked by <see cref="TransmutePower.GetRevertTarget"/> — non-null means it has a form to revert to.
    /// </summary>
    private bool IsTransmuteProduct()
    {
        return base.Owner.Creature.GetPower<TransmutePower>()?.GetRevertTarget(this) != null;
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
        base.DynamicVars.ExtraDamage.UpgradeValueBy(3m);
    }
}
