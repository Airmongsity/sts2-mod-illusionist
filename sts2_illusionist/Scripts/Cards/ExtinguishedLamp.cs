using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 熄灭油灯 (Extinguished Lamp) — a 0 cost, Retain Status token: the "off" form of 暗淡油灯 (Dim Lamp).
/// 千面 (MyriadFaces) dumps copies into your draw pile as its cost. 点灯 (Kindle) transmutes it into
/// a Dim Lamp. When played directly it does nothing — you want to transmute it, not play it bare.
/// Not upgradeable (like Wither).
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "EXTINGUISHED_LAMP")]
public sealed class ExtinguishedLampIllusionist : IllusionistCard
{
    public override int MaxUpgradeLevel => 0;


    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Retain,
        CardKeyword.Exhaust,
    };

    public ExtinguishedLampIllusionist()
        : base(0, CardType.Status, CardRarity.Token, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
}
