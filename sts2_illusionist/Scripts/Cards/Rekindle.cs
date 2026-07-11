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
/// 重燃 (RekindleIllusionist) — 1 cost Skill, Common (upgraded: 0 cost).
/// At the start of your next turn, Copy 1. The cheap, Common, reliable 0->1 restart: even if every
/// mirror shatters this turn, the delayed copy lands next turn no matter what.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "REKINDLE")]
public sealed class RekindleIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Copy, IllusionistKeywords.MirrorImage, IllusionistKeywords.Execute };

    public RekindleIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Delayed Copy 1: fires at the start of your next turn, guaranteeing a mirror even if all
        // of this turn's mirrors shatter (see RekindlePower).
        await PowerCmd.Apply<RekindlePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1); // 1 -> 0
    }
}
