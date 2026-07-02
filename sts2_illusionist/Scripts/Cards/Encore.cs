using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 返场 (EncoreIllusionist) — 2 cost Power, Uncommon (upgraded: gains Innate).
/// At the end of your turn, return all Retain cards from your discard pile to your hand. The
/// recursion engine for the intent/control suite: it lets you replay CounterIllusionist, ForesightIllusionist, ReversalIllusionist
/// and Catalyze every turn instead of once. Upgraded Innate so the engine is online from turn 1.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "ENCORE_ILLUSIONIST")]
public sealed class EncoreIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromKeyword(CardKeyword.Retain),
    };

    public EncoreIllusionist()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EncorePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
