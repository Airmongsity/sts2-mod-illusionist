using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Relics;

/// <summary>
/// Swift Boots - Common. The first non-autoplay card you play each combat costs 0 Energy.
/// </summary>
[RegisterRelic(typeof(IllusionistRelicPool))]
public sealed class HeadStart : IllusionistRelic
{
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Common;

    // Placeholder art until Head Start has its own.
    protected override string IconBaseName => "bookmark";

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_usedThisCombat || cardPlay.IsAutoPlay || !cardPlay.IsFirstInSeries || cardPlay.Card.Owner != base.Owner)
        {
            return Task.CompletedTask;
        }

        _usedThisCombat = true;
        Status = RelicStatus.Disabled;
        Flash();
        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        var owner = base.Owner;
        ICombatState? combat = owner?.Creature.CombatState;
        if (owner == null || combat == null || _usedThisCombat || originalCost <= 0m)
        {
            return false;
        }

        // In multiplayer this cost hook fires for every player's cards.
        if (card.Owner != owner)
        {
            return false;
        }

        modifiedCost = 0m;
        return true;
    }
}
