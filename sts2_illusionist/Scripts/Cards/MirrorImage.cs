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

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 镜像 (Mirror Image) — 1 cost Skill, Uncommon (upgraded: 0 cost).
/// Copy 1 (create a mirror): while a mirror is present, the first card you play each turn is
/// replayed once. Taking unblocked damage shatters all mirrors. No drawback.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "MIRROR_IMAGE")]
public sealed class MirrorImageIllusionist : IllusionistCard
{

    // 复制 (Copy) is our own mechanic, not an engine keyword, so it gets no automatic tooltip —
    // attach them explicitly (like Forge/铸造 has): the Copy action plus the mirror-image entity
    // (复制品) it creates, so the card explains both what "Copy 1" does and how the copy behaves.
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[] { IllusionHoverTips.Copy, IllusionHoverTips.CopyToken };

    public MirrorImageIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MirrorImagePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
        // SummonIllusionist the cosmetic clone that stands beside you (visual only; mechanics live in the power).
        await MirrorClone.SummonIllusionist(base.Owner);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
