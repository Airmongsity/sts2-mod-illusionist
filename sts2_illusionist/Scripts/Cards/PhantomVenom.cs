using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 淬毒 (Phantom Venom) — 0 cost Attack, Common (upgraded: 9 damage). Named PhantomVenomIllusionist because the
/// base game already has a card whose type is "Envenom" (model ids are derived from the type name).
/// Deal 6 damage, add a copy of this card — 幻化 (transmuted) into a 毒素 (Toxic) — to your discard
/// pile, then Exhaust.
///
/// <para>This sidesteps the "a card can't transform ITSELF mid-play (it hangs)" limitation entirely:
/// the played card simply Exhausts, and the Toxic you see is a SEPARATE, freshly-added card, so the
/// 幻化 uses the normal (visible) <see cref="Transmutation.TransmuteCards"/> path — you watch the new
/// venom morph into a Toxic. That Toxic reverts into a Phantom Venom at the start of your next turn, so
/// the card cycles back while the played copy is consumed.</para>
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "PHANTOM_VENOM")]
public sealed class PhantomVenomIllusionist : IllusionistCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.TransmuteIllusionist,
        HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Toxic>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    public PhantomVenomIllusionist()
        : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        try
        {
            Player owner = base.Owner;

            // Add a fresh copy of this card (keeps its upgrade) to the discard pile, then 幻化 it into a
            // Toxic via the normal transmute path — it is NOT the in-play card, so the transform is safe
            // and VISIBLE. The Toxic reverts to this Phantom Venom at the start of your next turn.
            CardModel venom = CreateClone();
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(venom, PileType.Discard, owner);
            if (result.cardAdded != null)
            {
                await Transmutation.TransmuteCards(new[] { result.cardAdded }, this, choiceContext,
                    _ => base.CardScope!.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Toxic>(owner));
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] PhantomVenomIllusionist: add/transmute Toxic failed: {ex}");
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
