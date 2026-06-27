using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 因祸得福 (Silver Lining) — 0 cost Skill, Common.
/// If you have a negative effect (a debuff, or any power with a negative amount such as the
/// Illusionist's −Strength/−Dexterity), gain 1 energy. Upgraded: gain the energy unconditionally.
/// </summary>
public sealed class SilverLiningIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public SilverLiningIllusionist()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature me = base.Owner.Creature;
        bool hasNegative = IsUpgraded
            || me.Powers.Any(p => p.Type == PowerType.Debuff || p.Amount < 0m);
        if (hasNegative)
        {
            await PlayerCmd.GainEnergy(1, base.Owner);
        }
    }
}
