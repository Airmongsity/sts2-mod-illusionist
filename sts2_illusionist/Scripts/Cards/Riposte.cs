using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 反击 (RiposteIllusionist) — 1 cost Attack, Basic (starter).
/// Deal 6 damage; if the enemy intends to attack this turn, deal 4 extra damage. Then 幻化
/// (transmute) this card into its upgraded version — each play upgrades it IN PLACE, so replays/
/// re-draws hit harder each time. It reverts one tier at the START of your next turn.
///
/// <para>Tiers stack via a self-buff (the 凋萎 / Wither pattern), applied directly to this instance
/// rather than swapping in a clone — so a replay's later hits use the just-upgraded stats, and the
/// real campfire upgrade is untouched (fake-upgrade and true-upgrade coexist). The revert runs in
/// <see cref="AfterPlayerTurnStart"/>; every combat card receives that hook in any pile.</para>
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "RIPOSTE")]
[RegisterCharacterStarterCard(typeof(Characters.Illusionist))]
[RegisterArchaicToothTranscendence(typeof(SabotageIllusionist))]
public sealed class RiposteIllusionist : IllusionistCard
{
    /// <summary>Per play-morph tier: extra base damage and extra "intends to attack" bonus.</summary>
    private const decimal MorphDamage = 2m;
    private const decimal MorphBonus = 2m;

    private int _morphLevel;


    // Show the play-morph tier in the title (on top of any real "+" campfire upgrade).
    public override string Title => _morphLevel > 0 ? $"{base.Title}+{_morphLevel}" : base.Title;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        Illusionist.Scripts.IllusionHoverTips.TransmuteIllusionist,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("Bonus", 4m),
    };

    public RiposteIllusionist()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        Creature target = cardPlay.Target;

        decimal damage = base.DynamicVars.Damage.BaseValue;
        if (IntendsToAttack(target))
        {
            damage += base.DynamicVars["Bonus"].BaseValue;
        }

        await DamageCmd.Attack(damage).FromCard(this).Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 幻化: upgrade IN PLACE after each hit, so a replay's next hit (and any later re-draw this
        // turn) is stronger. Capped; reverts one tier at the start of your next turn.
        await ApplyMorph(choiceContext);
    }

    /// <summary>Revert one play-morph tier at the start of your turn — the 幻化 "reverts next turn".</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (_morphLevel > 0 && base.Owner == player)
        {
            // RevertOneMorph already pops the reverted form up in the temporary card preview —
            // a second Preview here showed the card TWICE on revert.
            await RevertOneMorph(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars["Bonus"].UpgradeValueBy(2m);
    }

    /// <summary>Bump this card up one play-morph tier (buffs its stats; 凋萎 / Wither style).</summary>
    private async Task ApplyMorph(PlayerChoiceContext choiceContext)
    {
        _morphLevel++;
        base.DynamicVars.Damage.UpgradeValueBy(MorphDamage);
        base.DynamicVars["Bonus"].UpgradeValueBy(MorphBonus);
        CardCmd.Preview(this);
        await Transmutation.NotifyTransformed(base.Owner, choiceContext, this);
    }

    /// <summary>Inverse of <see cref="ApplyMorph"/>.</summary>
    private async Task RevertOneMorph(PlayerChoiceContext choiceContext)
    {
        _morphLevel--;
        base.DynamicVars.Damage.UpgradeValueBy(-MorphDamage);
        base.DynamicVars["Bonus"].UpgradeValueBy(-MorphBonus);
        CardCmd.Preview(this);
        await Transmutation.NotifyTransformed(base.Owner, choiceContext, this);
    }

    private static bool IntendsToAttack(Creature target)
    {
        if (target.Monster == null)
        {
            return false;
        }
        return target.Monster.NextMove.Intents.Any(i => i.IntentType == IntentType.Attack);
    }
}
