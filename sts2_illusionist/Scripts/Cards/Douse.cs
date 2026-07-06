using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
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
/// 浇熄 (Douse) — 1 cost Attack, Uncommon (upgraded: 10 -> 14 damage). Deal 10 damage, then 幻化 a random set
/// of cards from your discard pile — as many as you hold in hand — into 熄灭油灯 (Extinguished Lamps) until
/// the start of your next turn. Snuffing those cards pours the transmute system's payoffs (流变 draw,
/// 即兴/傀影 auto-plays, 势能) through one card, then they revert next turn.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "DOUSE")]
public sealed class DouseIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Transmute };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<ExtinguishedLampIllusionist>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
    };

    public DouseIllusionist()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 幻化 a random subset of the discard pile — as many cards as you're holding — into Extinguished
        // Lamps (reverts at your next turn start).
        List<CardModel> pool = PileType.Discard.GetPile(base.Owner).Cards.ToList();
        int take = System.Math.Min(PileType.Hand.GetPile(base.Owner).Cards.Count, pool.Count);
        if (take > 0)
        {
            // Partial Fisher-Yates over the mod's deterministic per-run RNG stream.
            MegaCrit.Sts2.Core.Random.Rng rng = IllusionistRng.RunRng(base.Owner);
            for (int i = 0; i < take; i++)
            {
                int j = i + rng.NextInt(pool.Count - i);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            await Transmutation.TransmuteCards(pool.GetRange(0, take), this, choiceContext,
                original => original.CardScope!.CreateCard<ExtinguishedLampIllusionist>(original.Owner));
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
