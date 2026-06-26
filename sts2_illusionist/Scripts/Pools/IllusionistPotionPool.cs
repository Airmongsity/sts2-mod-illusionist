using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace Illusionist.Scripts;

/// <summary>
/// The Illusionist's dedicated potion pool. Potions are added via
/// <c>ModHelper.AddModelToPool&lt;IllusionistPotionPool, T&gt;()</c> in <see cref="Entry"/> (base
/// <see cref="PotionPoolModel.AllPotions"/> = GenerateAllPotions + mod additions). Shared/common
/// potions come from the game's shared pools; this only holds the Illusionist's own.
/// </summary>
public sealed class IllusionistPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "necrobinder";

    protected override IEnumerable<PotionModel> GenerateAllPotions() => Array.Empty<PotionModel>();
}
