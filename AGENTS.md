# Illusionist Project Instructions

## Required Context

- Before evaluating or changing card balance, read `next-mirror.md`.
- Treat the latest user-confirmed design as authoritative over older OpenSpec change artifacts and historical notes.
- When diagnosing runtime behavior, inspect the latest relevant files under `logs/` before proposing a fix.

## Change Discipline

- Preserve user-provided text enclosed in `【】` verbatim. Do not paraphrase, normalize punctuation, or add omitted wording.
- Change only the fields explicitly requested. A numeric or similarly local adjustment must not replace or rewrite an otherwise unchanged description.
- Preserve registered card, power, and content stable IDs unless the user explicitly requests an identity change.
- Keep English and Simplified Chinese localization behaviorally consistent while preserving confirmed Chinese wording exactly.
- Do not broaden a balance adjustment into an unrelated redesign.
- Preserve unrelated worktree changes.

## Design and Specifications

- Use `next-mirror.md` for the character's current systems, card roles, and balance philosophy.
- Use OpenSpec for scoped, non-trivial changes that benefit from an explicit proposal, design, requirements, and task list.
- Treat completed OpenSpec changes as historical implementation records. They do not override later decisions or current source-of-truth documentation.
- Keep release history separate from current design rules where practical.

## Build and Verification

- Whenever compilation or packaging is required, use `sts2_illusionist\build-illusionist-windows.ps1` as the supported build-and-install workflow; do not treat a standalone `dotnet build` or Godot pack command as the final build.
- After completing each edit batch, run `sts2_illusionist\build-illusionist-windows.ps1` without `-SkipInstallCopy`.
- A change is not complete until the script builds the DLL and PCK and installs them into the configured Slay the Spire 2 mod directory.
- Keep the build script and its PCK/import helpers working as content evolves. When adding a new resource category or loading path (including affliction assets), verify that the script imports, packs, and installs it; update the script or packer in the same change if it does not.
- Report build warnings or errors and verify relevant localization, generated assets, or installed artifacts in proportion to the change.

## Versioning

- `sts2_illusionist\mod_manifest.json` is the runtime MOD version presented to the game.
- When bumping a release version, keep the manifest, the current version heading in `next-mirror.md`, and `illusionist-workshop\workshop.json` release notes aligned.
