using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 围攻 (SiegeIllusionist) — 1 cost Attack, Rare.
/// For each mirror clone (复制品) you have, deal 8 damage to ALL enemies once.
/// Upgraded: 9 damage.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "SIEGE_ILLUSIONIST")]
public sealed class SiegeIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { IllusionHoverTips.CopyToken };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
    };

    public SiegeIllusionist()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int clones = MirrorClone.CountAlive(base.Owner);
        if (clones <= 0)
        {
            return;
        }

        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat == null || combat.HittableEnemies.Count == 0)
        {
            return;
        }

        // One 3-damage hit to all enemies per mirror clone (clones are NOT consumed).
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(combat)
            .WithHitCount(clones)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
