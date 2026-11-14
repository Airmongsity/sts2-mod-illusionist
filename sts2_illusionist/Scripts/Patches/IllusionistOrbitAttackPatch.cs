using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Patching.Models;

namespace Illusionist.Scripts.Patches;

public sealed class IllusionistOrbitAttackPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<AttackCommand, object> Prepared = new();
    private static readonly FieldInfo BeforeDamageField = typeof(AttackCommand).GetField("_beforeDamage", BindingFlags.NonPublic | BindingFlags.Instance)!;
    public static string PatchId => "illusionist_orbit_target_fx";
    public static string Description => "Vanish Illusionist flames before damage and reappear on impact";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => new[] { new ModPatchTarget(typeof(AttackCommand), nameof(AttackCommand.Execute)) };
    private static void Prefix(AttackCommand __instance)
    {
        IllusionistOrbitFx? fx = IllusionistOrbitFx.For(__instance.Attacker);
        if (fx == null) return;
        Configure(__instance, fx);
    }

    internal static void Configure(AttackCommand __instance, IllusionistOrbitFx fx)
    {
        if (Prepared.TryGetValue(__instance, out _)) return;
        Prepared.Add(__instance, new object());
        // These placeholders otherwise cover the teleport heads on every card hit.
        // Keep hit audio and any card-specific/custom effect callbacks intact.
        if (__instance.HitVfx is "vfx/vfx_attack_slash" or "vfx/vfx_attack_blunt")
            __instance.WithHitFx(null, __instance.HitSfx, __instance.TmpHitSfx);
        var targets = new List<Creature>();
        var previous = (Func<Task>?)BeforeDamageField.GetValue(__instance);
        __instance.WithAttackerFx(() =>
        {
            try { if (GodotObject.IsInstanceValid(fx)) fx.PrepareAttack(); }
            catch (Exception ex) { Log.Error($"[illusionist] Orbit target FX failed: {ex}"); }
            return null;
        });
        __instance.WithHitVfxNode(target =>
        {
            targets.Add(target);
            return null;
        });
        __instance.BeforeDamage(async () =>
        {
            // Preserve card-specific callbacks and the native target/RNG pipeline.
            if (previous != null) await previous();
            Creature[] resolved = targets.ToArray();
            targets.Clear();
            try { if (GodotObject.IsInstanceValid(fx)) await fx.TeleportAttack(resolved); }
            catch (Exception ex) { Log.Error($"[illusionist] Orbit teleport FX failed: {ex}"); }
        });
    }
}
