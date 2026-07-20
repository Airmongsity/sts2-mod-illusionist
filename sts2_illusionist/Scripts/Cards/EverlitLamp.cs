using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 长明灯 (Everlit Lamp) - 2 cost Power, Rare (upgraded: gains Innate). Each turn, the first time you
/// 幻化 a card in your hand into an 熄灭油灯 (Extinguished Lamp), it becomes a 暗淡油灯 (Dim Lamp) instead
/// (0 cost: gain 1 energy, draw 2). A Transmute-pillar ceiling raiser, not a damage finisher: it turns
/// the "blank a card" cost (Riposte / Disillusion) into a productive Dim Lamp. Hand-only - the swap
/// lives in TransmuteCards with an original.Pile==Hand gate, so it can't fire on (or deadlock) the
/// transmute-revert path or discard/draw lamp additions. Upgraded Innate so it's online from turn 1.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "EVERLIT_LAMP")]
public sealed class EverlitLampIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Transmute };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<ExtinguishedLampIllusionist>(),
        HoverTipFactory.FromCard<DimLampIllusionist>(),
    };

    public EverlitLampIllusionist()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EverlitLampPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
