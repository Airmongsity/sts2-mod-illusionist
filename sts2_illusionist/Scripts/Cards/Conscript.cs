using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 征召 (Conscript) — 1 cost Skill, Rare (upgraded: 0 cost), Exhaust.
/// Copy 4 (create four mirror images, each with its own cosmetic clone), then lose 1 Strength
/// and 1 Dexterity for the rest of combat.
/// </summary>
public sealed class Conscript : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Copy,
        IllusionHoverTips.CopyToken,
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
    };

    public Conscript()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Copy 4: four mirrors (power amount 4) plus one cosmetic clone per mirror.
        await PowerCmd.Apply<MirrorImagePower>(choiceContext, base.Owner.Creature, 4, base.Owner.Creature, this);
        await MirrorClone.Summon(base.Owner);
        await MirrorClone.Summon(base.Owner);
        await MirrorClone.Summon(base.Owner);
        await MirrorClone.Summon(base.Owner);
        // Cost of the copies: lose 1 Strength and 1 Dexterity (both allow negative stacks).
        await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner.Creature, -1, base.Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(choiceContext, base.Owner.Creature, -1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
