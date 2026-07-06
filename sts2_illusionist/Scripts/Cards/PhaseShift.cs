using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 虚实转换 (Phase Shift) — 1 cost Skill, Common (upgraded: 0 cost). Cycle every mirror's type:
/// all 刃镜 Blade → 棘镜 Thorn, all Thorn → 守镜 Guard, all Guard → Blade. The total mirror count is
/// unchanged; only their kinds rotate.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "PHASE_SHIFT")]
public sealed class PhaseShiftIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.MirrorImage };

    public PhaseShiftIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await MirrorClone.RotateTypes(base.Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
