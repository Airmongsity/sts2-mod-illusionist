## ADDED Requirements

### Requirement: Reproducible game input discovery
The pipeline SHALL accept an explicit Slay the Spire 2 installation directory, SHALL validate the managed assembly, PCK, and release metadata, and SHALL record release identity and SHA-256 hashes in the generated snapshot.

#### Scenario: Extract an installed game update
- **WHEN** the user runs the pipeline against a valid installation
- **THEN** the snapshot manifest identifies the game version, commit, assembly hash, PCK hash, extractor version, and selected English localization inputs

#### Scenario: Reject an incomplete installation
- **WHEN** a required game input is absent
- **THEN** the pipeline exits unsuccessfully and names the missing path without creating a successful snapshot manifest

### Requirement: Cached managed decompilation
The pipeline SHALL decompile the installed `sts2.dll` into a non-published work directory using a managed decompiler and SHALL reuse work only when its recorded assembly hash matches the current input.

#### Scenario: Re-run unchanged build
- **WHEN** a completed decompile cache exists for the current assembly hash
- **THEN** the pipeline reuses the cache unless the user explicitly requests a fresh decompile

#### Scenario: Game assembly changes
- **WHEN** the installed assembly hash differs from the cached hash
- **THEN** the pipeline creates or selects a new hash-keyed source work tree

### Requirement: English localization extraction
The pipeline SHALL mount the installed game PCK read-only and extract the English localization tables required by cards, relics, potions, monsters, powers, rooms/events, and ancients.

#### Scenario: Required localization table exists
- **WHEN** the PCK contains a configured English localization table
- **THEN** the pipeline copies its JSON content for semantic joining and records the resource path in the manifest

#### Scenario: Localization layout changes
- **WHEN** a required configured table cannot be read
- **THEN** the pipeline reports the missing resource path and marks affected content as unlocalized rather than silently inventing text

### Requirement: Required content category coverage
The analyzer SHALL emit records for all discovered base-game cards, relics, potions, monsters, powers, structural room types, and ancient event models, while distinguishing mocks, deprecated definitions, tokens, events, and other non-reward content through extracted flags or provenance.

#### Scenario: Analyze all content categories
- **WHEN** semantic extraction completes
- **THEN** separate JSON collections and a coverage summary exist for cards, relics, potions, monsters, powers, rooms, and ancients

#### Scenario: New subclass appears
- **WHEN** an update adds a subclass of a supported base model
- **THEN** it appears in the corresponding output or is named in diagnostics as an unclassified or failed record

### Requirement: Card upgrade representation
Card records SHALL combine English localization, constructor metadata, declarative properties, canonical dynamic variables, play behavior, and upgrade behavior. Numeric values SHALL retain separate base and upgraded forms and human-readable output SHALL use `base(upgraded)` when the value changes.

#### Scenario: Numeric card upgrade
- **WHEN** a card's cost changes from 2 to 1 and damage changes from 6 to 9
- **THEN** its normalized record contains both value pairs and its Markdown summary can render `2(1)c` and `Deal 6(9) damage`

#### Scenario: Non-numeric upgrade
- **WHEN** an upgrade changes a keyword, targeting rule, or other behavior
- **THEN** the upgrade method and changed expression remain present even if no numeric compact form applies

### Requirement: Behavioral extraction for models
Records for cards, relics, potions, powers, rooms, and ancients SHALL retain relevant override hooks as ordered statements and SHALL identify common game command invocations without requiring the game assembly to execute.

#### Scenario: Power reacts to a card play
- **WHEN** a power overrides card-play hooks
- **THEN** the output identifies the hook names, conditions, and ordered command expressions used by those hooks

#### Scenario: Ancient generates conditional options
- **WHEN** an ancient defines randomized or conditionally filtered options
- **THEN** the option-generation method and referenced rewards or conditions remain discoverable in the ancient record

### Requirement: Monster intent state-machine extraction
Monster records SHALL include localized identity, HP and difficulty expressions, move states, intent declarations, initial state, transition edges, random or conditional branches, weights, cooldowns, repeat restrictions, and the ordered behavior of each referenced move method.

#### Scenario: Random state with cooldowns
- **WHEN** a monster adds equal-weight branches with cooldown and repeat restrictions
- **THEN** the output distinguishes branch weight from cooldown and records how eligible branches are selected

#### Scenario: Conditional phase transition
- **WHEN** a monster selects a state based on HP, allies, powers, or prior moves
- **THEN** the output preserves the condition, destination state, evaluation order, and relevant external hook methods

#### Scenario: Ordered move effects
- **WHEN** a move deals damage and then applies a debuff
- **THEN** the move record lists those operations in execution order and retains their exact value expressions

### Requirement: Deterministic machine and LLM outputs
The pipeline SHALL emit stable JSON with provenance and concise categorized Markdown suitable for retrieval by LLMs. Re-running identical inputs with the same extractor SHALL not create semantic differences.

#### Scenario: Repeat extraction
- **WHEN** the same version and hashes are extracted twice
- **THEN** category JSON and Markdown content compare equal apart from explicitly non-semantic run metadata

### Requirement: Validation and diagnostics
The pipeline SHALL generate diagnostics covering discovered/emitted counts, localization joins, parser fallbacks, unresolved monster moves, and structural failures. A strict mode SHALL fail on structural coverage loss.

#### Scenario: Unsupported future construct
- **WHEN** the analyzer encounters a construct it cannot normalize
- **THEN** it preserves the source expression or statement and emits a diagnostic referencing the content ID and source location

#### Scenario: Strict coverage failure
- **WHEN** a discovered supported subclass has no emitted record or a referenced monster move method cannot be found
- **THEN** strict mode exits unsuccessfully after writing actionable diagnostics

### Requirement: Version snapshot comparison
The tooling SHALL compare two generated snapshots by category and stable model ID and SHALL report added, removed, and changed records without reporting cache paths or generation timestamps as gameplay changes.

#### Scenario: Compare consecutive game versions
- **WHEN** the user supplies two completed snapshot directories
- **THEN** the pipeline emits a machine-readable and readable diff grouped by category and stable ID

### Requirement: Safe read-only operation
The extraction pipeline SHALL not modify files inside the selected game installation and SHALL keep decompiled sources and transient localization copies outside published mod content.

#### Scenario: Complete extraction
- **WHEN** extraction succeeds or fails
- **THEN** all writes are confined to the configured work and output roots
