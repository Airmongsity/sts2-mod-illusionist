using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 镜涌 (CascadeIllusionist) — 3 cost Power, Uncommon (upgraded: Innate). At the start of each turn,
/// Copy 1 — a steady, self-sustaining mirror engine. Stacks: play it twice to Copy 2 each turn.
/// Costed heavily (1 -> 3 in the v5 slowdown pass) because a free-running mirror faucet made the
/// board ramp too fast. (Reworked from 渐强 / Crescendo.)
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "CASCADE")]
public sealed class CascadeIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Copy, IllusionistKeywords.MirrorImage, IllusionistKeywords.Execute };

    public CascadeIllusionist()
        : base(3, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CascadePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
