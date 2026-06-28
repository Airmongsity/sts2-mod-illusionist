using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts;

/// <summary>
/// Custom image art for the Illusionist's own potions, applied via a Harmony patch on
/// <c>PotionModel.Image</c> (see <c>Patches/RelicPotionArtPatch.cs</c>). Base-game potions use atlas
/// sprites we can't add to, so — like <see cref="CardArt"/> — we decode a raw bitmap from the PCK at
/// runtime and substitute it.
///
/// <para><b>Adding art is filename-only.</b> Drop a file at
/// <c>res://illusionist/art/potions/&lt;classname&gt;.(webp|tga|png)</c> where <c>&lt;classname&gt;</c> is
/// the potion's class name, lower-cased (e.g. <c>IllusionPotion</c> → <c>illusionpotion.webp</c>).
/// Draw it ~80×80 (square) with a real alpha channel (32-bit WebP/TGA/PNG, or a grayscale
/// <c>&lt;classname&gt;_mask.png</c> companion). Potions without a matching file keep their borrowed
/// atlas image.</para>
/// </summary>
public static class PotionArt
{
    private const string ArtDir = "res://illusionist/art/potions/";

    private static readonly Dictionary<Type, ImageTexture?> Cache = new();

    public static ImageTexture? For(PotionModel potion)
    {
        Type type = potion.GetType();
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
        if (type.Assembly != typeof(PotionArt).Assembly)
        {
            return null;
        }
        return ArtImage.Load(ArtDir + type.Name.ToLowerInvariant());
    }
}
