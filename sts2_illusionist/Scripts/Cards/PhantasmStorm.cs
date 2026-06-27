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

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻象风暴 (Phantasm Storm) — 1 cost Attack, Ancient. The Illusionist's Darv ancient card: the only
/// CardRarity.Ancient card in our pool that is NOT an Archaic Tooth transcendence target, so Darv's
/// DustyTome (which picks a random non-transcendence Ancient card and adds it upgraded to your deck)
/// can find it — fixing the empty-pool NRE that hung the Darv event. Deal 6 damage to ALL enemies
/// and Copy 2. Upgraded: 9 damage. (Registered in the pool but Ancient rarity keeps it out of
/// normal rewards.)
/// </summary>
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
        new DamageVar(6m, ValueProp.Move),
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
            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this)
                .TargetingAllOpponents(combat)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        await MirrorClone.Copy(base.Owner, 2, choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
