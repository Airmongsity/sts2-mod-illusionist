using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
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
/// 突袭 (AmbushIllusionist) — 1 cost Attack, Common. Deal 8 damage, then hand yourself a 先见
/// (Prescience) worth 8 Block — but delivered in its 熄灭油灯 (Extinguished Lamp) form. Creating it
/// counts as a 幻化 right now (feeding 嬗变 / 流变 / 恍惚 …), and the Lamp reverts into the block-granting
/// Prescience at the start of your next turn (a second transform). Upgraded: 10 damage / 10 Block.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "AMBUSH")]
public sealed class AmbushIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Transmute };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<PrescienceIllusionist>(),
        HoverTipFactory.FromCard<ExtinguishedLampIllusionist>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new BlockVar(8m, ValueProp.Move),
    };

    public AmbushIllusionist()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // A lethal hit can put the combat into its ending state before the rest of this card resolves.
        // PowerCmd.Apply deliberately returns null in that state, and generated combat cards have no
        // next turn to revert on, so stop here instead of starting a dead transmutation chain.
        if (CombatManager.Instance.IsEnding)
        {
            return;
        }

        // Build the 先见 (Prescience) worth this card's Block — that's what the player ultimately gets —
        // but deliver it as its 熄灭油灯 (Extinguished Lamp) form: a 幻化 product that reverts to the
        // Prescience at the start of the next turn.
        CardModel prescience = base.CardScope!.CreateCard<PrescienceIllusionist>(base.Owner);
        prescience.DynamicVars.Block.BaseValue = base.DynamicVars.Block.BaseValue;

        CardModel lamp = base.CardScope!.CreateCard<ExtinguishedLampIllusionist>(base.Owner);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(lamp, PileType.Hand, base.Owner);
        CardCmd.PreviewCardPileAdd(result, 1.8f);

        // Register the Lamp -> Prescience revert (next turn start), and count this delivery as a 幻化 now
        // so it feeds the transmute payoffs immediately.
        if (!await Transmutation.RegisterRevert(base.Owner, choiceContext, this, prescience, lamp))
        {
            return;
        }
        await Transmutation.NotifyTransformed(base.Owner, choiceContext, lamp);

        // This Lamp was generated directly into the hand rather than drawn or produced by
        // TransmuteCards. Let 长明灯 process it only AFTER the Prescience -> Lamp history above has
        // been registered, so the resulting chain remains Prescience -> Lamp -> Dim Lamp.
        EverlitLampPower? everlit = base.Owner.Creature.GetPower<EverlitLampPower>();
        if (everlit != null)
        {
            await everlit.TryTransmuteLampInHand(choiceContext, lamp);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars.Block.UpgradeValueBy(2m);
    }
}
