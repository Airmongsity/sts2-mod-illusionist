using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;

namespace Illusionist.Scripts.Characters;

/// <summary>
/// 幻术师 (Illusionist) — a STANDALONE character built on RitsuLib's
/// <see cref="ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}"/>. Any asset we don't own falls
/// back per-field to Necrobinder's via <see cref="PlaceholderCharacterId"/> (this replaced the old
/// CharacterAssetRedirectPatch / roster / epoch-guard patch family). Starting deck and relic are
/// declared on the card/relic classes with <c>[RegisterCharacterStarterCard/Relic]</c>.
/// </summary>
[RegisterCharacter]
public sealed class Illusionist : ModCharacterTemplate<IllusionistCardPool, IllusionistRelicPool, IllusionistPotionPool>
{
    public override Color NameColor => new Color("4FC3F7"); // illusion cyan

    public override CharacterGender Gender => CharacterGender.Feminine;

    public override int StartingHp => 66;

    public override int StartingGold => 99;

    public override float AttackAnimDelay => 0.15f;

    public override float CastAnimDelay => 0.4f;

    public override Color EnergyLabelOutlineColor => new Color("702D6FFF");

    // No timeline/epoch story — RitsuLib then guards the per-character epoch/unlock code paths that
    // the base game hardcodes for its five characters (was CharacterEpochGuardPatch).
    public override bool RequiresEpochAndTimeline => false;

    // Everything not listed in AssetProfile falls back to Necrobinder's assets.
    public override string? PlaceholderCharacterId => "necrobinder";

    public override CharacterAssetProfile AssetProfile => new(
        Ui: new CharacterUiAssetSet(
            IconTexturePath: "res://illusionist/art/avatar-s.png",
            IconPath: "res://illusionist/scenes/illusionist_icon.tscn",
            CharacterSelectBgPath: "res://illusionist/scenes/char_select_bg_illusionist.tscn",
            CharacterSelectIconPath: "res://illusionist/art/avatar-m.png"));

    public override List<string> GetArchitectAttackVfx() => new List<string>
    {
        "vfx/vfx_thrash",
        "vfx/vfx_heavy_blunt",
        "vfx/vfx_attack_slash",
        "vfx/vfx_bloody_impact",
    };

    /// <summary>
    /// Combat body: instantiate the borrowed Necrobinder creature-visuals scene (so every marker the
    /// combat system needs — %Bounds, %IntentPos, %CenterPos — stays correct), then swap its Spine
    /// skeleton for ours once the scene is ready (was IllusionistCombatBodyPatch). Falls back to the
    /// flat static image if Spine is unavailable, and to the borrowed body if that fails too.
    /// </summary>
    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        string? scenePath = ResolvedAssetProfile.Scenes?.VisualsPath;
        if (scenePath == null)
        {
            return null;
        }

        if (ResourceLoader.Load<PackedScene>(scenePath)?.Instantiate() is not NCreatureVisuals visuals)
        {
            return null;
        }

        if (!SpineBody.SwapOnReady(visuals, alpha: 1f))
        {
            ImageTexture? texture = IllusionistArt.CombatBody;
            if (texture != null)
            {
                StaticBodyOverlay.ApplyOnReady(visuals, texture, 300f, 0f, alpha: 1f, "IllusionistStaticBody");
            }
        }

        return visuals;
    }

    // Mirror Necrobinder's animator (we reuse its creature visuals/spine via the placeholder).
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new AnimState("idle_loop", isLooping: true);
        AnimState cast = new AnimState("cast");
        AnimState attack = new AnimState("attack");
        AnimState hurt = new AnimState("hurt");
        AnimState die = new AnimState("die");
        AnimState castMighty = new AnimState("cast_mighty");
        AnimState relaxed = new AnimState("relaxed_loop", isLooping: true);
        cast.NextState = idle;
        attack.NextState = idle;
        hurt.NextState = idle;
        castMighty.NextState = idle;
        relaxed.AddBranch("Idle", idle);
        CreatureAnimator animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("summonTrigger", cast);
        animator.AddAnyState("Cast", castMighty);
        animator.AddAnyState("Relaxed", relaxed);
        animator.AddAnyState("PowerUp", castMighty);
        return animator;
    }
}
