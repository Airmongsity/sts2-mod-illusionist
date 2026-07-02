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

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 汲取 (SiphonIllusionist) — 2 cost Skill, Rare (upgraded: 1 cost).
/// Destroy all mirror clones; for each one destroyed, gain 1 Strength and 1 Dexterity.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SIPHON")]
public sealed class SiphonIllusionist : IllusionistCard
{

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.CopyToken,
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
    };

    public SiphonIllusionist()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Spend every mirror; gain 1 Strength and 1 Dexterity per clone destroyed.
        int clones = await MirrorClone.ConsumeAll(base.Owner);
        if (clones <= 0)
        {
            return;
        }

        int amount = clones;
        await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner.Creature, amount, base.Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(choiceContext, base.Owner.Creature, amount, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
