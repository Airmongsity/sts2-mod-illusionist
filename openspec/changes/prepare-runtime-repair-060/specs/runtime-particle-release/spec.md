## ADDED Requirements

### Requirement: Repair integration is verified and scoped
The repair SHALL verify every file in the ZIP manifest and merge only the native particle renderer, its shaders, and its opt-in probe. Existing content IDs, confirmed gameplay/localization, character-selection artwork, and unrelated worktree files MUST remain intact.

#### Scenario: Apply runtime repair
- **WHEN** the repair is merged into the current visual update
- **THEN** its renderer/shaders are present while unrelated scripts and selection-art hashes remain unchanged

### Requirement: Native particles match approved runtime behavior
The installed game SHALL render the supplied particle density model, periodic idle movement, causal guard afterimages, temporary power acceleration, and directed attack presentation.

#### Scenario: Run installed native probe
- **WHEN** the explicit particle probe runs through the installed mod loader
- **THEN** it reports the new renderer, equal first/last idle frames, guard copy counts 0 through 4 without pre-activation samples, more than 90 particle primitives, and power speed returning to 1

### Requirement: Release notes describe net published differences
The 0.6.0 runtime manifest, bilingual Workshop change note, changelog, and newly exported immutable baseline SHALL agree. Notes MUST describe only net changes from the published 0.5.8 baseline and matching release reference, including cross-cutting visual and compatibility fixes.

#### Scenario: Generate release materials
- **WHEN** release-surface selftests and baseline comparison complete
- **THEN** reviewed notes reflect the final release state and previous baselines are unchanged

### Requirement: Release payload is built, installed, and isolated from source art
The supported Windows build SHALL produce/install DLL, PCK, and manifest and synchronize the Workshop payload. Release Git staging MUST use an explicit allowlist and exclude art/resource sources, scenes, binaries, and unrelated work.

#### Scenario: Finalize release preparation
- **WHEN** the 0.6.0 release is prepared
- **THEN** source/build/game/Workshop artifacts match, staged code has been reviewed, and the Workshop upload remains a separate user action
