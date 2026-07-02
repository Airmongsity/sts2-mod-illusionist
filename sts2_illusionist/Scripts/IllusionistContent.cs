using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Scaffolding.Content;

namespace Illusionist.Scripts;

/// <summary>
/// Convention-based art paths for Illusionist content, mirroring the naming the old runtime-decoded
/// loaders (CardArt/PowerArt/PotionArt/RelicArtPath) used. All art ships as IMPORTED resources in the
/// PCK (see build_pck.gd IMPORTED_DIRS) so RitsuLib's asset-override patches can ResourceLoader-load
/// them. Returns null (→ engine default) when the file doesn't exist or the type isn't ours.
/// </summary>
internal static class IllusionistArtPaths
{
    private static readonly Dictionary<Type, string?> Cache = new();

    /// <summary>cards/&lt;name&gt;.png — class name with the "Illusionist" prefix/suffix stripped.</summary>
    public static string? CardPortrait(Type type) =>
        Resolve(type, "res://illusionist/art/cards/", StripAffix(type.Name), ".png");

    /// <summary>powers/&lt;name&gt;.webp — class name with the "Power" suffix stripped.</summary>
    public static string? PowerIcon(Type type) =>
        Resolve(type, "res://illusionist/art/powers/", StripSuffix(type.Name, "Power"), ".webp");

    /// <summary>relics/&lt;classname&gt;.webp.</summary>
    public static string? RelicIcon(Type type) =>
        Resolve(type, "res://illusionist/art/relics/", type.Name, ".webp");

    /// <summary>potions/&lt;classname&gt;.webp.</summary>
    public static string? PotionImage(Type type) =>
        Resolve(type, "res://illusionist/art/potions/", type.Name, ".webp");

    private static string? Resolve(Type type, string dir, string shortName, string ext)
    {
        if (Cache.TryGetValue(type, out string? cached))
        {
            return cached;
        }

        string? path = null;
        // Only our own content may carry custom art — never hijack a base-game model with a shared name.
        if (type.Assembly == typeof(IllusionistArtPaths).Assembly)
        {
            string candidate = dir + shortName.ToLowerInvariant() + ext;
            if (ResourceLoader.Exists(candidate))
            {
                path = candidate;
            }
        }

        Cache[type] = path;
        return path;
    }

    private static string StripAffix(string name)
    {
        const string tag = "Illusionist";
        if (name.EndsWith(tag, StringComparison.Ordinal))
        {
            name = name[..^tag.Length];
        }
        if (name.StartsWith(tag, StringComparison.Ordinal))
        {
            name = name[tag.Length..];
        }
        return name;
    }

    private static string StripSuffix(string name, string suffix) =>
        name.EndsWith(suffix, StringComparison.Ordinal) ? name[..^suffix.Length] : name;
}

/// <summary>
/// Base for every Illusionist card: RitsuLib card template + our pool + convention-based portrait.
/// </summary>
public abstract class IllusionistCard : ModCardTemplate
{
    protected IllusionistCard(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true)
        : base(baseCost, type, rarity, target, showInCardLibrary)
    {
    }

    public sealed override CardPoolModel Pool => ModelDb.CardPool<IllusionistCardPool>();

    public override string? CustomPortraitPath => IllusionistArtPaths.CardPortrait(GetType());
}

/// <summary>Base for Illusionist powers: RitsuLib power template + convention-based icon.</summary>
public abstract class IllusionistPower : ModPowerTemplate
{
    public override string? CustomIconPath => IllusionistArtPaths.PowerIcon(GetType());

    public override string? CustomBigIconPath => IllusionistArtPaths.PowerIcon(GetType());
}

/// <summary>Base for Illusionist relics: RitsuLib relic template + convention-based icon.</summary>
public abstract class IllusionistRelic : ModRelicTemplate
{
    public override string? CustomIconPath => IllusionistArtPaths.RelicIcon(GetType());

    public override string? CustomIconOutlinePath => IllusionistArtPaths.RelicIcon(GetType());

    public override string? CustomBigIconPath => IllusionistArtPaths.RelicIcon(GetType());
}

/// <summary>Base for Illusionist potions: RitsuLib potion template + convention-based image.</summary>
public abstract class IllusionistPotion : ModPotionTemplate
{
    public override string? CustomImagePath => IllusionistArtPaths.PotionImage(GetType());
}
