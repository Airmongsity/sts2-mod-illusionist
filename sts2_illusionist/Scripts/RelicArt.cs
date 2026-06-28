using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts;

/// <summary>
/// Custom icon art for the Illusionist's own relics, applied via Harmony patches on
/// <c>RelicModel.Icon</c> / <c>RelicModel.BigIcon</c> (see <c>Patches/RelicPotionArtPatch.cs</c>).
/// Base-game relics use atlas sprites we can't add to, so — like <see cref="CardArt"/> — we decode a
/// raw bitmap from the PCK at runtime and substitute it.
///
/// <para><b>Adding art is filename-only.</b> Drop a file at
/// <c>res://illusionist/art/relics/&lt;classname&gt;.(webp|tga|png)</c> where <c>&lt;classname&gt;</c> is
/// the relic's class name, lower-cased (e.g. <c>AncientLamp</c> → <c>ancientlamp.webp</c>). The one
/// image is used for both the small relic-bar icon and the big tooltip icon — draw it ~256×256 with a
/// real alpha channel (32-bit WebP/TGA/PNG, or a grayscale <c>&lt;classname&gt;_mask.png</c> companion).
/// Relics without a matching file keep their borrowed atlas icon, so this never hijacks anything.</para>
/// </summary>
public static class RelicArt
{
    private const string ArtDir = "res://illusionist/art/relics/";

    private static readonly Dictionary<Type, ImageTexture?> Cache = new();
    private static readonly Dictionary<Type, ImageTexture?> OutlineCache = new();

    public static ImageTexture? For(RelicModel relic)
    {
        Type type = relic.GetType();
        if (Cache.TryGetValue(type, out ImageTexture? cached))
        {
            return cached;
        }

        ImageTexture? texture = Resolve(type);
        Cache[type] = texture;
        return texture;
    }

    /// <summary>
    /// A white silhouette built from the relic art's alpha, for the <c>IconOutline</c> slot (the
    /// selection glow). Without this the borrowed outline atlas sprite is missing and renders as a
    /// "NOPE" placeholder behind the icon.
    /// </summary>
    public static ImageTexture? OutlineFor(RelicModel relic)
    {
        Type type = relic.GetType();
        if (OutlineCache.TryGetValue(type, out ImageTexture? cached))
        {
            return cached;
        }

        ImageTexture? outline = ResolveOutline(type);
        OutlineCache[type] = outline;
        return outline;
    }

    private static ImageTexture? Resolve(Type type)
    {
        // Only our own relics may carry custom art.
        if (type.Assembly != typeof(RelicArt).Assembly)
        {
            return null;
        }
        return ArtImage.Load(ArtDir + type.Name.ToLowerInvariant());
    }

    private static ImageTexture? ResolveOutline(Type type)
    {
        if (type.Assembly != typeof(RelicArt).Assembly)
        {
            return null;
        }
        Image? image = ArtImage.LoadImage(ArtDir + type.Name.ToLowerInvariant());
        if (image == null)
        {
            return null;
        }

        // Flatten to a white silhouette (keep alpha) so the glow shader tints the relic's shape.
        if (image.GetFormat() != Image.Format.Rgba8)
        {
            image.Convert(Image.Format.Rgba8);
        }
        int w = image.GetWidth();
        int h = image.GetHeight();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float a = image.GetPixel(x, y).A;
                image.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        return ImageTexture.CreateFromImage(image);
    }
}
