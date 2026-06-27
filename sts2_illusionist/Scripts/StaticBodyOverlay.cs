using System;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Illusionist.Scripts;

/// <summary>
/// Replaces a borrowed (Necrobinder Spine) <see cref="NCreatureVisuals"/> body with a flat, static
/// bitmap — used for both the Illusionist's own combat body and its mirror clones until real Spine
/// art exists. We simply hide the Spine body node (<c>%Visuals</c>) and overlay our own
/// <see cref="Sprite2D"/> as a sibling.
///
/// <para>No death animation: the engine's dissolve reparents the (hidden) Spine body, so our static
/// image just disappears on death. That's intentional for now — real attack/cast/hurt/die animations
/// come with the Spine skeleton. Hiding the Spine reliably needs <c>Visible = false</c> (the Spine
/// runtime ignores SelfModulate / a transparent material), which also hides any child, so the overlay
/// can't ride the dissolve — hence a sibling and no death VFX.</para>
/// </summary>
public static class StaticBodyOverlay
{
    /// <summary>
    /// Apply the overlay once the visuals' <c>_Ready</c> has run (so <c>%Visuals</c> / <c>%Bounds</c>
    /// are resolved). Safe to call on a freshly-instantiated, not-yet-in-tree node.
    /// </summary>
    public static void ApplyOnReady(NCreatureVisuals visuals, ImageTexture texture, float targetHeightPx, float yOffset, float alpha, string nodeName)
    {
        if (visuals == null || texture == null)
        {
            return;
        }

        visuals.Ready += () => Apply(visuals, texture, targetHeightPx, yOffset, alpha, nodeName);
    }

    private static void Apply(NCreatureVisuals visuals, ImageTexture texture, float targetHeightPx, float yOffset, float alpha, string nodeName)
    {
        try
        {
            if (!GodotObject.IsInstanceValid(visuals) || visuals.GetNodeOrNull(nodeName) != null)
            {
                return;
            }

            Node2D? body = visuals.GetNodeOrNull<Node2D>("%Visuals");
            if (body == null)
            {
                Log.Error($"[illusionist] StaticBodyOverlay: %Visuals not found for {nodeName}; keeping borrowed body.");
                return;
            }

            // Reliably hide the borrowed Spine (Visible=false; the Spine runtime ignores material /
            // SelfModulate tricks). Our overlay is a sibling on the visuals root so it stays visible.
            body.Visible = false;

            Sprite2D sprite = new Sprite2D
            {
                Name = nodeName,
                Texture = texture,
                Centered = true,
                Modulate = new Color(1f, 1f, 1f, alpha),
            };
            visuals.AddChild(sprite);

            float gScaleY = Mathf.Abs(visuals.GlobalScale.Y);
            if (gScaleY < 0.0001f)
            {
                gScaleY = 1f;
            }
            float localScale = targetHeightPx / texture.GetHeight() / gScaleY;
            sprite.Scale = new Vector2(localScale, localScale);

            Vector2 globalCenter = (visuals.Bounds != null)
                ? visuals.Bounds.GetGlobalRect().GetCenter()
                : body.GlobalPosition;
            sprite.Position = visuals.ToLocal(globalCenter) + new Vector2(0f, yOffset);
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] StaticBodyOverlay: apply failed for {nodeName}: {ex}");
        }
    }
}
