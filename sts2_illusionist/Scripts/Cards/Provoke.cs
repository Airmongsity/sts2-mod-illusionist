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
using MegaCrit.Sts2.Core.Models.Powers;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 挑衅 (Provoke) — 0 cost Skill, Uncommon (upgraded: 10 -> 18 Strength, 1 -> 2 Dexterity).
/// This turn, give the target enemy temporary Strength (inflating its attack telegraph), and gain
/// permanent Dexterity yourself. The intent payoff terminal: goad the enemy into a bigger swing,
/// then reflect it with 抗衡 (Counter) or block it with 预见 (Foresight). It scales with the Strength
/// YOU grant — not the enemy's base numbers — so it beats high-HP single targets that a 1:1 reflect
/// never could, and the Dexterity makes it worth playing even outside the combo (block scaling).
///
/// The Strength is temporary (<see cref="ProvokePower"/>, Flex-style): it lasts through the enemy's
/// own attack and is removed at the end of the enemy's turn. So Provoke makes the REAL incoming hit
/// bigger — the player must neutralize it (Foresight block, a kill, or an intent change), which is
/// the skill check that keeps "give the enemy +Strength" honest. The Dexterity is permanent (self).
/// </summary>
public sealed class Provoke : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(10m),
        new PowerVar<DexterityPower>(1m),
    };

    public Provoke()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        // Temporary Strength to the enemy (inflates its telegraph this turn).
        await PowerCmd.Apply<ProvokePower>(choiceContext, cardPlay.Target, base.DynamicVars.Strength.BaseValue, base.Owner.Creature, this);
        // Permanent Dexterity to yourself (rest of combat).
        await PowerCmd.Apply<DexterityPower>(choiceContext, base.Owner.Creature, base.DynamicVars.Dexterity.BaseValue, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Strength.UpgradeValueBy(8m);
        base.DynamicVars.Dexterity.UpgradeValueBy(1m);
    }
}
