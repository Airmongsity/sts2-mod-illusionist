using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 干扰 (DisruptIllusionist) — 1 cost Skill, Basic (starter). Targets an enemy.
/// Gain 6 Block and apply 1 Weak (虚弱: the enemy deals 25% less attack damage). If you have no
/// mirror images, also Copy 1 (so it doubles as your opener for the Mirror system). Upgraded: 8 Block / 2 Weak.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "DISRUPT_ILLUSIONIST")]
[RegisterCharacterStarterCard(typeof(Characters.Illusionist))]
public sealed class DisruptIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override bool GainsBlock => true;

    // Block tip comes from GainsBlock; Weak + Copy/the mirror token need their tips added explicitly.
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<WeakPower>(),
        IllusionHoverTips.Copy,
        IllusionHoverTips.CopyToken,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move),
        new PowerVar<WeakPower>(1m),
    };

    public DisruptIllusionist()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, base.DynamicVars.Weak.BaseValue, base.Owner.Creature, this);

        // Kickstart the Mirror system: if you have no mirror images yet, Copy 1.
        if (Illusionist.Scripts.Monsters.MirrorClone.CountAlive(base.Owner) == 0)
        {
            await Illusionist.Scripts.Monsters.MirrorClone.Copy(base.Owner, 1, choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
        base.DynamicVars.Weak.UpgradeValueBy(1m);
    }
}
