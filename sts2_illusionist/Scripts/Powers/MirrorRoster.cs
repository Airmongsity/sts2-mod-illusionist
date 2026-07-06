using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib;

namespace Illusionist.Scripts.Powers;

/// <summary>
/// Picks the TYPE of each mirror created by Copy, using a per-tier CAP on how many of each rarity may be
/// live at once plus a weighted roll among the tiers that still have room.
///
/// <para>Caps (live stacks this combat): Common <see cref="CommonCap"/>, Uncommon <see cref="UncommonCap"/>,
/// Rare <see cref="RareCap"/>. A tier that is at its cap is excluded — so once you hold 3 Common the next
/// Copy must be Uncommon or Rare, once you hold 3 Common + 3 Uncommon it must be Rare, and so on. When
/// EVERY tier is capped, Common and Uncommon re-open as overflow (Rare stays hard-capped at
/// <see cref="RareCap"/>, so a board can never hold more than that many Rares). Among the open tiers the
/// roll is weighted Common 70 / Uncommon 25 / Rare 5, then a uniform type within the chosen tier.</para>
///
/// <para>The situational "nudge" toward survival mirrors when the player is in danger is deliberately
/// NOT here yet — by decision it's tuned last, once every type exists.</para>
/// </summary>
public static class MirrorRoster
{
    /// <summary>
    /// The mod's per-run RNG stream — deterministic, seeded from the run seed (so a given run seed always
    /// generates the same mirrors) and persisted across save/reload via RitsuLib's ModRunRng registry.
    /// Shared by every random choice in the 幻术师 systems (mirror-type selection, 浇熄's discard shuffle)
    /// so they all derive from one unified seed instead of a wall-clock <c>new Random()</c>.
    /// </summary>
    public static Rng RunRng(Player player) =>
        RitsuLibFramework.GetModRunRng(player, Entry.ModId, "illusionist");

    private const int WeightCommon = 70;
    private const int WeightUncommon = 25;
    private const int WeightRare = 5;

    // Max live stacks per tier before the tier is excluded from selection.
    private const int CommonCap = 3;
    private const int UncommonCap = 3;
    private const int RareCap = 2;

    private delegate Task Applier(PlayerChoiceContext choiceContext, Creature owner);

    private readonly struct MirrorDef
    {
        public readonly Type PowerType;
        public readonly Applier Apply;
        // Optional situational gate: if set and it returns false, this type isn't offered right now.
        public readonly Func<Creature, bool>? CanOffer;

        public MirrorDef(Type powerType, Applier apply, Func<Creature, bool>? canOffer = null)
        {
            PowerType = powerType;
            Apply = apply;
            CanOffer = canOffer;
        }
    }

    // Prism only matters against 2+ enemies; don't offer it in a duel.
    private static bool HasMultipleEnemies(Creature owner) =>
        (owner.CombatState?.HittableEnemies.Count ?? 0) >= 2;

    private static readonly MirrorDef[] CommonMirrors =
    {
        new MirrorDef(typeof(GuardMirrorPower), (cc, o) => PowerCmd.Apply<GuardMirrorPower>(cc, o, 1, o, null)),
        new MirrorDef(typeof(BladeMirrorPower), (cc, o) => PowerCmd.Apply<BladeMirrorPower>(cc, o, 1, o, null)),
        new MirrorDef(typeof(ThornMirrorPower), (cc, o) => PowerCmd.Apply<ThornMirrorPower>(cc, o, 1, o, null)),
    };

    private static readonly MirrorDef[] UncommonMirrors =
    {
        new MirrorDef(typeof(RadiantMirrorPower), (cc, o) => PowerCmd.Apply<RadiantMirrorPower>(cc, o, 1, o, null)),
        new MirrorDef(typeof(EmberMirrorPower), (cc, o) => PowerCmd.Apply<EmberMirrorPower>(cc, o, 1, o, null)),
        new MirrorDef(typeof(MendingMirrorPower), (cc, o) => PowerCmd.Apply<MendingMirrorPower>(cc, o, 1, o, null)),
    };

    private static readonly MirrorDef[] RareMirrors =
    {
        new MirrorDef(typeof(AbyssMirrorPower), (cc, o) => PowerCmd.Apply<AbyssMirrorPower>(cc, o, 1, o, null)),
        new MirrorDef(typeof(EchoMirrorPower), (cc, o) => PowerCmd.Apply<EchoMirrorPower>(cc, o, 1, o, null)),
        new MirrorDef(typeof(SwiftMirrorPower), (cc, o) => PowerCmd.Apply<SwiftMirrorPower>(cc, o, 1, o, null)),
        new MirrorDef(typeof(RuptureMirrorPower), (cc, o) => PowerCmd.Apply<RuptureMirrorPower>(cc, o, 1, o, null)),
        new MirrorDef(typeof(PhantomMirrorPower), (cc, o) => PowerCmd.Apply<PhantomMirrorPower>(cc, o, 1, o, null)),
        new MirrorDef(typeof(EffigyMirrorPower), (cc, o) => PowerCmd.Apply<EffigyMirrorPower>(cc, o, 1, o, null)),
        new MirrorDef(typeof(PrismMirrorPower), (cc, o) => PowerCmd.Apply<PrismMirrorPower>(cc, o, 1, o, null), HasMultipleEnemies),
    };

    /// <summary>Roll one mirror's type (respecting the per-tier caps) and apply that type's power to the owner.</summary>
    public static async Task ApplyRandom(Player player, PlayerChoiceContext choiceContext)
    {
        Creature owner = player.Creature;
        Rng rng = RunRng(player);

        bool commonOpen = TierCount(owner, CommonMirrors) < CommonCap;
        bool uncommonOpen = TierCount(owner, UncommonMirrors) < UncommonCap;
        bool rareOpen = TierCount(owner, RareMirrors) < RareCap;

        // Every tier capped: overflow into Common / Uncommon (Rare stays hard-capped).
        if (!commonOpen && !uncommonOpen && !rareOpen)
        {
            commonOpen = true;
            uncommonOpen = true;
        }

        int wCommon = commonOpen ? WeightCommon : 0;
        int wUncommon = uncommonOpen ? WeightUncommon : 0;
        int wRare = rareOpen ? WeightRare : 0;
        int total = wCommon + wUncommon + wRare;
        if (total <= 0)
        {
            return;
        }

        int roll = rng.NextInt(total);
        MirrorDef[] tier = roll < wCommon ? CommonMirrors
            : roll < wCommon + wUncommon ? UncommonMirrors
            : RareMirrors;

        // Drop types whose situational gate says "not now" (e.g. Prism with a single enemy).
        MirrorDef[] eligible = tier.Where(d => d.CanOffer == null || d.CanOffer(owner)).ToArray();
        if (eligible.Length == 0)
        {
            return;
        }

        await eligible[rng.NextInt(eligible.Length)].Apply(choiceContext, owner);
    }

    /// <summary>Sum the owner's live stacks across every type in the given tier.</summary>
    private static int TierCount(Creature owner, MirrorDef[] tier)
    {
        int sum = 0;
        foreach (MirrorTypePower power in owner.Powers.OfType<MirrorTypePower>())
        {
            Type type = power.GetType();
            foreach (MirrorDef def in tier)
            {
                if (def.PowerType == type)
                {
                    sum += (int)power.Amount;
                    break;
                }
            }
        }
        return sum;
    }
}
