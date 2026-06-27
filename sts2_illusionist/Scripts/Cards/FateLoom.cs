using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 命运织机 (Fate Loom) — 1 cost Skill, Common.
/// Gain 8 Block, then put a chosen card from your discard pile on top of your draw pile
/// (top-deck control). Upgraded: gain 10 Block.
/// </summary>
public sealed class FateLoomIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(8m, ValueProp.Move),
    };

    public FateLoomIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // Pick 1 card from the discard pile and place it on top of the draw pile.
        CardModel? selected = (await CardSelectCmd.FromCombatPile(
            context: choiceContext,
            pile: PileType.Discard.GetPile(base.Owner),
            player: base.Owner,
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selected != null)
        {
            await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
    }
}
