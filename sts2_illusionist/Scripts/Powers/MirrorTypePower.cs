namespace Illusionist.Scripts.Powers;

/// <summary>
/// Marker base for the mirror TYPE powers — Common (守 Guard / 刃 Blade / 棘 Thorn), Uncommon (辉 Radiant /
/// 焰 Ember / 愈 Mending), and Rare (渊 Abyss / 回 Echo / 速 Swift / 裂 Rupture / 虚 Phantom / 傀 Effigy /
/// 棱 Prism).
/// The total mirror count and the bulk remove/consume operations enumerate the owner's powers
/// <c>OfType&lt;MirrorTypePower&gt;()</c>, so every type is counted and spent uniformly and new types are
/// picked up automatically. <see cref="MirrorRoster"/> also groups these live stacks by tier to enforce
/// the per-tier caps when Copy rolls the next mirror's type.
/// </summary>
public abstract class MirrorTypePower : IllusionistPower
{
}
