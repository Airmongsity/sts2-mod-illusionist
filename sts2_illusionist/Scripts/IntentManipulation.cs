using System.Threading.Tasks;
using Illusionist.Scripts.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Illusionist.Scripts;

/// <summary>
/// Shared notification point for player effects that successfully change an enemy's intent structure.
/// Numeric preview changes such as Strength or Weak do not call this entry point.
/// </summary>
public static class IntentManipulation
{
    public static Task NotifyChanged(PlayerChoiceContext choiceContext, Player player)
    {
        MesmerizingArrayPower? power = player.Creature.GetPower<MesmerizingArrayPower>();
        return power?.OnIntentChanged(choiceContext, player) ?? Task.CompletedTask;
    }
}
