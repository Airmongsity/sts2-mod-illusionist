using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts.Afflictions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 戏服 (Costume) — 1 cost Skill, Event rarity (upgraded: Innate). Afflict one chosen card in hand with
/// 易容 (<see cref="Disguised"/>), which re-rolls that card into another character's card the first
/// time an enemy gains Block each round. Built on the 星火 / Ember shape, so the two affliction
/// appliers read the same way.
///
/// <para>Event rarity rather than Uncommon: card rewards and the merchant both skip
/// Basic/Ancient/Event, so this stays out of the roll and the reward pool holds at the base
/// characters' 85. The card itself is unchanged and still works wherever it is granted.
/// The borrowed-character line was retired as a system — these cards are kept as working
/// models rather than deleted, but they no longer dilute the rolls.</para>
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "COSTUME")]
public sealed class CostumeIllusionist : IllusionistCard
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromAffliction<Disguised>();

    public CostumeIllusionist()
        : base(1, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Disguised disguised = ModelDb.Affliction<Disguised>();
        CardModel? selected = (await CardSelectCmd.FromHand(
            choiceContext,
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
            disguised.CanAfflict,
            this)).FirstOrDefault();
        if (selected != null)
        {
            await CardCmd.Afflict<Disguised>(selected, 1);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
