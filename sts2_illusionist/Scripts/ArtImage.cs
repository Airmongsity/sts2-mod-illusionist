using System;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace Illusionist.Scripts;

/// <summary>
/// Shared runtime loader for the mod's raw bitmap art (packed verbatim in the PCK, no Godot import
/// step). Resolves a file WITHOUT an extension by trying alpha-capable formats first
/// (<c>.tga</c> → <c>.webp</c> → <c>.png</c>) and supports an optional grayscale companion
/// <c>&lt;base&gt;_mask.png</c> (white = opaque, black = transparent) so transparency can be supplied
/// even by tools that only export opaque images. Used by <see cref="RelicArt"/> / <see cref="PotionArt"/>
/// (and the avatar loader). No color-keying — transparency must be real (alpha channel or mask).
/// </summary>
public static class ArtImage
{
    // Alpha-capable formats first; .jpg/.png(24-bit) last (opaque — pair with a "_mask.png" for cut-out).
    private static readonly string[] Extensions = { ".tga", ".webp", ".png", ".jpg", ".jpeg" };

    /// <summary>Load "&lt;baseNoExt&gt;.(tga|webp|png)" (+ optional "_mask.png") as a texture, or null.</summary>
    public static ImageTexture? Load(string baseNoExt)
    {
        Image? image = LoadImage(baseNoExt);
        return image != null ? ImageTexture.CreateFromImage(image) : null;
    }

    /// <summary>Load "&lt;baseNoExt&gt;.(tga|webp|png)" (+ optional "_mask.png") as an <see cref="Image"/>, or null.</summary>
    public static Image? LoadImage(string baseNoExt)
    {
        Image? image = null;
        foreach (string ext in Extensions)
        {
            image = Decode(baseNoExt + ext);
            if (image != null)
            {
                break;
            }
        }
        if (image == null)
        {
            return null;
        }

        ApplyMask(image, baseNoExt + "_mask.png");
        return image;
    }

    /// <summary>Decode a single file to an <see cref="Image"/> by extension, or null if absent/unreadable.</summary>
    public static Image? Decode(string path)
    {
        if (!Godot.FileAccess.FileExists(path))
        {
            return null;
        }
        byte[] bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        Image image = new Image();
        string ext = path.GetExtension().ToLowerInvariant();
        Error err = ext switch
        {
            "tga" => image.LoadTgaFromBuffer(bytes),
            "webp" => image.LoadWebpFromBuffer(bytes),
            "jpg" or "jpeg" => image.LoadJpgFromBuffer(bytes),
            _ => image.LoadPngFromBuffer(bytes),
        };
        if (err != Error.Ok)
        {
            Log.Error($"[illusionist] ArtImage: decode failed ({err}) for {path}");
            return null;
        }
        return image;
    }

    /// <summary>If <paramref name="maskPath"/> exists, use its luminance as the image's alpha channel.</summary>
    private static void ApplyMask(Image image, string maskPath)
    {
        Image? mask = Decode(maskPath);
        if (mask == null)
        {
            return;
        }

        if (image.GetFormat() != Image.Format.Rgba8)
        {
            image.Convert(Image.Format.Rgba8);
        }
        if (mask.GetSize() != image.GetSize())
        {
            mask.Resize(image.GetWidth(), image.GetHeight());
        }

        int width = image.GetWidth();
        int height = image.GetHeight();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color c = image.GetPixel(x, y);
                Color m = mask.GetPixel(x, y);
                c.A = (m.R + m.G + m.B) / 3f;
                image.SetPixel(x, y, c);
            }
        }
    }
}
