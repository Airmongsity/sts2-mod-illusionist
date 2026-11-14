using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 拆穿 (Call the Bluff) - 1 cost Attack, Uncommon. Deal 9 damage (upgraded: 13); first
/// REMOVE all of the target's Block and add that much damage to the hit - the whole strike lands
/// on the freshly-bared enemy, so the swing is worth base damage + 2×Block versus attacking into the shell.
/// The punish half of 虚张声势 / Bluff's "fatten the turtle, then crack it" line - it scales off
/// whatever Block the enemy holds, fed or natural. The card face shows the base damage + the target's current
/// Block (Body Slam-style <see cref="ModCardVars.ComputedDamage"/>) so the real hit is visible.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "CALL_THE_BLUFF")]
public sealed class CallTheBluffIllusionist : IllusionistCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        // Displayed damage = printed base + the target's current Block (live, target-aware like Body Slam).
        ModCardVars.ComputedDamage(
            "Damage",
            9m,
            CallTheBluffDamage,
            ValueProp.Move),
    };

    public CallTheBluffIllusionist()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        // Shatter the shell first: strip ALL Block, then land base damage + the stripped amount
        // on the bared target (nothing left to absorb the hit).
        decimal damage = CallTheBluffDamage(this, target);
        int block = target.Block;
        if (block > 0)
        {
            await CreatureCmd.LoseBlock(choiceContext, target, block, base.Owner.Creature);
        }

        await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["Damage"].UpgradeValueBy(4m); // 9 -> 13
    }

    private static decimal CallTheBluffDamage(CardModel? card, Creature? target)
    {
        decimal baseDamage = card?.DynamicVars["Damage"].BaseValue ?? 9m;
        return baseDamage + (target?.Block ?? 0);
    }
}
