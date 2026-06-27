using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 虚晃 (Feint) — 2 cost Attack, Rare (upgraded: 1 cost). Retain.
/// If the target's intent this turn includes Attack, Stun it (it does nothing this turn). A reactive
/// control piece for the Intent system — hold it (Retain) until a foe telegraphs a big hit, then
/// cancel the swing entirely.
/// </summary>
public sealed class Feint : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    // Retain: it only pays off against an attack intent, so let the player bank it until then.
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Retain };

    public Feint()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        Creature target = cardPlay.Target;
        if (target.Monster != null && target.Monster.NextMove.Intents.Any(i => i.IntentType == IntentType.Attack))
        {
            await CreatureCmd.Stun(target);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
