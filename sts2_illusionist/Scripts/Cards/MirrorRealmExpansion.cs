using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 镜界扩张 (MirrorRealmExpansionIllusionist) - 1 cost Skill, Rare, Exhaust (upgraded: 0 cost).
/// Raise the [gold]镜像[/gold] cap by 2 (a stacking 镜界 buff that survives mirror death), draw 2, then
/// leave a copy of THIS card in the discard pile that costs 1 more. CreateClone copies local cost
/// modifiers, so each generation's +1 (EnergyCost.AddThisCombat) accumulates: 1 -> 2 -> 3 -> 4 cost,
/// gating the snowball - the ceiling rises, but each extra +2 cap costs more energy to chase.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "MIRROR_REALM_EXPANSION")]
public sealed class MirrorRealmExpansionIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust, IllusionistKeywords.MirrorImage };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<MirrorCapUpPower>(),
    };

    public MirrorRealmExpansionIllusionist()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 镜界: +2 to the mirror cap for the rest of combat (stacks across plays and across the
        // self-replicating chain - 8 -> 10 -> 12 -> ...).
        await PowerCmd.Apply<MirrorCapUpPower>(choiceContext, base.Owner.Creature, 2, base.Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, 2m, base.Owner);

        // A copy of this card that costs 1 more. AddThisCombat is a RELATIVE local cost modifier, and
        // CardEnergyCost.Clone (called by CreateClone) copies _localModifiers - so the +1 stacks across
        // generations: play the 1c original -> leaves a 2c copy -> play that -> leaves a 3c copy -> ...
        // Echo's AddGeneratedCardToCombat + PreviewCardPileAdd pattern for the discard-pile fly animation.
        CardModel copy = CreateClone();
        copy.EnergyCost.AddThisCombat(1);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, base.Owner), 2.2f);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
