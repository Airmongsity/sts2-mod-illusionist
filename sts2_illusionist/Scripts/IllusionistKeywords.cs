using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace Illusionist.Scripts;

/// <summary>
/// Declares the Illusionist's custom mechanics as RitsuLib owned card keywords (replacing the old
/// hand-rolled IllusionHoverTips). Each keyword gets a hover tip built from
/// <c>card_keywords.json</c> (<c>ILLUSIONIST_KEYWORD_*.title/.description</c>) and is attached to a
/// card by including it in <see cref="CardModel.CanonicalKeywords"/> via <see cref="IllusionistKeywords"/>.
/// Description placement stays None — the terms are already written into the card text.
/// </summary>
[RegisterOwnedCardKeyword("copy")]
[RegisterOwnedCardKeyword("mirror_image")]
[RegisterOwnedCardKeyword("execute")]
[RegisterOwnedCardKeyword("first_move")]
[RegisterOwnedCardKeyword("transmute")]
[RegisterOwnedCardKeyword(
    "severe_cold",
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
public sealed class IllusionistKeywordRegistrations
{
}

/// <summary>Qualified ids + resolved <see cref="CardKeyword"/> values for the keywords above.</summary>
public static class IllusionistKeywords
{
    /// <summary>"Copy N" — the action: create N mirror images.</summary>
    public const string CopyId = "ILLUSIONIST_KEYWORD_COPY";

    /// <summary>The mirror-image entity itself (复制品) — what a created copy does on the field.</summary>
    public const string MirrorImageId = "ILLUSIONIST_KEYWORD_MIRROR_IMAGE";

    public const string ExecuteId = "ILLUSIONIST_KEYWORD_EXECUTE";

    /// <summary>"First Move" (先机) — the bonus triggers only on the first card played this turn.</summary>
    public const string FirstMoveId = "ILLUSIONIST_KEYWORD_FIRST_MOVE";

    /// <summary>幻化 — transform a card; it reverts one step at the start of your next turn.</summary>
    public const string TransmuteId = "ILLUSIONIST_KEYWORD_TRANSMUTE";

    /// <summary>严寒 — at combat start, apply Frozen to this card's combat copy.</summary>
    public const string SevereColdId = "ILLUSIONIST_KEYWORD_SEVERE_COLD";

    public static CardKeyword Copy => CopyId.GetModCardKeyword();

    public static CardKeyword MirrorImage => MirrorImageId.GetModCardKeyword();

    public static CardKeyword Execute => ExecuteId.GetModCardKeyword();

    public static CardKeyword FirstMove => FirstMoveId.GetModCardKeyword();

    public static CardKeyword Transmute => TransmuteId.GetModCardKeyword();

    public static CardKeyword SevereCold => SevereColdId.GetModCardKeyword();
}
