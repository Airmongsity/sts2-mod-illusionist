## Why

Slay the Spire 2 is still changing frequently, while mod development and LLM-assisted analysis need an accurate, repeatable view of the current base game's rules rather than manually maintained notes or potentially outdated wiki pages. A reusable extractor can turn each installed game update into stable machine-readable data and readable reference documents.

## What Changes

- Add a reusable Windows pipeline that locates an installed Slay the Spire 2 build, records its version and inputs, decompiles the current managed game assembly, and reads English localization from the game PCK.
- Extract cards, relics, potions, monsters, powers, rooms, and ancients into versioned JSON and Markdown outputs.
- Combine localized titles and descriptions with code-derived mechanics, including base/upgraded card values and monster intent state machines with ordered move effects and transition rules.
- Add validation, diagnostics, deterministic output, and version-to-version diff support so incomplete extraction remains visible instead of silently producing misleading facts.
- Document the pipeline, its dependencies, supported constructs, limitations, and update workflow for reuse by this and future mod projects.

## Capabilities

### New Capabilities

- `base-game-content-extraction`: Reproducible extraction and validation of localized base-game content definitions, gameplay mechanics, and monster state machines from an installed game build.

### Modified Capabilities

None.

## Impact

- Adds standalone tooling and generated-data conventions at the project root without changing Illusionist runtime behavior.
- Reads the installed game's managed assembly and PCK; it does not modify the game installation.
- Requires a compatible .NET SDK, Godot executable for PCK access, and a pinned managed decompiler/parser dependency.
- Generated snapshots may be large and will be kept separate from hand-maintained `game-facts.md` and source-controlled according to explicit repository rules.
