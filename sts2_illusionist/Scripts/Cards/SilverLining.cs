using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 一线生机 (Silver Lining) — 0 cost Skill, Common, targets an enemy (upgraded: gains Retain).
/// Gain 1 energy. If the targeted enemy has a negative effect (a debuff, or any power with a negative
/// amount), gain 1 more energy — rewarding the intent/debuff suite (Weak/Vulnerable/Frail you apply).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "SILVER_LINING_ILLUSIONIST")]
public sealed class SilverLiningIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public SilverLiningIllusionist()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Always gain 1 energy; gain 1 more if the targeted enemy carries a negative effect.
        await PlayerCmd.GainEnergy(1, base.Owner);

        Creature? target = cardPlay.Target;
        if (target != null && target.Powers.Any(p => p.Type == PowerType.Debuff || p.Amount < 0m))
        {
            await PlayerCmd.GainEnergy(1, base.Owner);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
