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

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 即兴 (ImproviseIllusionist) — 1 cost Power, Rare (upgraded: Innate). Each turn, the first card you
/// transmute is played for free at a random enemy; Improvise stacks (Counter), so with N copies the
/// first N transmuted cards each turn are each auto-played. High-risk/high-reward: you don't choose the
/// target, but it's free (cheats out expensive transmuted cards) and, as your first play, it's copied
/// by your mirror images.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "IMPROVISE_ILLUSIONIST")]
public sealed class ImproviseIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
    };

    public ImproviseIllusionist()
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
