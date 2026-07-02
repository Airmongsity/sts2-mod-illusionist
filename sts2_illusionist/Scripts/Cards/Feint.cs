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

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 虚晃 (FeintIllusionist) — 3 cost Attack, Rare. Upgrade keeps the 3 cost but adds Retain.
/// If the target's intent this turn includes Attack, Stun it (it does nothing this turn). A reactive
/// control piece for the Intent system; once upgraded, Retain lets you bank it until a foe telegraphs
/// a big hit, then cancel the swing entirely.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "FEINT")]
public sealed class FeintIllusionist : IllusionistCard
{

    public FeintIllusionist()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
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
        // Cost stays 3; the upgrade grants Retain so you can hold it for an attack intent.
        AddKeyword(CardKeyword.Retain);
    }
}
