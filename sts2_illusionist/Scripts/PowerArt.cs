using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts;

/// <summary>
/// Custom icon art for the Illusionist's own powers (the buff/debuff icons shown under the character),
/// applied via Harmony patches on <c>PowerModel.Icon</c> / <c>PowerModel.BigIcon</c> (see
/// <c>Patches/PowerArtPatch.cs</c>). Base-game powers use atlas sprites we can't add to, so — like
/// <see cref="CardArt"/> — we decode a raw bitmap at runtime and substitute it.
///
/// <para><b>Adding art is filename-only.</b> Drop a file at
/// <c>res://illusionist/art/powers/&lt;name&gt;.(webp|tga|png)</c> where <c>&lt;name&gt;</c> is the power's
/// class name with the trailing "Power" stripped, lower-cased (e.g. <c>TransmutePower</c> →
/// <c>transmute.webp</c>, <c>MirrorImagePower</c> → <c>mirrorimage.webp</c>). The one image is used for
/// both the small buff icon and the big tooltip icon — draw it ~64×64 (square) with a real alpha
/// channel (32-bit WebP/TGA/PNG, or a grayscale <c>&lt;name&gt;_mask.png</c> companion). Powers without a
/// matching file keep their (missing) borrowed icon.</para>
/// </summary>
public static class PowerArt
{
    private const string ArtDir = "res://illusionist/art/powers/";

    private static readonly Dictionary<Type, ImageTexture?> Cache = new();

    public static ImageTexture? For(PowerModel power)
    {
        Type type = power.GetType();
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
        // Only our own powers may carry custom art.
        if (type.Assembly != typeof(PowerArt).Assembly)
        {
            return null;
        }
        return ArtImage.Load(ArtDir + ShortName(type));
    }

    /// <summary>Power class name with a trailing "Power" stripped, lower-cased.</summary>
    private static string ShortName(Type type)
    {
        const string suffix = "Power";
        string name = type.Name;
        if (name.EndsWith(suffix, StringComparison.Ordinal))
        {
            name = name[..^suffix.Length];
        }
        return name.ToLowerInvariant();
    }
}
