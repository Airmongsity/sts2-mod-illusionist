using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 献祭 (SacrificeIllusionist) — 0 cost Skill, Uncommon.
/// Gain 2 energy and draw 2 cards. If you have any mirror images, destroy one of them.
/// Upgraded: draw 4 cards instead of 2.
/// </summary>
public sealed class SacrificeIllusionist : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.CopyToken,
    };

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

        ICombatState? combat = base.Owner.Creature.CombatState;
        if (combat == null) return;

        Creature? clone = combat.Allies
            .FirstOrDefault(c => c.Monster is MirrorClone && c.PetOwner == base.Owner && c.IsAlive);
        if (clone != null)
        {
            await CreatureCmd.Kill(clone, force: true);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
