## ADDED Requirements

### Requirement: Deterministic public surface export
The release tool SHALL export a deterministic JSON snapshot of registered cards, relics, potions, powers, and afflictions without loading the game runtime or art assets.

#### Scenario: Repeated export without source changes
- **WHEN** the exporter runs twice against the same source tree and manifest version
- **THEN** both output files contain byte-equivalent public entity data and ordering

### Requirement: Stable identity and localized public fields
The snapshot SHALL key each entity by content kind and stable ID and SHALL include available English and Simplified Chinese localized fields.

#### Scenario: Card display name changes while identity is preserved
- **WHEN** a card keeps its registered stable stem but changes its localized title
- **THEN** the comparator reports one changed card under the original stable ID rather than an added and removed pair

### Requirement: Net release comparison
The comparator SHALL report only added, removed, or final changed entity state between the published baseline and current snapshot.

#### Scenario: Multiple development revisions
- **WHEN** an entity is revised multiple times after the baseline
- **THEN** the generated result contains one comparison between the baseline state and the final current state

#### Scenario: Reverted development revision
- **WHEN** an entity's final current state equals its baseline state
- **THEN** the entity is absent from the generated release difference

### Requirement: Focused human-readable draft
The comparator SHALL generate a Markdown draft that groups changed content by kind and presents structured before/after fields where available.

#### Scenario: Numeric card balance change
- **WHEN** a card's base dynamic-variable declaration changes
- **THEN** the draft identifies that card and shows the old and new public value expressions

### Requirement: Explicit behavioral release notes
The release tool SHALL collect deduplicated `Release-Note:` lines from Git commits after a supplied base reference.

#### Scenario: Cross-cutting bug fix trailer
- **WHEN** a commit between the base reference and `HEAD` contains `Release-Note: 修复镜像结算错误`
- **THEN** the Markdown and JSON outputs include `修复镜像结算错误` exactly once as an explicit note

### Requirement: Published baseline lifecycle
The repository SHALL contain a versioned v0.5.3 public-surface baseline and SHALL not mutate it during ordinary development exports.

#### Scenario: Development comparison
- **WHEN** a developer generates a current snapshot or draft against v0.5.3
- **THEN** the tracked v0.5.3 baseline remains unchanged

### Requirement: Failure visibility
The release tool MUST fail with a non-zero exit status when required inputs are missing, malformed, duplicated by stable key, or refer to an invalid Git base.

#### Scenario: Duplicate stable ID
- **WHEN** two registered entities resolve to the same kind and stable ID
- **THEN** snapshot generation stops and identifies the duplicate key
