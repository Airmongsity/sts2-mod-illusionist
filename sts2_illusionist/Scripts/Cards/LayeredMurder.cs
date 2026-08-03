using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// Layered Murder - deal 12 damage, then deal 7 additional damage for each layer reverted
/// from a selected card in hand. Upgraded cost is 1.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "LAYERED_MURDER")]
public sealed class LayeredMurderIllusionist : IllusionistCard
{
    private const decimal DamagePerLayer = 7m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { IllusionistKeywords.Transmute };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DamageVar(12m, ValueProp.Move) };

    public LayeredMurderIllusionist()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromHand(
            choiceContext,
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
            _ => true,
            this)).FirstOrDefault();
        if (selected == null)
        {
            return;
        }

        TransmutePower? transmute = base.Owner.Creature.GetPower<TransmutePower>();
        if (transmute == null)
        {
            return;
        }

        IReadOnlyList<CardModel> revertedLayers =
            await transmute.RevertFully(choiceContext, selected);
        for (int i = 0; i < revertedLayers.Count && !CombatManager.Instance.IsOverOrEnding; i++)
        {
            await DamageCmd.Attack(DamagePerLayer).FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
