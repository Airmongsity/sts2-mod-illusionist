using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// Provoke - gain temporary Dexterity; an attacking target gains temporary Strength and refunds
/// Energy. The upgrade increases the refund from 1 to 2.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "PROVOKE")]
public sealed class ProvokeIllusionist : IllusionistCard
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(3m),
        new PowerVar<DexterityPower>(2m),
        new EnergyVar(1),
    };

    public ProvokeIllusionist()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await PowerCmd.Apply<ProvokeDexterityPower>(
            choiceContext,
            base.Owner.Creature,
            base.DynamicVars.Dexterity.BaseValue,
            base.Owner.Creature,
            this);

        if (!IntendsToAttack(cardPlay.Target))
        {
            return;
        }

        await PowerCmd.Apply<ProvokePower>(
            choiceContext,
            cardPlay.Target,
            base.DynamicVars.Strength.BaseValue,
            base.Owner.Creature,
            this);
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
    }

    private static bool IntendsToAttack(Creature target)
    {
        return target.Monster?.NextMove.Intents.Any(intent =>
            intent.IntentType == IntentType.Attack) == true;
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
