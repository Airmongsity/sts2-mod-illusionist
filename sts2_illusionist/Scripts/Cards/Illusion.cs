using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 错觉 (Illusion) — 1 cost Skill, Uncommon, Exhaust.
/// This turn, treat your Strength and Dexterity as their absolute values — i.e. flip any
/// negative Strength/Dexterity (which the Illusionist accrues from mirror costs) up to positive
/// for the turn. Upgraded: no longer Exhausts.
/// </summary>
public sealed class Illusion : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<NecrobinderCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
    };

    public Illusion()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature me = base.Owner.Creature;

        // For a negative stat of -n, applying +2n of (auto-expiring) temporary stat makes the
        // effective value +n = |-n| for this turn. Non-negative stats are already their absolute
        // value, so we leave them alone.
        int strength = me.GetPowerAmount<StrengthPower>();
        if (strength < 0)
        {
            await PowerCmd.Apply<IllusionStrengthPower>(choiceContext, me, -2 * strength, me, this);
        }

        int dexterity = me.GetPowerAmount<DexterityPower>();
        if (dexterity < 0)
        {
            await PowerCmd.Apply<IllusionDexterityPower>(choiceContext, me, -2 * dexterity, me, this);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
