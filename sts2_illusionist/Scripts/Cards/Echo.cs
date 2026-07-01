using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 回响 (EchoIllusionist) — 2 cost Skill, Rare (upgraded: 1 cost), Exhaust.
/// Gain 3 Block, then put a copy of this card into the discard pile whose Replay count is one
/// higher than this card's (so each generation plays an extra time and snowballs).
/// </summary>
public sealed class EchoIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(3m, ValueProp.Move),
    };

    public EchoIllusionist()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // Put a copy of this card into the discard pile with one more Replay than this one.
        // CreateClone() copies this card's full state (cost/upgrade/replay). The animation comes
        // from CardCmd.PreviewCardPileAdd on the add result (same as Ironclad's Anger) — without it
        // the copy just appears in the discard pile with no visible "card flies out" preview.
        CardModel copy = CreateClone();
        copy.BaseReplayCount = BaseReplayCount + 1;
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, base.Owner), 2.2f);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
    }
}
