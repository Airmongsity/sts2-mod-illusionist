using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace Illusionist.Scripts;

/// <summary>
/// Shared hover-tips (悬浮小框) for the Illusionist's custom mechanics. These let cards explain terms
/// that are NOT engine keywords — like "Copy" (复制) — the same way base-game keywords such as
/// Forge (铸造) do: a small box beside the card describing what the term means. A card surfaces one
/// by overriding <c>ExtraHoverTips</c> to return it.
///
/// Built from loc keys we ship in <c>cards.json</c> (English is the per-language fallback, so the
/// eng entry is mandatory and translations are optional) — exactly how
/// <c>HoverTipFactory.Static</c> builds the game's own static tips. <see cref="HoverTip"/> resolves
/// the LocStrings eagerly in its constructor, so the keys must exist or it throws; that's why a new
/// instance is created per access (cheap; mirrors the engine's own Static tips).
/// </summary>
public static class IllusionHoverTips
{
    /// <summary>
    /// "Copy N" — the action: create N mirror images. Loc: COPY.title / COPY.description in cards.json.
    /// Pair with <see cref="CopyToken"/> so the player also sees what the created entity does.
    /// </summary>
    public static IHoverTip Copy => new HoverTip(
        new LocString("cards", "COPY.title"),
        new LocString("cards", "COPY.description"));

    /// <summary>
    /// The mirror-image entity itself (复制品) — what a created copy does while it's on the field.
    /// Loc: COPY_TOKEN.title / COPY_TOKEN.description in cards.json.
    /// </summary>
    public static IHoverTip CopyToken => new HoverTip(
        new LocString("cards", "COPY_TOKEN.title"),
        new LocString("cards", "COPY_TOKEN.description"));

    /// <summary>
    /// "First Move" (先机) — the bonus triggers only on the first card played this turn. Loc:
    /// FIRST_MOVE.title / FIRST_MOVE.description in cards.json. Mirrors how the engine's built-in
    /// Replay keyword surfaces its own static tip via <c>HoverTipFactory.Static</c>.
    /// </summary>
    public static IHoverTip FirstMove => new HoverTip(
        new LocString("cards", "FIRST_MOVE.title"),
        new LocString("cards", "FIRST_MOVE.description"));
}
