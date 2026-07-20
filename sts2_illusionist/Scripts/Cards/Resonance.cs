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
/// 共鸣 (Resonance) - 2 cost Power, Rare (upgraded: 1 cost). Once per turn, the first card you play
/// that shares a name with a card stored in a mirror costs 1 less. A mirror-pillar ceiling raiser that
/// rewards the hand↔mirror overlap: you want cards in your mirror that you ALSO hold copies of, so
/// replaying them is discounted. Doesn't lower storage risk (stored cards still auto-fire and can still
/// be lost) - it only adds a synergy for keeping your hand and your mirror aligned.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "RESONANCE")]
public sealed class ResonanceIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.MirrorImage };

    public ResonanceIllusionist()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ResonancePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1); // 2 -> 1
    }
}
