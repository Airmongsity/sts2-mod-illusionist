using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Interop.AutoRegistration;
using Illusionist.Scripts.Powers;

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
    /// <see cref="ModCardPileSpec.VisibleWhen"/> predicate. The button shows for the Illusionist
    /// unconditionally (they always have access to mirror cards), and for any other character that has
    /// acquired <see cref="MirrorImagePower"/> — making the mirror pile accessible to cross-class
    /// acquisitions (e.g. Prism Shard). The icon is set dynamically by <see cref="MirrorPileIconPatch"/>
    /// to show the owning character's avatar instead of Illusionist's mirror icon.
    /// Call once from Entry.Init, after assembly registration. The registered id matches <see cref="Id" />
    /// so <see cref="Type" /> / <see cref="Get" /> keep working.
    /// </summary>
    public static void Register()
    {
        ModCardPileRegistry.For("illusionist").RegisterOwned("mirror", new ModCardPileSpec
        {
            Scope = ModCardPileScope.CombatOnly,
            Style = ModCardPileUiStyle.BottomLeft,
            Anchor = new ModCardPileAnchor(ModCardPileAnchorKind.BottomLeftPrimary),
            IconPath = null,  // set dynamically by MirrorPileIconPatch
            VisibleWhen = ctx =>
            {
                if (ctx.Player == null) return false;
                // Always show for the Illusionist (they always have access to mirror cards).
                if (ctx.Player.Character is global::Illusionist.Scripts.Characters.Illusionist) return true;
                // For other characters, show only when they have acquired MirrorImagePower.
                return ctx.Player.Creature?.GetPower<MirrorImagePower>() != null;
            },
        });
    }
}
