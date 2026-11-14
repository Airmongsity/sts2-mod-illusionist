# Verification — 2026-11-14

## Repair scope and recovery

- Verified all 28 files against `illusionist-runtime-repair-20261114.zip`'s SHA-256/length manifest.
- Backed up 226 current source/localization/configuration/baseline/selection files to `.build_output/illusionist-before-runtime-repair-060.zip`, SHA-256 `78d7c2a834292466b2c56953926649fb66d2954844afd224d745ab1bf34fe621`.
- Replaced only the orbit implementation; added `IllusionistParticleFire`, `ParticleRuntimeProbe`, the initializer's new probe call, and the two supplied shaders/sidecars. The old archive's prebuilt DLL/manifest/full patch were not copied over current code. Its PCK delta targets the original migration archive and was not applied to our independently built PCK; a source rebuild preserves current localization and selection resources.
- All 222 pre-existing files outside the repair/release allowlist remain byte-identical to their before-repair hashes, including confirmed card scripts/localization, earlier baselines, `next-mirror.md`, `game-facts.md`, and all four protected character-selection source files.
- The original complete art backup remains at `art-backups/illusionist-before-art-migration-20261114-004804.zip`.

## Release baseline and notes

- Installed Steam Workshop item `3750819223` reports version 0.5.8. Tracked `release-baselines/v0.5.8.json` belongs to existing release commit `9e7665fa60f53d8f7afe6bc4924580046ccfb374`.
- Local and remote lacked `v0.5.8`; restored that missing local tag at the existing commit, without changing older content or tags.
- `release_surface.ps1 selftest` passed all checks.
- Diff used the immutable 0.5.8 baseline with `-BaseRef v0.5.8`: 0 additions, 0 removals, 12 net changes. Reviewed raw implementation-only entries and actual source for the five finishers, since computed-variable literals are not all shown individually by the draft.
- Wrote `release-notes/v0.6.0.md` and aligned bilingual `workshop.json` notes. Excluded the 0.5.8 Reversal/energy-orb fixes and temporary unreleased implementation mistakes. Gameplay localization text was not rewritten during this task.
- Exported `release-baselines/v0.6.0.json` after version/source decisions: 143 registered entities, unchanged stable-ID set. A second comparison against the live tree reports zero differences.
- Runtime version 0.6.0, minimum game 0.113.0, minimum RitsuLib 0.6.12. Workshop dependency ID remains unchanged.

## Build and packed artifacts

Completed the supported full build/install workflow, without `-SkipInstallCopy`:

```powershell
$env:RitsuLibAutoCopy = 'false'
& .\sts2_illusionist\build-illusionist-windows.ps1 -NoRitsuLibUpdate
```

- C# compilation: 0 warnings, 0 errors. All three Spine conversions succeeded. The converter's existing 13 motion-mesh hull-length warnings were automatically normalized; the particle repair did not change skeleton source assets.
- Godot 4.5.1 built 649 PCK entries. Every entry passed MD5 validation; every runtime source file matches packed bytes; imported texture dependency chains resolve; both supplied shader files are present byte-for-byte.
- DLL, PCK, and manifest match the game mod installation and `illusionist-workshop/content/` by SHA-256:

| Artifact | SHA-256 |
| --- | --- |
| DLL | `d87d065b94ab76b83fee07003d749cc555cf857ab96e7d05c54aff12c7c4c8b8` |
| PCK | `e9848b563e6dd453e1f259b6a0654d911b02d1ebab138f1c3e60ca362faee220` |
| manifest | `3f4d0c601adc65ac511502f7d49f53a19e04e0dcd573b31dcf6359c309594042` |

- Local evidence: `.build_output/runtime-repair-060-build.log`, `.build_output/runtime-repair-060-validation/artifact-check.json` (11 checks passed), and the ignored standard-library verification helper `.build_output/verify_runtime_repair_060.py`.

## Installed native particle probe

Ran `--illusionist-particle-probe` through the installed real game loader using OpenGL compatibility rendering at 30 FPS. It uses an isolated viewport and does not start a run; existing mods remain enabled. Exit code 0; `passed: true`.

- Renderer: `html-particle-density-v1`; 149 particle primitives.
- Idle frame at 0 seconds equals 16 seconds byte-for-byte.
- Guard history copies accumulate `0, 1, 2, 3, 4`, never sampling before activation.
- Power speed peaks at 2.9702× and recovers to 1.0×.
- Directed attack capture retains 0.20-second stagger and renders the new impact sparks.
- All 20 Illusionist patches applied, with zero ignored/failed; all 161 registration operations succeeded. No particle shader compilation or Illusionist visual-hook failures appeared.
- Inspected actual idle, guard, and attack PNG captures. Logs/frames/results are under `.build_output/runtime-repair-060-validation/particle/`.
- Existing game teardown RID/resource-leak diagnostics remain. The pre-repair user log's disposed-sprite exceptions originate in `BloodySpire.FluidPresenter`, outside this repair. This is focused visual/API validation, not a full-run or multiplayer gameplay regression.

## Git and upload handoff

Release staging uses an explicit code/localization/configuration/docs/baseline allowlist. Art, shader-resource sources, scenes, converters, built artifacts, ZIPs, logs, and unrelated experiments are excluded. The packed shaders/art are included in the local Workshop PCK.

Reviewed all 61 staged names and the staged diff, verified the index matched the tested worktree, and confirmed supplied controller/probe sources matched their reviewed packages. `git diff --cached --check` passed.

Release commit `84865ef02f1128e7d76dc6f10fa252b591a10f1b` and annotated `v0.6.0` were pushed successfully. The restored `v0.5.8` tag was also pushed at the existing published commit. Remote main and the peeled `v0.6.0` tag were verified against the release commit. A documentation-only follow-up records this completed verification; it does not change the tagged release payload.

Workshop uploading is left to the author. From `sts2-mod-uploader/`, run `.\ModUploader.exe upload -w ..\illusionist-workshop` when ready. No uploader was run by this task.
