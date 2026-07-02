using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts.Monsters;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 萃取 (ExtractIllusionist) — 1 cost Power, Uncommon (upgraded: Retain).
/// Destroy all mirror images, then gain +1 energy per turn. Stacks with each play.
/// Friendship power pattern.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "EXTRACT_ILLUSIONIST")]
public sealed class ExtractIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public ExtractIllusionist()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await MirrorClone.ShatterAll(base.Owner);

        await PowerCmd.Apply<ExtractPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
