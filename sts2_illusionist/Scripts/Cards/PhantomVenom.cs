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

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 淬毒 (Phantom Venom) — 0 cost Attack, Common (upgraded: 18 damage). Named PhantomVenom because the
/// base game already has a card whose type is "Envenom" (model ids are derived from the type name).
/// Deal 12 damage, then 幻化 (transmute) this card into a Toxic — it reverts back at the start of your
/// next turn, so it stays a repeatable cheap nuke while temporarily polluting your pile.
///
/// <para>Implementation: a card can't safely transform ITSELF mid-play (it hangs — no base game card
/// does it). Instead this card removes itself cleanly via the engine's post-play <see cref="PileType.None"/>
/// result pile, drops a Toxic in the discard, and registers that Toxic to revert into a clone of this
/// card next turn (via <see cref="Transmutation.RegisterRevert"/>).</para>
/// </summary>
public sealed class PhantomVenom : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Transmute,
        HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Toxic>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move),
    };

    public PhantomVenom()
        : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // We replace this card with a Toxic on play, so it must NOT also go to the discard pile.
    // PileType.None makes the engine remove it from combat after the play resolves (cleanly, not
    // mid-play). The Toxic + revert are set up in OnPlay.
    protected override PileType GetResultPileTypeForCardPlay() => PileType.None;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // Replays reuse this instance and it's removed only once post-play, so transmute just once.
        if (cardPlay.PlayIndex < cardPlay.PlayCount - 1)
        {
            return;
        }

        try
        {
            Player owner = base.Owner;
            CardModel revertTo = CreateClone();   // a copy of this attack (keeps upgrade) to revert into
            CardModel toxic = base.CardScope!.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Toxic>(owner);

            await CardPileCmd.Add(toxic, PileType.Discard);
            await Transmutation.RegisterRevert(owner, choiceContext, this, revertTo, toxic);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] PhantomVenom: transmute-to-Toxic failed: {ex}");
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(6m);
    }
}
