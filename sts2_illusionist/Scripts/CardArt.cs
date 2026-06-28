using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts;

/// <summary>
/// Per-card portrait art for the Illusionist. The mod packs raw PNGs (no Godot import step) under
/// <c>res://illusionist/art/cards/</c>, so — exactly like <see cref="IllusionistArt"/> for the body —
/// we can't reference them as <c>Texture2D</c> resources from a scene. Instead we decode the raw bytes
/// into an <see cref="ImageTexture"/> at runtime (cached) and hand it to the card via a Harmony patch
/// on <c>CardModel.Portrait</c> (see <c>Patches/CardPortraitPatch.cs</c>).
///
/// <para><b>Adding art is filename-only — no code change.</b> Drop a PNG at
/// <c>res://illusionist/art/cards/&lt;name&gt;.png</c> where <c>&lt;name&gt;</c> is the card's short
/// name (its class name with the "Illusionist" prefix/suffix stripped, lower-cased):
/// <list type="bullet">
/// <item><c>IllusionistStrikeIllusionist</c> → <c>strike.png</c></item>
/// <item><c>IllusionistDefendIllusionist</c> → <c>defend.png</c></item>
/// <item><c>ReversalIllusionist</c> → <c>reversal.png</c></item>
/// <item><c>PhantomVenomIllusionist</c> → <c>phantomvenom.png</c></item>
/// </list>
/// Draw at the 250×190 / 25:19 card-portrait ratio for the cleanest fit. Cards with no matching file
/// keep the default portrait. Only the Illusionist mod's own cards are considered, so a file can never
/// hijack a base-game card that happens to share a name.</para>
/// </summary>
public static class CardArt
{
    private const string ArtDir = "res://illusionist/art/cards/";

    private static readonly Dictionary<Type, ImageTexture?> Cache = new();

    /// <summary>
    /// The custom portrait for <paramref name="card"/>, or null if it has none (caller should then let
    /// the engine resolve the default portrait). Resolved once and cached per card type.
    /// </summary>
    public static ImageTexture? For(CardModel card)
    {
        Type type = card.GetType();
        if (Cache.TryGetValue(type, out ImageTexture? cached))
        {
            return cached;
        }

        ImageTexture? texture = Resolve(type);
        Cache[type] = texture;
        return texture;
    }

    private static ImageTexture? Resolve(Type type)
    {
        // Only our own cards may carry custom art — never hijack a base-game card with a shared name.
        if (type.Assembly != typeof(CardArt).Assembly)
        {
            return null;
        }

        string path = ArtDir + ShortName(type) + ".png";
        return Godot.FileAccess.FileExists(path) ? Load(path) : null;
    }

    /// <summary>Card class name with the "Illusionist" prefix/suffix stripped, lower-cased.</summary>
    private static string ShortName(Type type)
    {
        const string tag = "Illusionist";
        string name = type.Name;
        if (name.EndsWith(tag, StringComparison.Ordinal))
        {
            name = name[..^tag.Length];
        }
        if (name.StartsWith(tag, StringComparison.Ordinal))
        {
            name = name[tag.Length..];
        }
        return name.ToLowerInvariant();
    }

    private static ImageTexture? Load(string path)
    {
        byte[] bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes == null || bytes.Length == 0)
        {
            Log.Error($"[illusionist] CardArt: empty/unreadable file: {path}");
            return null;
        }

        Image image = new Image();
        Error err = image.LoadPngFromBuffer(bytes);
        if (err != Error.Ok)
        {
            Log.Error($"[illusionist] CardArt: PNG decode failed ({err}) for {path}");
            return null;
        }

        return ImageTexture.CreateFromImage(image);
    }
}
