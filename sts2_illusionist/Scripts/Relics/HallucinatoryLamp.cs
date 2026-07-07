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
/// At the start of each combat, Copy 2 (two empty mirrors — see <see cref="MirrorImagePower"/>):
/// one slot for the first-card imprint, one spare as armor/magazine, so the engine has a clip
/// from turn 1.
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

    protected override IEnumerable<string> RegisteredKeywordIds => new[] { IllusionistKeywords.CopyId, IllusionistKeywords.MirrorImageId };

    public override async Task BeforeCombatStart()
    {
        try
        {
            await MirrorClone.Copy(base.Owner, 2, new ThrowingPlayerChoiceContext());
            Log.Info("[illusionist] HallucinatoryLamp: combat start — Copy 2.");
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] HallucinatoryLamp failed to apply Mirror Image: {ex}");
        }
    }
}
