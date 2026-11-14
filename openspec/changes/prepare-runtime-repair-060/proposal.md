## Why

The supplied runtime repair brings the approved preview particle fire into the real game renderer. The author wants these repairs included with the current visual refresh and confirmed gameplay fixes in a 0.6.0 Workshop-ready release.

## What Changes

- Verify and selectively merge the supplied particle controller, orbit changes, shaders, and opt-in native visual probe.
- Preserve current character-selection artwork, confirmed card behavior, content IDs, localization, and unrelated worktree changes.
- Generate release-surface differences against the last published 0.5.8 baseline and matching release commit/tag; write bilingual player-facing net-change notes.
- Set runtime version to 0.6.0, align Workshop notes, and export a new immutable release baseline.
- Build/install DLL and PCK, synchronize the local Workshop payload, verify actual runtime rendering, and finalize the code-only release under the project workflow.

## Capabilities

### New Capabilities

- `runtime-particle-release`: Verified native particle repair and reproducible 0.6.0 release preparation.

### Modified Capabilities

None.

## Impact

Orbit/particle renderer and optional probe, two local shader resources, release metadata/changelog/baseline, and local installed/Workshop artifacts. Existing art backups remain intact. Workshop uploading is a separate user action.
