using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 献身 (SacrificeIllusionist) — 0 cost Skill, Uncommon.
/// Gain 2 energy and draw 2 cards. If you have any mirror images, destroy one of them.
/// Upgraded: draw 4 cards instead of 2.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "SACRIFICE")]
public sealed class SacrificeIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.MirrorImage };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(1),
    };

    public SacrificeIllusionist()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(1, base.Owner);

        int draw = base.DynamicVars.Cards.IntValue;
        await CardPileCmd.Draw(choiceContext, draw, base.Owner);

        // Destroy one mirror if present — its death fires the stored card / empty burst.
        await MirrorClone.ConsumeOne(base.Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
