using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using Illusionist.Scripts.Cards;
using Illusionist.Scripts.Relics;

namespace Illusionist.Scripts.Characters;

/// <summary>
/// 幻术师 (Illusionist) — a STANDALONE character (id: ILLUSIONIST), auto-discovered into
/// <c>ModelDb.AllCharacters</c> so it shows up in the character-select screen on its own, instead of
/// reskinning the Necrobinder slot.
///
/// MINIMAL STUB: it reuses Necrobinder's pools, starting deck/relic, animations and ART so we can
/// confirm the standalone character appears and is playable before migrating to dedicated pools.
/// All of Necrobinder's ID-derived asset paths (creature visuals, icons, energy counter, select bg,
/// …) are reused via <see cref="Patches.CharacterAssetRedirectPatch"/>, which rewrites this
/// character's "illusionist" asset paths to "necrobinder".
/// </summary>
public sealed class Illusionist : CharacterModel
{
    public override Color NameColor => new Color("4FC3F7"); // illusion cyan

    public override CharacterGender Gender => CharacterGender.Feminine;

    // No unlock requirement — available from the start.
    protected override CharacterModel? UnlocksAfterRunAs => null;

    public override int StartingHp => 66;

    public override int StartingGold => 99;

    // Dedicated Illusionist pools (decoupled from Necrobinder).
    public override CardPoolModel CardPool => ModelDb.CardPool<IllusionistCardPool>();

    public override RelicPoolModel RelicPool => ModelDb.RelicPool<IllusionistRelicPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<IllusionistPotionPool>();

    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        ModelDb.Card<StrikeNecrobinder>(),
        ModelDb.Card<StrikeNecrobinder>(),
        ModelDb.Card<StrikeNecrobinder>(),
        ModelDb.Card<StrikeNecrobinder>(),
        ModelDb.Card<DefendNecrobinder>(),
        ModelDb.Card<DefendNecrobinder>(),
        ModelDb.Card<DefendNecrobinder>(),
        ModelDb.Card<DefendNecrobinder>(),
        ModelDb.Card<Riposte>(),
        ModelDb.Card<Disrupt>(),
    };

    public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[]
    {
        ModelDb.Relic<HallucinatoryLamp>(),
    };

    public override float AttackAnimDelay => 0.15f;

    public override float CastAnimDelay => 0.4f;

    public override Color EnergyLabelOutlineColor => new Color("702D6FFF");

    public override List<string> GetArchitectAttackVfx() => new List<string>
    {
        "vfx/vfx_thrash",
        "vfx/vfx_heavy_blunt",
        "vfx/vfx_attack_slash",
        "vfx/vfx_bloody_impact",
    };

    // Mirror Necrobinder's animator (we reuse its creature visuals/spine via the asset redirect).
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
