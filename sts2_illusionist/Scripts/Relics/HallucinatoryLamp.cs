using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using Illusionist.Scripts;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Relics;

/// <summary>
/// 迷幻灯 (Hallucinatory Lamp) — the Illusionist's starter relic.
/// At the start of each combat, Copy 1 (create a mirror image — see <see cref="MirrorImagePower"/>):
/// the first card you play each turn is replayed until you take unblocked damage.
///
/// No art yet: the icon reuses Bound Phylactery's sprite as a placeholder (RelicModel icons
/// resolve to blank/"missing" safely, but reusing an existing sprite shows a real icon).
/// </summary>
[RegisterRelic(typeof(IllusionistRelicPool))]
[RegisterCharacterStarterRelic(typeof(Characters.Illusionist))]
[RegisterTouchOfOrobasRefinement(typeof(AncientLamp))]
public sealed class HallucinatoryLamp : IllusionistRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    // Reuse Bound Phylactery's art as a placeholder until the Lamp has its own.
    protected override string IconBaseName => "boundphylactery";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new IHoverTip[]
    {
        IllusionHoverTips.Copy,
        IllusionHoverTips.CopyToken,
    };

    public override async Task BeforeCombatStart()
    {
        try
        {
            await MirrorClone.Copy(base.Owner, 1, new ThrowingPlayerChoiceContext());
            Log.Info("[illusionist] HallucinatoryLamp: combat start — Copy 1.");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] HallucinatoryLamp failed to apply Mirror Image: {ex}");
        }
    }
}
