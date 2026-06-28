using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace Illusionist.Scripts;

/// <summary>
/// Loads the Illusionist's own bitmap art (shipped raw in the mod PCK under
/// <c>res://illusionist/art/</c>) into Godot textures at runtime.
///
/// <para>The build packs these files verbatim (no Godot import step), so we can't reference them as
/// <c>Texture2D</c> resources from a scene — instead we read the raw bytes with <see cref="Godot.FileAccess"/>
/// and decode them via <see cref="Image"/>'s buffer loaders, exactly how a static-image character is
/// done. Results are cached; a failed load logs and returns null so callers fall back to the borrowed
/// Necrobinder art rather than crashing.</para>
/// </summary>
public static class IllusionistArt
{
    private const string CombatBodyPath = "res://illusionist/art/illusionist.jpg";
    private const string CharacterSelectBgPath = "res://illusionist/art/illusionist_background.png";
    // Avatars are resolved WITHOUT an extension: the loader tries .tga / .webp / .png in turn (the
    // first two carry an alpha channel; 24-bit PNG does not). An optional grayscale companion file
    // "<base>_mask.png" supplies alpha for tools that can only export opaque images.
    private const string TopBarIconBase = "res://illusionist/art/avatar-s";
    private const string SelectPortraitBase = "res://illusionist/art/avatar-m";

    /// <summary>
    /// Pixels whose R, G and B are all at or above this (0..1) are treated as the combat image's
    /// white backdrop and made fully transparent. The source is a JPEG (no alpha), so we key the
    /// white out ourselves. Lower it if a pale halo remains; raise it if light parts of the
    /// character are being eaten.
    /// </summary>
    private const float WhiteKeyThreshold = 0.90f;

    private static ImageTexture? _combatBody;
    private static bool _combatBodyTried;

    private static ImageTexture? _characterSelectBg;
    private static bool _characterSelectBgTried;

    private static ImageTexture? _topBarIcon;
    private static bool _topBarIconTried;

    private static ImageTexture? _selectPortrait;
    private static bool _selectPortraitTried;

    /// <summary>The static in-combat character image (replaces the Necrobinder Spine body).</summary>
    public static ImageTexture? CombatBody
    {
        get
        {
            if (!_combatBodyTried)
            {
                _combatBodyTried = true;
                _combatBody = LoadJpg(CombatBodyPath, keyWhite: true);
            }
            return _combatBody;
        }
    }

    /// <summary>The character-select backdrop image.</summary>
    public static ImageTexture? CharacterSelectBackground
    {
        get
        {
            if (!_characterSelectBgTried)
            {
                _characterSelectBgTried = true;
                _characterSelectBg = LoadPng(CharacterSelectBgPath);
            }
            return _characterSelectBg;
        }
    }

    /// <summary>The in-run top-bar / run-history avatar (square; replaces Necrobinder's 85×85 icon).</summary>
    public static ImageTexture? TopBarIcon
    {
        get
        {
            if (!_topBarIconTried)
            {
                _topBarIconTried = true;
                _topBarIcon = LoadAvatar(TopBarIconBase);
            }
            return _topBarIcon;
        }
    }

    /// <summary>The character-select portrait (replaces Necrobinder's 132×195 select icon).</summary>
    public static ImageTexture? SelectPortrait
    {
        get
        {
            if (!_selectPortraitTried)
            {
                _selectPortraitTried = true;
                _selectPortrait = LoadAvatar(SelectPortraitBase);
            }
            return _selectPortrait;
        }
    }

    private static ImageTexture? LoadJpg(string path, bool keyWhite = false) => Load(path, jpg: true, keyWhite);

    private static ImageTexture? LoadPng(string path) => Load(path, jpg: false, keyWhite: false);

    private static ImageTexture? Load(string path, bool jpg, bool keyWhite)
    {
        if (!Godot.FileAccess.FileExists(path))
        {
            Log.Error($"[illusionist] Art: file not found in PCK: {path}");
            return null;
        }

        byte[] bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes == null || bytes.Length == 0)
        {
            Log.Error($"[illusionist] Art: empty/unreadable file: {path}");
            return null;
        }

        Image image = new Image();
        Error err = jpg ? image.LoadJpgFromBuffer(bytes) : image.LoadPngFromBuffer(bytes);
        if (err != Error.Ok)
        {
            Log.Error($"[illusionist] Art: decode failed ({err}) for {path}");
            return null;
        }

        if (keyWhite)
        {
            KeyOutWhite(image, WhiteKeyThreshold);
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Load an avatar from "&lt;baseNoExt&gt;.(tga|webp|png)" (+ optional "_mask.png") via the shared
    /// <see cref="ArtImage"/> loader. Transparency must be real (alpha channel or mask) — no keying.
    /// </summary>
    private static ImageTexture? LoadAvatar(string baseNoExt) => ArtImage.Load(baseNoExt);

    /// <summary>
    /// Make the image's white backdrop transparent (color-key). Runs once at load.
    /// </summary>
    private static void KeyOutWhite(Image image, float threshold)
    {
        if (image.GetFormat() != Image.Format.Rgba8)
        {
            image.Convert(Image.Format.Rgba8);
        }

        int width = image.GetWidth();
        int height = image.GetHeight();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color c = image.GetPixel(x, y);
                if (c.R >= threshold && c.G >= threshold && c.B >= threshold)
                {
                    image.SetPixel(x, y, new Color(c.R, c.G, c.B, 0f));
                }
            }
        }
    }
}
