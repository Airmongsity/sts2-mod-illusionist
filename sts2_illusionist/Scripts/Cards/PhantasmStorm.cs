using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Illusionist.Scripts.Afflictions;
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
/// 幻象风暴 test version (keeps the PHANTASM_STORM stable identity) - 2 cost Ancient Power. Copy 1;
/// the first exhaust each turn makes every mirror execute. Upgraded: 1 cost.
///
/// The previous 焚城 and Copy 8 + PhantasmStormPower implementations remain behind
/// <see cref="ActiveVersion"/> so either experiment can be restored without reconstructing it.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "PHANTASM_STORM")]
[RegisterDustyTomeCard(typeof(Characters.Illusionist))]
public sealed class PhantasmStormIllusionist : IllusionistCard
{
    private enum TestVersion
    {
        LegacyPhantasmStorm,
        BurnTheCity,
        FantasyStorm,
    }

    private static readonly TestVersion ActiveVersion = TestVersion.FantasyStorm;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        ActiveVersion == TestVersion.BurnTheCity
            ? Array.Empty<CardKeyword>()
            : new[]
            {
                IllusionistKeywords.Copy,
                IllusionistKeywords.MirrorImage,
                IllusionistKeywords.Execute,
            };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        ActiveVersion == TestVersion.BurnTheCity
            ? HoverTipFactory.FromAffliction<Scorching>()
            : Array.Empty<IHoverTip>();

    public PhantasmStormIllusionist()
        : base(ActiveVersion == TestVersion.BurnTheCity ? 1 : 2, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (ActiveVersion == TestVersion.FantasyStorm)
        {
            await MirrorClone.Copy(base.Owner, 1, choiceContext);
            await PowerCmd.Apply<FantasyStormPower>(
                choiceContext,
                base.Owner.Creature,
                1,
                base.Owner.Creature,
                this);
            return;
        }

        if (ActiveVersion == TestVersion.BurnTheCity)
        {
            await PowerCmd.Apply<BurnTheCityPower>(
                choiceContext,
                base.Owner.Creature,
                1,
                base.Owner.Creature,
                this);
            return;
        }

        // Preserved legacy 幻象风暴 implementation.
        await MirrorClone.Copy(base.Owner, 8, choiceContext);
        await PowerCmd.Apply<PhantasmStormPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        if (ActiveVersion == TestVersion.FantasyStorm)
        {
            base.EnergyCost.UpgradeBy(-1);
            return;
        }

        if (ActiveVersion == TestVersion.BurnTheCity)
        {
            AddKeyword(CardKeyword.Innate);
            return;
        }

        // Preserved legacy 幻象风暴 upgrade.
        base.EnergyCost.UpgradeBy(-1);
    }
}
