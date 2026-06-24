using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 镜像 (Mirror Image) — 1 cost Skill, Uncommon, Exhaust (upgraded: no longer Exhausts).
/// Copy 1 (create a mirror): while a mirror is present, the first card you play each turn is
/// replayed once. Taking unblocked damage shatters all mirrors. No drawback.
/// </summary>
public sealed class MirrorImage : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    // 复制 (Copy) is our own mechanic, not an engine keyword, so it gets no automatic tooltip —
    // attach them explicitly (like Forge/铸造 has): the Copy action plus the mirror-image entity
    // (复制品) it creates, so the card explains both what "Copy 1" does and how the copy behaves.
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { IllusionHoverTips.Copy, IllusionHoverTips.CopyToken };

    public MirrorImage()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MirrorImagePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
        // Summon the cosmetic clone that stands beside you (visual only; mechanics live in the power).
        await MirrorClone.Summon(base.Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
