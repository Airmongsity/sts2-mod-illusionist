using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻形波 (ShiftingWaveIllusionist) — 1 cost Attack, Common.
/// Deal 6 damage and gain 6 Block. Then 幻化 every ShiftingWave in all piles
/// — each copy gets one morph tier (+3/+3, Riposte/Wither pattern) and shows +N.
/// Reverts one tier at turn start. Both morph and revert count as 变化 (transform)
/// for Fluxweave/Momentum via Transmutation.NotifyTransformed.
/// </summary>
public sealed class ShiftingWaveIllusionist : CardModel
{
    private const decimal MorphDamage = 3m;
    private const decimal MorphBlock = 3m;

    private int _morphLevel;

    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    public override string Title => _morphLevel > 0 ? $"{base.Title}+{_morphLevel}" : base.Title;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
        new BlockVar(6m, ValueProp.Move),
    };

    public ShiftingWaveIllusionist()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        foreach (CardModel card in AllCopies().ToList())
        {
            if (card is ShiftingWaveIllusionist sw)
            {
                await sw.ApplyMorph(choiceContext);
            }
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (_morphLevel > 0 && player == base.Owner)
        {
            await RevertOneMorph(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(MorphDamage);
        base.DynamicVars.Block.UpgradeValueBy(MorphBlock);
    }

    private async Task ApplyMorph(PlayerChoiceContext choiceContext)
    {
        _morphLevel++;
        base.DynamicVars.Damage.UpgradeValueBy(MorphDamage);
        base.DynamicVars.Block.UpgradeValueBy(MorphBlock);
        CardCmd.Preview(this);
        await Transmutation.NotifyTransformed(base.Owner, choiceContext, this);
    }

    private async Task RevertOneMorph(PlayerChoiceContext choiceContext)
    {
        _morphLevel--;
        base.DynamicVars.Damage.UpgradeValueBy(-MorphDamage);
        base.DynamicVars.Block.UpgradeValueBy(-MorphBlock);
        CardCmd.Preview(this);
        await Transmutation.NotifyTransformed(base.Owner, choiceContext, this);
    }

    private IEnumerable<CardModel> AllCopies()
    {
        PileType[] piles = { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust };
        foreach (PileType pt in piles)
        {
            CardPile pile = pt.GetPile(base.Owner);
            foreach (CardModel card in pile.Cards)
            {
                if (card is ShiftingWaveIllusionist)
                    yield return card;
            }
        }
    }
}
