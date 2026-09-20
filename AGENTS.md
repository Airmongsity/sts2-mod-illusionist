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
- When bumping a release version, keep the manifest and `illusionist-workshop\workshop.json` release notes aligned.
- `next-mirror.md` is the character's design and balance document, maintained by the author. Assistants read it and never edit it; it is not a changelog and carries no per-release section.

## Release Workflow

1. Identify the last published tag and its immutable `release-baselines/vX.Y.Z.json`; do not overwrite an older baseline.
2. Run `sts2_illusionist\tools\release_surface.ps1 selftest`, then run `diff` against that baseline with the matching tag as `-BaseRef`. Release notes describe only the net difference from the last published version; do not list intermediate development iterations that were later replaced or reverted.
3. Review the generated draft. Rewrite raw expressions as player-facing changes and add cross-cutting fixes or compatibility changes that the registered-content snapshot cannot infer. During development, record such changes with `Release-Note:` commit trailers when practical.
4. Bump `sts2_illusionist\mod_manifest.json` and update `illusionist-workshop\workshop.json` `changeNote`. Keep English and Simplified Chinese release information behaviorally consistent. Do not edit `next-mirror.md`.
5. Export `release-baselines/v<NEW_VERSION>.json` only after the release contents and version are final, and commit that new baseline with the release.
6. Run `sts2_illusionist\build-illusionist-windows.ps1` without `-SkipInstallCopy`. Confirm the DLL, PCK, and manifest were installed and that `illusionist-workshop/content/` contains the synchronized Workshop payload.
7. When a GitHub release archive is required, package the built DLL, PCK, and manifest under a top-level `illusionist/` directory. The archive includes packed art in the PCK even though source art is excluded from Git.
8. Stage an explicit allowlist of code, localization, configuration, documentation, and release-baseline files. Never use a blanket stage for a release. Do not commit art/resource sources (including PNG, JPG/JPEG, WEBP, GIF, SVG, SKEL, ATLAS, SPINE, PSD, or Godot import sidecars) or `.tscn` files.
9. Inspect `git diff --cached --name-only` and the staged diff before committing. Commit and push the release commit, then create/push the matching `vX.Y.Z` tag when the release is being finalized.
10. Do not run `ModUploader.exe` unless the user asks Codex to upload. Normally hand off the prepared Workshop payload so the user can run `sts2-mod-uploader\ModUploader.exe upload -w ..\illusionist-workshop` manually.
