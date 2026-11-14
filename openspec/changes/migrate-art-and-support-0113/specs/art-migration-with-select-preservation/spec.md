## ADDED Requirements

### Requirement: Existing artwork is recoverable
The migration SHALL create a local backup of existing art and scenes at original-relative paths before replacing resources, together with the visual loading and build configuration needed to restore them.

#### Scenario: Restore previous visual configuration
- **WHEN** the pre-migration backup is opened
- **THEN** it contains the previous art, scene, script, project, manifest, and build-helper files under their repository-relative names

### Requirement: Package integrity is verified
Every extracted file listed by the supplied package checksum manifest SHALL match its recorded byte length and SHA-256 before being used.

#### Scenario: Verified package staging
- **WHEN** the supplied migration ZIP has been extracted
- **THEN** all 1,149 manifest-listed files pass checksum verification

### Requirement: Character-selection artwork is preserved
The character-selection scene, all `avatar-m` variants, all `illusionist_background` variants, and the character-selection asset profile configuration SHALL retain their existing content and behavior.

#### Scenario: Character-selection resources after migration
- **WHEN** migration and import have completed
- **THEN** the protected source artwork and scene hashes equal those in the pre-migration backup and the character profile continues to load those paths

### Requirement: Other supplied runtime visual categories are active
The game SHALL use the supplied non-selection runtime card, power, relic, potion, UI, event, multiplayer, affliction, combat body, animation, rest, merchant, and shader resources, with the visual controllers required to display them.

#### Scenario: New combat and context resources load
- **WHEN** the installed mod loads its combat body, rest pose, merchant/event visuals, and representative card or icon resources
- **THEN** their referenced textures, Spine data, scenes, and shaders are available in the built PCK

### Requirement: Existing gameplay and content identities survive migration
The migration SHALL preserve live stable content IDs, user-confirmed card behavior, existing localization wording, and unrelated worktree changes. Compatibility adaptations SHALL be limited to matching the native API without redesigning effects.

#### Scenario: Merge art source support
- **WHEN** visual support from the snapshot is merged
- **THEN** live finisher changes and prior encyclopedia/transmutation fixes remain present

### Requirement: The supported workflow installs a 0.113.0-compatible build
The supported Windows script SHALL build the mod against the installed game and a compatible RitsuLib API, package every required imported/raw visual resource, install DLL/PCK/manifest, and synchronize the local Workshop payload.

#### Scenario: Complete build and installation
- **WHEN** the migration build succeeds
- **THEN** the configured game mod directory and Workshop content directory contain DLL, PCK, and manifest matching the generated artifacts
