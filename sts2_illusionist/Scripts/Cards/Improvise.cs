using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 即兴 (Improvise) — 1 cost Power, Rare (upgraded: Innate). The first time you transmute a card each
/// turn, play that transmuted card for free at a random enemy. High-risk/high-reward: you don't choose
/// the target, but it's free (cheats out expensive transmuted cards) and, as your first play, it's
/// replayed by your mirror images.
/// </summary>
public sealed class Improvise : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Transmute,
    };

    public Improvise()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ImprovisePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
