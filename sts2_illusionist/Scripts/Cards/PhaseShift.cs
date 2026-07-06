using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 虚实转换 (Phase Shift) — PARKED (awaiting a redesign). Its old effect cycled the mirror TYPES
/// (刃→棘→守), which no longer exist under the store/release mirror system (next-mirror.md), so the
/// card currently has no effect. Same retirement pattern as 逆转/Reversal: reclassified as an
/// [gold]Event[/gold] card AND registered to the non-reward <see cref="IllusionistTokenPool"/> so it
/// is unobtainable (the reward/merchant/combat-gen paths never roll Event, and the pool move covers
/// the Uniform selection path). Kept as a live model (file + loc retained) for the future rework.
/// </summary>
[RegisterCard(typeof(IllusionistTokenPool), StableEntryStem = "PHASE_SHIFT")]
public sealed class PhaseShiftIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.MirrorImage };

    public PhaseShiftIllusionist()
        : base(1, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // No effect while parked — the type-cycling this card performed was removed with the typed
        // mirrors. Unobtainable (Event rarity + token pool), so this never runs from normal play.
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
