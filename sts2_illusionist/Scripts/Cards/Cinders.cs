using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 灰烬 (Cinders) — 2 cost Attack, Common (upgraded: 10 -> 14 damage). Deal 10 damage to ALL enemies,
/// Exhaust. While it sits in your exhaust pile it keeps smoldering: at the start of each of your turns it
/// replays itself (dealing its damage again to all enemies) and re-exhausts — a persistent AoE engine from
/// the ash pile. Copies the native Bombardment pattern (<see cref="AfterAutoPrePlayPhaseEnteredEarly"/> +
/// <see cref="CardKeyword.Exhaust"/>).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "CINDERS")]
public sealed class CindersIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
    };

    public CindersIllusionist()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat == null || combat.HittableEnemies.Count == 0)
        {
            return;
        }

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .TargetingAllOpponents(combat)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    /// <summary>
    /// While this card is in the exhaust pile, replay it at the start of your turn (native Bombardment
    /// pattern). Early phase avoids double-firing with other pre-play exhaust effects; the Exhaust keyword
    /// then sends it straight back to the exhaust pile so it keeps recurring.
    /// </summary>
    public override async Task AfterAutoPrePlayPhaseEnteredEarly(PlayerChoiceContext choiceContext, Player player)
    {
        CardPile? pile = base.Pile;
        if (pile != null && pile.Type == PileType.Exhaust && player == base.Owner)
        {
            await CardCmd.AutoPlay(choiceContext, this, null);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
