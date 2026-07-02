using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Potions;

/// <summary>
/// 预见药剂 (ForesightIllusionist Draught) — Uncommon, combat-only. Draw 2 cards and Retain your hand this
/// turn (via the engine's RetainHandPower — the same mechanism as Stable Serum / 稳定血清).
/// </summary>
[RegisterPotion(typeof(IllusionistPotionPool))]
public sealed class ForesightDraught : IllusionistPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        Player? player = target?.Player;
        if (player == null)
        {
            return;
        }

        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, player);
        await PowerCmd.Apply<RetainHandPower>(choiceContext, player.Creature, 1, base.Owner.Creature, null);
    }
}
