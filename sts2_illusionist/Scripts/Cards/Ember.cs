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
/// 星火 (Ember) — 1 cost Skill, Event rarity (upgraded: Innate). Afflict one chosen card in hand with
/// 灼热 (<see cref="Scorching"/>).
///
/// <para>The affliction is the payload, not the drawback: Scorching Exhausts its card at the start of
/// your turn and jumps to another card in the same pile, so one Ember becomes a self-sustaining
/// one-Exhaust-per-turn engine — exactly what the mirror system is fed on. Playing the afflicted card
/// removes Scorching instead, so the player keeps an out. Selection is filtered through
/// <see cref="Illusionist.Scripts.Afflictions.Scorching.CanAfflict"/>, which already refuses cards that
/// carry a different affliction (a card holds only one).</para>
///
/// <para>Event rarity rather than Uncommon: card rewards and the merchant both skip
/// Basic/Ancient/Event, so this stays out of the roll and the reward pool holds at the base
/// characters' 85. The card itself is unchanged and still works wherever it is granted.</para>
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "EMBER")]
public sealed class EmberIllusionist : IllusionistCard
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromAffliction<Scorching>();

    public EmberIllusionist()
        : base(1, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Scorching scorching = ModelDb.Affliction<Scorching>();
        CardModel? selected = (await CardSelectCmd.FromHand(
            choiceContext,
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
            scorching.CanAfflict,
            this)).FirstOrDefault();
        if (selected != null)
        {
            await CardCmd.Afflict<Scorching>(selected, 1);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
