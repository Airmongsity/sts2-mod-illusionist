using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace Illusionist.Scripts;

/// <summary>
/// Silences one creature's outgoing damage for the duration of a scope, so a move can be run for
/// everything EXCEPT the damage it deals.
///
/// <para>逆转 (Reversal) turns an enemy's attack into Block. It used to do that by replacing the
/// whole <c>MoveState</c> with a block-only one, which threw away everything else the move did —
/// the Thieving Hopper's card theft, Living Fog's status cards, Fabricator's summon, and 63 other
/// base-game moves that bundle an attack with a non-attack effect inside a single action. The
/// telegraph kept showing those effects, so it lied. Now Reversal runs the original action
/// (<c>MoveState.PerformMove</c>, the same way 虚张声势 and 抢拍 already did) inside
/// <see cref="Suppress"/>: the theft still happens, the attack lands for 0, and the damage it
/// would have dealt is already sitting on the enemy as Block.</para>
///
/// <para>The suppression is a multiplicative damage modifier of 0, which the engine applies to
/// every damage event regardless of <see cref="ValueProp"/> — Unblockable and Unpowered move damage
/// included. It keys on the DEALER, so a suppressed enemy hits nothing during the scope, while
/// damage from other sources (other monsters, Poison, which passes no dealer) is untouched.
/// A 0-damage hit leaves no unblocked damage, so it costs no mirror
/// (<c>MirrorImagePower.AfterDamageReceived</c> ignores <c>UnblockedDamage &lt;= 0</c>).</para>
///
/// <para>Registered as a run-scoped singleton rather than a power on the enemy: powers show an icon
/// and play apply/remove VFX, and this lives for one <c>await</c>, not one turn.</para>
/// </summary>
[RegisterSingleton]
public sealed class ReversedAttack : HookedSingletonModel
{
    private static readonly HashSet<Creature> Silenced = new();

    public ReversedAttack()
        : base(HookType.Run)
    {
    }

    /// <summary>
    /// Whether this model is actually being iterated as a run hook this combat — proven by the fact
    /// that <see cref="BeforeCombatStart"/> ran, which the engine drives from the same
    /// <c>runState.IterateHookListeners</c> loop that feeds <see cref="ModifyDamageMultiplicative"/>.
    ///
    /// <para>Callers MUST check this before relying on suppression. If the singleton were ever not
    /// in the hook chain, silently running an enemy's attack unsuppressed would be far worse than
    /// the bug this fixes: the enemy would deal its full damage AND keep the Block. 逆转 falls back
    /// to replacing the move outright when this is false.</para>
    /// </summary>
    public static bool IsLive { get; private set; }

    /// <summary>
    /// Makes <paramref name="creature"/> deal 0 damage until the returned scope is disposed.
    /// Always use it with <c>using</c> so an exception inside the move cannot leave it silenced.
    /// </summary>
    public static IDisposable Suppress(Creature creature) => new Scope(creature);

    /// <summary>Stale entries can't match a new combat's creatures, but don't accumulate them either.</summary>
    public override Task BeforeCombatStart()
    {
        IsLive = true;
        Silenced.Clear();
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return dealer != null && Silenced.Contains(dealer) ? 0m : 1m;
    }

    private sealed class Scope : IDisposable
    {
        private readonly Creature _creature;
        private readonly bool _owned;

        internal Scope(Creature creature)
        {
            _creature = creature;
            _owned = Silenced.Add(creature);
        }

        public void Dispose()
        {
            if (_owned)
            {
                Silenced.Remove(_creature);
            }
        }
    }
}
