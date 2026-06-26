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
/// 防御 (Defend) — the Illusionist's own basic Defend (1 cost Skill, Basic). Gain 5 Block
/// (upgraded: 8). Its own card so it belongs to <see cref="IllusionistCardPool"/> rather than the
/// Necrobinder's (so deck-transform effects target the Illusionist's cards).
/// </summary>
public sealed class IllusionistDefend : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    // The Defend tag is how the game/relics identify a starter Defend (Leafy Poultice, …).
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Defend };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(5m, ValueProp.Move),
    };

    public IllusionistDefend()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
    }
}
