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
/// 镜像 (Mirror Image) — 1 cost Skill, Uncommon (upgraded: 0 cost). Copy 1 (create one empty
/// mirror — see <see cref="MirrorImagePower"/>). Back to Copy 1 in the v5 slowdown pass: mirror
/// slots are meant to be built deliberately, not flooded.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "MIRROR_IMAGE")]
public sealed class MirrorImageIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Copy, IllusionistKeywords.MirrorImage, IllusionistKeywords.Execute };

    public MirrorImageIllusionist()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await MirrorClone.Copy(base.Owner, 1, choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
