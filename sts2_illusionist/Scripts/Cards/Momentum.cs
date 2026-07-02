using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 势能 (MomentumIllusionist) — 1 cost Power, Uncommon (upgraded: Innate).
/// Gain Momentum: a permanent power that draws 1 card and gains 1 energy every
/// 10th transform. Instanced — playing it twice gives two independent effects.
/// (Pattern follows base-game Orbit.)
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "MOMENTUM")]
public sealed class MomentumIllusionist : IllusionistCard
{

    public MomentumIllusionist()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MomentumPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
