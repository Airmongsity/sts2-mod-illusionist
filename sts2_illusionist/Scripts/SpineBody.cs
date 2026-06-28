using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Illusionist.Scripts;

/// <summary>
/// Renders the Illusionist with real Spine skeletons via the game's spine-godot runtime (the combat
/// body, its mirror clones, and the rest-site character). The skeletons were authored in Spine 3.8 and
/// converted to the runtime version (4.2.43) with <c>SpineSkeletonDataConverter</c>; they ship in the
/// PCK as <c>illusionist.skel</c> (combat) / <c>illusionist_rest.skel</c> (rest), sharing one atlas +
/// texture (<c>illusionist.atlas</c> + <c>illusionist.png</c>, the PNG packed as an imported
/// <c>.ctex</c> — see <c>tools/build_pck.gd</c>).
///
/// <para>spine-godot has no C# bindings, so everything is driven by name through <see cref="ClassDB"/>
/// reflection. Each skeleton's heavy <c>SpineSkeletonDataResource</c> is built once and cached; every
/// <c>SpineSprite</c> (player + clones) shares it. If anything is missing the callers fall back to the
/// flat bitmap, so this can never crash the character.</para>
/// </summary>
public static class SpineBody
{
    public const string CombatSkel = "res://illusionist/art/illusionist.skel";
    public const string RestSkel = "res://illusionist/art/illusionist_rest.skel";

    // Each skeleton has its own atlas: the combat body is a multi-frame flipbook packed by Spine
    // (skeleton.atlas -> skeleton.png / skeleton2.png), the rest body is a single image.
    private const string CombatAtlas = "res://illusionist/art/skeleton.atlas";
    private const string RestAtlas = "res://illusionist/art/illusionist.atlas";

    private static string AtlasFor(string skelPath) => (skelPath == RestSkel) ? RestAtlas : CombatAtlas;

    /// <summary>Animation names to prefer, in order; falls back to the skeleton's first animation.</summary>
    private static readonly string[] PreferredAnimations = { "idle_loop", "animation" };

    /// <summary>Scale multiplier applied to the swapped combat body (our skeleton is smaller). Tune.</summary>
    private const float CombatSizeMultiplier = 1.4f;

    private static readonly Dictionary<string, GodotObject?> DataBySkel = new();
    private static readonly List<GodotObject> KeepAlive = new(); // hold atlas/file/data refs for the process
    private static bool _extChecked;
    private static bool _extPresent;

    private static bool SpinePresent()
    {
        if (!_extChecked)
        {
            _extChecked = true;
            _extPresent = ClassDB.ClassExists("SpineSprite");
            if (!_extPresent)
            {
                Log.Error("[illusionist] Spine: spine-godot GDExtension not present; using static body.");
            }
        }
        return _extPresent;
    }

    /// <summary>Build (once, cached) the shared skeleton data for a .skel path, or null on failure.</summary>
    private static GodotObject? GetData(string skelPath)
    {
        if (DataBySkel.TryGetValue(skelPath, out GodotObject? cached))
        {
            return cached;
        }

        GodotObject? data = BuildData(skelPath);
        DataBySkel[skelPath] = data;
        return data;
    }

    private static GodotObject? BuildData(string skelPath)
    {
        try
        {
            if (!SpinePresent())
            {
                return null;
            }
            string atlasPath = AtlasFor(skelPath);
            if (!Godot.FileAccess.FileExists(skelPath) || !Godot.FileAccess.FileExists(atlasPath))
            {
                Log.Error($"[illusionist] Spine: missing {skelPath} or {atlasPath}; using static body.");
                return null;
            }

            GodotObject atlas = ClassDB.Instantiate("SpineAtlasResource").AsGodotObject();
            atlas.Call("load_from_atlas_file", atlasPath);

            GodotObject skeletonFile = ClassDB.Instantiate("SpineSkeletonFileResource").AsGodotObject();
            skeletonFile.Call("load_from_file", skelPath);

            GodotObject data = ClassDB.Instantiate("SpineSkeletonDataResource").AsGodotObject();
            data.Call("set_atlas_res", atlas);
            data.Call("set_skeleton_file_res", skeletonFile);

            if (!data.Call("is_skeleton_data_loaded").AsBool())
            {
                Log.Error($"[illusionist] Spine: skeleton data failed to load for {skelPath}; using static body.");
                return null;
            }

            KeepAlive.Add(atlas);
            KeepAlive.Add(skeletonFile);
            KeepAlive.Add(data);
            return data;
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Spine: building skeleton data failed for {skelPath}: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Replace the creature's OWN spine body (the game-driven <c>%Visuals</c> <c>SpineSprite</c>) with our
    /// combat skeleton, once <c>_Ready</c> has run. This is what makes attack/cast/hurt/die animate: the
    /// game's animator already drives that node by animation name (idle_loop / attack / cast / hurt /
    /// die), and our skeleton has the same names — so swapping its <c>skeleton_data_res</c> hands the whole
    /// animation system to our art. (An overlay sprite the game can't see only ever shows a frozen idle.)
    /// Returns false if Spine is unavailable so the caller can fall back. Used by the player and clones.
    /// </summary>
    public static bool SwapOnReady(NCreatureVisuals visuals, float alpha)
    {
        if (visuals == null || GetData(CombatSkel) == null)
        {
            return false;
        }

        visuals.Ready += () => Swap(visuals, alpha);
        return true;
    }

    private static void Swap(NCreatureVisuals visuals, float alpha)
    {
        try
        {
            if (!GodotObject.IsInstanceValid(visuals))
            {
                return;
            }
            Node2D? body = visuals.GetNodeOrNull<Node2D>("%Visuals");
            if (body == null || body.HasMeta("illusionist_swapped"))
            {
                return;
            }
            GodotObject? data = GetData(CombatSkel);
            if (data == null)
            {
                return;
            }

            // Diagnostics: dump the body + its children so we can see exactly what's attached in-game.
            Log.Info($"[illusionist] Swap: body='{body.Name}' class={body.GetClass()} parent='{body.GetParent()?.Name}' scaleBefore={body.Scale} children={body.GetChildCount()}");
            foreach (Node ch in body.GetChildren())
            {
                Log.Info($"[illusionist] Swap child: '{ch.Name}' class={ch.GetClass()}");
            }

            body.Call("set_skeleton_data_res", data);   // game now drives OUR skeleton
            body.Modulate = new Color(1f, 1f, 1f, alpha);
            body.Scale = body.Scale * CombatSizeMultiplier;   // our skeleton is smaller than Necrobinder's
            body.SetMeta("illusionist_swapped", true);
            Log.Info($"[illusionist] Swap: scaleAfter={body.Scale}");

            // REMOVE the borrowed Necrobinder bone-attached VFX (head fire, scythe, flame) entirely —
            // hiding can be undone by the game, so free them. Our skeleton renders via SpineMesh2D
            // children (never matched here), so the body itself is untouched.
            foreach (Node child in body.GetChildren())
            {
                string cls = child.GetClass();
                string nm = child.Name;
                if (cls == "SpineMesh2D" || cls == "SpineSprite")
                {
                    continue;
                }
                bool isBorrowedVfx = cls == "SpineBoneNode" || cls == "SpineSlotNode"
                    || nm.Contains("Vfx") || nm.Contains("Flame") || nm.Contains("Fire") || nm.Contains("Scythe") || nm.Contains("Bone");
                if (isBorrowedVfx)
                {
                    Log.Info($"[illusionist] Swap: removing borrowed vfx '{nm}' ({cls})");
                    child.QueueFree();
                }
            }

            // Bootstrap the idle so it animates even before the game's next SetAnimation call.
            GodotObject? animState = body.Call("get_animation_state").AsGodotObject();
            string anim = PickAnimation(data);
            if (animState != null && anim.Length > 0)
            {
                animState.Call("set_animation", anim, true, 0);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] SpineBody: skeleton swap failed: {ex}");
        }
    }

    /// <summary>
    /// Build a standalone <c>SpineSprite</c> for the given skeleton, modulated to <paramref name="alpha"/>.
    /// Used by the rest-site / shop overlays (which aren't game-driven). The caller adds it to the tree,
    /// positions it (<see cref="Place"/>), then starts it (<see cref="Play"/>) — animation must be set
    /// AFTER the sprite is in the tree or it silently no-ops. Returns null if Spine is unavailable.
    /// </summary>
    public static Node2D? CreateSprite(string skelPath, float alpha, string nodeName)
    {
        GodotObject? data = GetData(skelPath);
        if (data == null)
        {
            return null;
        }

        Node2D sprite = ClassDB.Instantiate("SpineSprite").As<Node2D>();
        sprite.Name = nodeName;
        sprite.Set("skeleton_data_res", data);
        sprite.Modulate = new Color(1f, 1f, 1f, alpha);
        return sprite;
    }

    /// <summary>Start a sprite's preferred looping animation. Call AFTER it has been added to the tree.</summary>
    public static void Play(Node2D sprite, string skelPath)
    {
        GodotObject? data = GetData(skelPath);
        if (data == null || !GodotObject.IsInstanceValid(sprite))
        {
            return;
        }
        GodotObject? animState = sprite.Call("get_animation_state").AsGodotObject();
        string anim = PickAnimation(data);
        if (animState != null && anim.Length > 0)
        {
            animState.Call("set_animation", anim, true, 0);
        }
    }

    /// <summary>
    /// Scale a sprite to <paramref name="targetHeightPx"/> on-screen and position it so the skeleton's
    /// content box is centred on <paramref name="anchorLocal"/> (in the sprite's parent space), plus
    /// <paramref name="yOffset"/>. Spine renders Y-up, so a Spine +Y maps to Godot -Y.
    /// </summary>
    public static void Place(Node2D sprite, string skelPath, Vector2 anchorLocal, float targetHeightPx, float parentGlobalScaleY, float yOffset)
    {
        GodotObject? data = GetData(skelPath);
        if (data == null)
        {
            return;
        }

        float height = (float)data.Call("get_height").AsDouble();
        float gScaleY = (parentGlobalScaleY < 0.0001f) ? 1f : parentGlobalScaleY;
        float localScale = (height > 0.0001f) ? targetHeightPx / height / gScaleY : 1f;
        sprite.Scale = new Vector2(localScale, localScale);

        float contentCx = (float)data.Call("get_x").AsDouble() + (float)data.Call("get_width").AsDouble() * 0.5f;
        float contentCyUp = (float)data.Call("get_y").AsDouble() + height * 0.5f;
        Vector2 contentCenterLocal = new Vector2(contentCx, -contentCyUp) * localScale;

        sprite.Position = anchorLocal - contentCenterLocal + new Vector2(0f, yOffset);
    }

    private static string PickAnimation(GodotObject data)
    {
        var names = new List<string>();
        foreach (Variant v in data.Call("get_animations").AsGodotArray())
        {
            GodotObject? anim = v.AsGodotObject();
            if (anim != null)
            {
                names.Add(anim.Call("get_name").AsString());
            }
        }
        foreach (string pref in PreferredAnimations)
        {
            if (names.Contains(pref))
            {
                return pref;
            }
        }
        return names.Count > 0 ? names[0] : string.Empty;
    }
}
