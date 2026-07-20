using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;
using Illusionist.Scripts.Powers;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Cards;

/// <summary>
/// 幻象风暴 (PhantasmStormIllusionist) - 2 cost Power, Ancient. [gold]复制[/gold]8 (fill the mirror
/// battery to the cap), then gain 幻象风暴: at the start of each of your turns, destroy one mirror (a
/// loaded mirror fires its stored card first, then shatters; an empty one just breaks) - the unstable
/// storm sheds one mirror a turn, so the 8-mirror burst isn't a free permanent win. Upgraded: 2 -> 1 cost.
/// The old per-mirror AoE Attack is retired.
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "PHANTASM_STORM")]
[RegisterDustyTomeCard(typeof(Characters.Illusionist))]
public sealed class PhantasmStormIllusionist : IllusionistCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.Copy, IllusionistKeywords.MirrorImage };

    public PhantasmStormIllusionist()
        : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await MirrorClone.Copy(base.Owner, 8, choiceContext);
        await PowerCmd.Apply<PhantasmStormPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
