using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 清算 (Reckoning) - 1 cost Exhaust Attack, Uncommon. The intent system's output port: deal 12 damage,
/// plus 8 more for each intent card you've played this combat (upgraded: 16 damage, +10 each). "Intent card" is
/// defined by TEXT - any card whose description mentions an enemy's 意图 (intent), see
/// <see cref="Illusionist.Scripts.IntentCards"/> - so the whole intent system feeds it: reactive cards
/// (反击/致盲/抗衡/预见), manipulators (逆转/虚晃/催化), 挑衅, and 破坏. The card face shows the real hit
/// (Body Slam-style <see cref="ModCardVars.ComputedDamage"/>) so the player doesn't track the running
/// intent-card count themselves.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "RECKONING")]
public sealed class ReckoningIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        // Displayed damage = printed base + Bonus × intent cards played this combat (live, Body Slam-style).
        ModCardVars.ComputedDamage("Damage", 12m, (card, _) => ReckoningDamage(card), ValueProp.Move),
        new DynamicVar("Bonus", 8m),
    };

    public ReckoningIllusionist()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // Same formula the card face shows (ComputedDamage) - no manual counting needed.
        decimal damage = ReckoningDamage(this);

        await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["Damage"].UpgradeValueBy(4m); // 12 -> 16
        base.DynamicVars["Bonus"].UpgradeValueBy(2m); // 8 -> 10
    }

    /// <summary>Total damage: printed Damage base + Bonus × intent cards played this combat (base alone outside combat).</summary>
    private static decimal ReckoningDamage(CardModel? card)
    {
        if (card == null)
        {
            return 12m;
        }

        decimal damageBase = card.DynamicVars["Damage"].BaseValue;
        if (card.Owner?.Creature?.CombatState == null)
        {
            return damageBase;
        }

        decimal bonusRate = card.DynamicVars["Bonus"].BaseValue;
        return damageBase + bonusRate * IntentCards.PlayedThisCombat(card.Owner);
    }
}
