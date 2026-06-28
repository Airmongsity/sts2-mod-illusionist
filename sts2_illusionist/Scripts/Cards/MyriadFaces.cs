using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using Illusionist.Scripts;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 千面 (Myriad Faces) — 1 cost Skill, Uncommon (upgraded: 0 cost). Choose a card in your hand; 幻化
/// every OTHER card in your hand into a copy of it until end of turn, then add that many Dazed to your
/// draw pile. The controllable burst enabler: pick 幻爆 (Phantom Blast) and your whole hand becomes
/// free copies of it to dump in one turn — and with FluxweaveIllusionist each reshape draws, refilling
/// for even more. The Dazed are the cost (no longer Exhaust): the more you reshape, the more your draw
/// pile clogs next turns.
/// </summary>
public sealed class MyriadFacesIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
    };

    public MyriadFacesIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;

        // Pick the template to copy (any card; it stays unchanged — only the others are reshaped).
        List<CardModel> picked = (await CardSelectCmd.FromHand(
            choiceContext, owner,
            new CardSelectorPrefs(new LocString("cards", "MYRIAD_FACES_ILLUSIONIST.selectionScreenPrompt"), 1),
            null,
            this)).ToList();
        if (picked.Count == 0)
        {
            return;
        }

        CardModel template = picked[0];

        // Every OTHER card in hand becomes a copy of the template (carrying its upgrades/enchants).
        List<CardModel> others = PileType.Hand.GetPile(owner).Cards.Where(c => c != template).ToList();
        int transformed = await Transmutation.TransmuteCards(others, this, choiceContext, _ => template.CreateClone());

        // Drawback: add one Dazed to the draw pile per card reshaped.
        if (transformed > 0)
        {
            await CardPileCmd.AddToCombatAndPreview<Dazed>(owner.Creature, PileType.Draw, transformed, owner, CardPilePosition.Random);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
