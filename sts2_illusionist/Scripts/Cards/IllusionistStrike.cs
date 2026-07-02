using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 打击 (Strike) — the Illusionist's own basic Strike (1 cost Attack, Basic). Deal 6 damage
/// (upgraded: 9). Its own card so it belongs to <see cref="IllusionistCardPool"/> rather than the
/// Necrobinder's — so deck-transform effects (event "transform a card", New Leaf, etc.) target the
/// Illusionist's cards.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), FullPublicEntry = "ILLUSIONIST_STRIKE_ILLUSIONIST")]
[RegisterCharacterStarterCard(typeof(Characters.Illusionist), 4)]
public sealed class IllusionistStrikeIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    // The Strike tag is how the game/relics identify a starter Strike (Leafy Poultice, Hellraiser, …).
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Strike };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    public IllusionistStrikeIllusionist()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
