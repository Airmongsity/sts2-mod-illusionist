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
/// Freeze Frame - 1 cost Uncommon Skill. Freeze one selected card in hand without a separate preview
/// animation. Upgraded cost is 0. Retains the SUPERIMPOSE stable identity.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SUPERIMPOSE")]
public sealed class SuperimposeIllusionist : IllusionistCard
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromAffliction<Frozen>();

    public SuperimposeIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Frozen frozen = ModelDb.Affliction<Frozen>();
        CardModel? selected = (await CardSelectCmd.FromHand(
            choiceContext,
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
            frozen.CanAfflict,
            this)).FirstOrDefault();
        if (selected != null)
        {
            await CardCmd.Afflict<Frozen>(selected, 1);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
