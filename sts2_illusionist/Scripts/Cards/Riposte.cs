using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// RiposteIllusionist - 1 cost Attack, Basic (starter); upgraded: 7 -> 9 damage, 4 -> 6 bonus.
/// Deal 7 damage; if the enemy intends to attack this turn, deal 4 extra.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "RIPOSTE")]
[RegisterCharacterStarterCard(typeof(Characters.Illusionist))]
[RegisterArchaicToothTranscendence(typeof(SabotageIllusionist))]
public sealed class RiposteIllusionist : IllusionistCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move),
        new DynamicVar("Bonus", 4m),
    };

    public RiposteIllusionist()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        Creature target = cardPlay.Target;

        decimal damage = base.DynamicVars.Damage.BaseValue;
        if (IntendsToAttack(target))
        {
            damage += base.DynamicVars["Bonus"].BaseValue;
        }

        await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);      // 7 -> 9
        base.DynamicVars["Bonus"].UpgradeValueBy(2m);    // 4 -> 6
    }

    private static bool IntendsToAttack(Creature target)
    {
        if (target.Monster == null)
        {
            return false;
        }
        return target.Monster.NextMove.Intents.Any(i => i.IntentType == IntentType.Attack);
    }
}
