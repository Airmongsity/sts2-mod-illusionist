using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts;

/// <summary>Combat-only holding pile for cards loaded into Mirror Image.</summary>
public sealed class MirrorPile
{
    public const string Id = "ILLUSIONIST_CARDPILE_MIRROR";

    /// <summary>The deterministic RitsuLib pile type registered for <see cref="Id"/>.</summary>
    public static PileType Type => Id.GetModCardPileType();

    public static CardPile Get(Player player)
    {
        return Type.GetPile(player);
    }

    public static bool IsMirrorPile(CardPile? pile)
    {
        return pile?.Type == Type;
    }

    /// <summary>
    /// Register the Mirror pile programmatically (not via [RegisterOwnedCardPile]) so we can supply a
    /// <see cref="ModCardPileSpec.VisibleWhen"/> predicate. The button only shows for the Illusionist -
    /// the Mirror pile is Illusionist-only (it was showing for EVERY character because attribute-driven
    /// registration can't pass a VisibleWhen delegate and defaults to always-visible). Call once from
    /// Entry.Init, after assembly registration. The registered id matches <see cref="Id" /> so
    /// <see cref="Type" /> / <see cref="Get" /> keep working.
    /// </summary>
    public static void Register()
    {
        ModCardPileRegistry.For("illusionist").RegisterOwned("mirror", new ModCardPileSpec
        {
            Scope = ModCardPileScope.CombatOnly,
            Style = ModCardPileUiStyle.BottomLeft,
            Anchor = new ModCardPileAnchor(ModCardPileAnchorKind.BottomLeftPrimary),
            IconPath = "res://illusionist/art/powers/mirrorimage.webp",
            VisibleWhen = ctx => ctx.Player?.Character is global::Illusionist.Scripts.Characters.Illusionist,
        });
    }
}
