using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib;

namespace Illusionist.Scripts;

/// <summary>
/// The mod's per-run RNG stream — deterministic, seeded from the run seed (so a given run seed always
/// produces the same rolls) and persisted across save/reload via RitsuLib's ModRunRng registry. Shared
/// by every random choice in the 幻术师 systems (折光's bolt target, 浇熄's discard shuffle, the
/// empty-mirror burst target) so they all derive from one unified seed instead of wall-clock
/// <c>new Random()</c>. (Moved out of the deleted MirrorRoster when the typed-mirror system was
/// replaced by the store/release mirrors — see next-mirror.md.)
/// </summary>
public static class IllusionistRng
{
    public static Rng RunRng(Player player) =>
        RitsuLibFramework.GetModRunRng(player, Entry.ModId, "illusionist");
}
