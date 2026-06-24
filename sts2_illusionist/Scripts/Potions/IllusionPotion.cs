using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Potions;

/// <summary>
/// 幻象药水 (Illusion Potion) — Common, combat-only. Copy 1 (gain a mirror image).
/// </summary>
public sealed class IllusionPotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Copy,
        IllusionHoverTips.CopyToken,
    };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        Player? player = target?.Player;
        if (player == null)
        {
            return;
        }

        await MirrorClone.Copy(player, 1, choiceContext);
    }
}
