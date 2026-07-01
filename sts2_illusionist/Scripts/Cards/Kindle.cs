using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 点灯 (KindleIllusionist) — 1 cost Skill, Common (upgraded: 10 Block instead of 7).
/// Gain {Block:diff()} [gold]Block[/gold], then [gold]Transmute[/gold] every Status card in your hand
/// into a 暗淡油灯 (Dim Lamp). Each Lamp reverts at the start of your next turn (transmute stack),
/// so play them this turn.
/// </summary>
public sealed class KindleIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
        HoverTipFactory.FromCard<DimLampIllusionist>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(7m, ValueProp.Move),
    };

    public KindleIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        List<CardModel> lamps = PileType.Hand.GetPile(base.Owner).Cards
            .Where(c => c.Type == CardType.Status).ToList();
        if (lamps.Count == 0)
        {
            return;
        }

        await Transmutation.TransmuteCards(lamps, this, choiceContext,
            original => original.CardScope!.CreateCard<DimLampIllusionist>(original.Owner));
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
    }
}
