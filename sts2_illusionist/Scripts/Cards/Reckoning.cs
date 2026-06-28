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

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 清算 (Reckoning) — 2 cost Attack, Rare. The intent system's output port: deal 16 damage, plus 8
/// more for each intent card you've played this combat (upgraded: 22 damage, +11 each). "Intent card" is
/// defined by TEXT — any card whose description mentions an enemy's 意图 (intent), see
/// <see cref="Illusionist.Scripts.IntentCards"/> — so the whole intent system feeds it: reactive cards
/// (反击/致盲/抗衡/预见), manipulators (逆转/虚晃/催化), 挑衅, and 破坏. This is what mirror/transmute
/// decks can't match: its scaling comes purely from playing intent cards.
/// </summary>
public sealed class ReckoningIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(16m, ValueProp.Move),
        new DynamicVar("Bonus", 8m),
    };

    public ReckoningIllusionist()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // This card isn't in CardPlaysFinished yet, so it counts the OTHER intent cards played first.
        int intentCards = IntentCards.PlayedThisCombat(base.Owner);
        decimal damage = base.DynamicVars.Damage.BaseValue + base.DynamicVars["Bonus"].BaseValue * intentCards;

        await DamageCmd.Attack(damage).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(6m);   // 16 -> 22
        base.DynamicVars["Bonus"].UpgradeValueBy(3m); // 8 -> 11
    }
}
