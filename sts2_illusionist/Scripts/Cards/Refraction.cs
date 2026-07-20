using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// RefractionIllusionist - 1 cost Power, Uncommon (upgraded: 4 -> 6 damage).
/// Whenever you transform a card, deal damage to a random enemy.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "REFRACTION")]
public sealed class RefractionIllusionist : IllusionistCard
{

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Damage", 4m),
    };

    public RefractionIllusionist()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int damage = (int)base.DynamicVars["Damage"].BaseValue;
        await PowerCmd.Apply<RefractionPower>(choiceContext, base.Owner.Creature, damage, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["Damage"].UpgradeValueBy(2m); // 4 -> 6
    }
}
