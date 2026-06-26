using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Illusionist.Scripts;

/// <summary>
/// The Illusionist's dedicated relic pool. Relics are added via
/// <c>ModHelper.AddModelToPool&lt;IllusionistRelicPool, T&gt;()</c> in <see cref="Entry"/> (base
/// <see cref="RelicPoolModel.AllRelics"/> = GenerateAllRelics + mod additions). <c>GetUnlockedRelics</c>
/// defaults to <c>AllRelics</c>, so no epoch gating.
/// </summary>
public sealed class IllusionistRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "necrobinder";

    protected override IEnumerable<RelicModel> GenerateAllRelics() => Array.Empty<RelicModel>();
}
