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
/// 幻想风暴 (PhantasmStorm) — 1 cost Attack, Ancient.
/// Deal X damage to ALL enemies per mirror image. Copy 3. If no mirrors, Copy 2 more.
/// Upgraded: 9 → 13 damage per mirror.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "PHANTASM_STORM_ILLUSIONIST")]
[RegisterDustyTomeCard(typeof(Characters.Illusionist))]
public sealed class PhantasmStormIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Copy,
        IllusionHoverTips.CopyToken,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(4m, ValueProp.Move),
    };

    public PhantasmStormIllusionist()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat != null && combat.HittableEnemies.Count > 0)
        {
            int mirrors = MirrorClone.CountAlive(base.Owner);
            decimal damage = base.DynamicVars.Damage.BaseValue * (mirrors > 0 ? mirrors : 1);
            await DamageCmd.Attack(damage).FromCard(this)
                .TargetingAllOpponents(combat)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        int copies = 3;
        if (MirrorClone.CountAlive(base.Owner) == 0)
            copies += 2;

        await MirrorClone.Copy(base.Owner, copies, choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
