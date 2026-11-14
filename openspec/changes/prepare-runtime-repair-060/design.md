## Context

The live tree contains the approved art migration and unreleased finisher/transmutation work. The repair ZIP contains a full historical patch and prebuilt artifacts in addition to the actual particle changes. Installed Steam Workshop Illusionist is 0.5.8, with an immutable baseline at the existing 0.5.8 release commit; neither local nor remote has its tag.

## Goals / Non-Goals

**Goals:** Activate the supplied native particle renderer; retain current behavior and selection art; prepare coherent bilingual 0.6.0 notes, baseline, installed artifacts, and code-only release history.

**Non-Goals:** Card redesign, unrelated tooling changes, publishing art sources, or running the Workshop uploader.

## Decisions

- Checksum-verify and back up the few overwritten source/configuration files before merging. Adopt `IllusionistOrbitFx`, add `IllusionistParticleFire` and `ParticleRuntimeProbe`, and merge only the new probe call in `Entry`. This avoids stale comments/metadata and unrelated historical patches.
- Copy the two shader resources and sidecars locally and rebuild the whole current project. The bundled PCK delta is a reference for the original archive, not a patch against our independently rebuilt/current-localization PCK.
- Restore a local `v0.5.8` tag at the existing release commit `9e7665f`, supported by the installed Steam Workshop manifest and tracked baseline. Never overwrite prior baselines or existing tags.
- Run release-surface selftests and diff from that matching tag/baseline. Rewrite the final net finisher differences in player language and add visual/compatibility/transmutation changes that static content extraction cannot detect. Keep confirmed Chinese strings untouched in gameplay localization.
- Export the 0.6.0 baseline only after final source/version decisions. Stage an explicit list of release code/localization/docs/config/baseline files, inspect the index, then follow the project release commit/push/tag workflow. Leave unrelated game-facts, experiments, logs, art, and scenes unstaged.
- Verify through the installed native particle probe. The previous motion probe contains renderer-specific expectations for the superseded implementation and is historical evidence rather than the new particle acceptance test.

## Risks / Trade-offs

- [Shaders missing from the built PCK] → Verify both source bytes in the pack and actual native material loads.
- [Repair source replaces live gameplay] → Keep a narrow merge allowlist, compare unaffected scripts/localization hashes, and review the final release diff.
- [Particle density changes fail in the game renderer] → Run the supplied opt-in probe and inspect rendered PNGs and logs, including loop/guard/power checks.
- [Changelog mixes already published and development changes] → Compare against the verified 0.5.8 baseline/tag and publish net results only.
- [Unrelated dirty files are committed] → Stage exact paths and audit staged names/diff before committing.

## Migration Plan

Verify ZIP and back up overwritten files; merge particle code/shaders; prepare 0.6.0 release metadata, notes, and baseline; full supported build/install; native probe and artifact/hash checks; code-only release finalization. Roll back renderer source/configuration from the local backup and rebuild, retaining original selection artwork and earlier art backup.
