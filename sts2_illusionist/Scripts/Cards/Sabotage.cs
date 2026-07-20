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
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻术师之怒 (SabotageIllusionist) - 1 cost Attack, Ancient. Orobas's reward: deal 18 damage,
/// 复制 1 (create one mirror image), and 幻化 (transmute) a card in your hand into a 暗淡油灯
/// (Dim Lamp: 0 cost, gain 1 energy, draw 2) for this turn. The self-copy recursion that let the old
/// version snowball is gone - the transmute now blanks a hand card into a productive lamp (feeding the
/// lamp economy) instead of spawning another Sabotage. Upgraded: 18 -> 26 damage.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SABOTAGE")]
public sealed class SabotageIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Copy, IllusionistKeywords.MirrorImage, IllusionistKeywords.Transmute };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<DimLampIllusionist>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(18m, ValueProp.Move),
    };

    public SabotageIllusionist()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await MirrorClone.Copy(base.Owner, 1, choiceContext);

        // 幻化 a hand card into a 暗淡油灯 (Dim Lamp) for this turn (reverts next turn if unplayed).
        await Transmutation.TransmuteOneFromHand(this, choiceContext,
            original => original.CardScope!.CreateCard<DimLampIllusionist>(original.Owner));
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(8m); // 18 -> 26
    }
}
