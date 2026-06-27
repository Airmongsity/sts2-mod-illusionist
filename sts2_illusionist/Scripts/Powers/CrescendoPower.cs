using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// 渐强 (CrescendoIllusionist) power. At the start of each of your turns, gain Strength equal to this power's
/// stacks (1 per CrescendoIllusionist played). Stacks add, so multiple Crescendos ramp faster.
/// </summary>
public sealed class CrescendoPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature)
        {
            return;
        }

        await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner, base.Amount, base.Owner, null);
    }
}
