## Context

The migration ZIP contains editable runtime resources, a source snapshot, built reference artifacts, Spine converters, and visual probes. Its source snapshot shares the checkout's published Git baseline but has separate uncommitted changes. The live checkout already includes user-approved finisher changes and runtime fixes that must not be overwritten. The installed game reports 0.113.0; the installed Workshop RitsuLib 0.6.12 includes a 0.113.0 runtime variant.

## Goals / Non-Goals

**Goals:**

- Back up existing assets and their loading/build code before overwriting anything.
- Adopt the supplied current runtime artwork and necessary visual support code.
- Preserve the exact existing character-selection artwork and configuration.
- Build against an appropriate RitsuLib/game API pair and verify installed resources.

**Non-Goals:**

- Card balance, content ID, localization wording, or unrelated gameplay changes.
- Replacing author-maintained design documents with package snapshots.
- Importing historical browser studies as active game assets, or treating bundled DLL/PCK files as the rebuilt deliverable.
- Release versioning, Git staging/pushing, or Workshop uploading.

## Decisions

### Back up raw resources together with the code needed to restore their use

The local backup contains all current art and scenes, current scripts, project configuration, manifest, and build/PCK helpers at original repository-relative paths. All package files are extracted to an ignored staging directory and verified against the supplied SHA-256 manifest before use. This is preferable to a loose copy whose completeness and restoration paths are unclear.

### Exclude character-selection assets explicitly

Protect `char_select_bg_illusionist.tscn`, all `avatar-m` variants and companions, and all `illusionist_background` variants and companions. Keep the live character-selection profile paths intact. The in-run `avatar-s` is allowed to change. Validate protected source hashes against the backup after import and packaging.

### Merge only visual source and necessary API adaptations

Review normalized text differences rather than copying the code snapshot wholesale. Import the visual body/orbit controllers, rest pose, fake-merchant support, and optional probes; merge their registration into the live initializer. Preserve the live transmutation fixes, finisher implementation, and localization. API compilation errors are resolved against the installed 0.113.0 assembly, not by restoring older snapshot gameplay files.

### Keep the supported build pipeline authoritative

Add the new motion Spine atlas and texture directories to packing, retain raw files required by bitmap loaders as well as imported resources required by ResourceLoader, and use the supplied fixed Spine converter when needed. The build must produce and install DLL, PCK, and manifest and synchronize the local Workshop payload.

## Risks / Trade-offs

- [New motion or context textures are copied but omitted from the PCK] → Explicitly import and pack their directories/atlases, then load representative packed resources.
- [A package source snapshot reverts live fixes] → Apply a reviewed visual-file allowlist and local patch hunks, and inspect the final diff.
- [Character-selection assets share the art root with allowed assets] → Exclude by exact filename families and verify hashes against the backup.
- [The game update changes reflection targets or hook signatures] → Compile against 0.113.0, inspect affected native members, and run optional visual probes or startup checks.
- [A converter rewrites a packaged skeleton differently] → Verify conversion succeeds, inspect runtime loading, and retain the pre-migration backup.

## Migration Plan

1. Create the local ZIP backup and checksum-verify the migration staging files.
2. Copy permitted runtime resources and merge visual/build support.
3. Adapt only required native API/dependency integration for 0.113.0.
4. Run the supported full build-and-install workflow and inspect packed/runtime results.
5. Confirm protected assets match the backup and installed/Workshop payload hashes match the build.

Rollback restores original-relative files from the backup and rebuilds through the same supported script. Newly introduced visual files can be identified from the package/source allowlist and migration verification record.

## Open Questions

None; runtime/browser-only distinctions are documented in the package, and the user explicitly excluded the character-selection page.
