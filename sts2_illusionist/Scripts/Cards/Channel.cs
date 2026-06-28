using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 导能 (Channel) — X cost Attack, Common. Spend all your energy (X), deal X+6 damage and refund X-1
/// energy (upgraded: deal X+9 damage and refund the full X). The net cost is just 1 energy (0 when
/// upgraded), and the Mirror synergy is the payoff: as your first card of the turn it's replayed by
/// every mirror image, so each clone repeats the full X-damage hit — a few images effectively multiply
/// it while you keep almost all your energy.
///
/// <para>X-cost pattern (mirrors the base game's Dirge): <see cref="HasEnergyCostX"/> = true, base
/// energy cost 0, and the spent energy is read at play time via
/// <see cref="CardModel.ResolveEnergyXValue"/>.</para>
/// </summary>
public sealed class ChannelIllusionist : CardModel
{
    /// <summary>Flat damage added on top of X (both normal and upgraded).</summary>
    private const int BaseBonus = 6;

    /// <summary>Additional flat damage added on top of X when upgraded.</summary>
    private const int UpgradeBonus = 3;

    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override bool HasEnergyCostX => true;

    public ChannelIllusionist()
        : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        int x = ResolveEnergyXValue();

        int damage = x + BaseBonus + (base.IsUpgraded ? UpgradeBonus : 0);
        if (damage > 0)
        {
            await DamageCmd.Attack(damage).FromCard(this).Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        // Refund energy: X-1 normally, the full X when upgraded.
        int energy = base.IsUpgraded ? x : x - 1;
        if (energy > 0)
        {
            await PlayerCmd.GainEnergy(energy, base.Owner);
        }
    }
}
