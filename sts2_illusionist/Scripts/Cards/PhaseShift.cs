using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 虚实转换 (Phase Shift) - 1 cost Power, Common (upgraded: 0 cost).
/// Apply 虚实转换: the next time you take unblocked damage, your mirror images do not die - one stack
/// absorbs one unblocked-damage instance (see <see cref="MirrorImagePower.AfterDamageReceived"/>, which
/// consumes a stack before killing a mirror). The mirror system's safety net: mirrors normally die on
/// every unblocked hit (a pure loss), so this trades a card for a reprieve that keeps a loaded volley
/// alive through a swing you can't fully block. Stacks counter - N stacks absorb N hits.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "PHASE_SHIFT")]
public sealed class PhaseShiftIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.MirrorImage, IllusionistKeywords.Execute };

    public PhaseShiftIllusionist()
        : base(1, CardType.Power, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PhaseShiftPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1); // 1 -> 0
    }
}
