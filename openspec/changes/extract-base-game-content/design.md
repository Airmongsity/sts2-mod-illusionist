## Context

The installed Windows build exposes gameplay code in `data_sts2_windows_x86_64/sts2.dll`, release identity in `release_info.json`, and English localization in `SlayTheSpire2.pck`. ILSpy can reproduce a C# project from the managed assembly, but the resulting source is too large and unstable to serve directly as an LLM reference. The repository already keeps decompiled code out of Git, and generated facts must retain provenance without redistributing the raw source snapshot.

The extractor must tolerate frequent early-access updates. In particular, it must preserve expressions and method bodies when a new construct cannot yet be interpreted, surface unresolved items in diagnostics, and avoid treating an incomplete heuristic summary as authoritative.

## Goals / Non-Goals

**Goals:**

- Provide one supported PowerShell entry point for Windows that validates inputs, fingerprints the game build, decompiles the current assembly, extracts English localization, runs semantic extraction, validates coverage, and optionally compares snapshots.
- Produce deterministic JSON for tools and categorized Markdown for LLM context.
- Represent card base/upgraded values compactly, while labeling monster ascension scaling separately.
- Recover monster move states, intents, transitions, restrictions, conditions, and ordered move implementation statements.
- Preserve source provenance and raw code expressions alongside best-effort normalized facts.
- Make parser gaps measurable through a diagnostics file and a strict mode suitable for update checks.

**Non-Goals:**

- Execute the game assembly or mutate the game installation.
- Reproduce audiovisual assets or raw decompiled source in published outputs.
- Perfectly simulate runtime state, multiplayer choices, localization formatting, or arbitrary future C# code.
- Replace curated design analysis in `game-facts.md`; generated files are evidence feeding that analysis.

## Decisions

### Use a three-stage offline pipeline

The entry script will orchestrate: (1) ILSpy project decompilation into an ignored work directory, (2) a headless Godot script that mounts the game PCK and copies selected English localization tables, and (3) a .NET/Roslyn analyzer that emits normalized snapshots. Separating stages makes failures attributable and lets developers rerun parsing without repeating a large decompile.

Direct runtime reflection was rejected because model construction depends on initialized game registries, Godot resources, and runtime state. Regex-only parsing was rejected because nested lambdas, initializers, generic calls, and state-machine expressions require a real C# syntax tree.

### Parse syntax without compiling or executing game code

The analyzer will use Roslyn syntax trees over the decompiled project. It will resolve the local class inheritance graph, classify content from its base model, capture constructors/properties/fields/methods, and perform category-specific pattern extraction. Because it does not require a successful compilation, it remains useful when ILSpy emits code that references unavailable native or generated dependencies.

### Preserve both normalized facts and evidence

Every record will include stable model identity, localization, source location, declarative properties, canonical dynamic variables, and relevant method/statement representations. Category-specific normalized fields are additive. Unsupported expressions remain in `expression` or `body` fields and contribute diagnostics rather than disappearing.

For cards, numeric upgrades will be represented as base/upgraded pairs and rendered as `base(upgraded)` only when they differ. For monsters, difficulty thresholds and expressions remain explicitly labeled instead of using card-upgrade notation.

### Treat monster behavior as a graph plus ordered actions

The analyzer will identify `MoveState`, `RandomBranchState`, and `ConditionalBranchState` construction; move IDs, perform-method delegates, intent constructors, follow-up assignments, branch calls, repeat rules, cooldowns, weights, conditions, and initial return state. Each referenced move method will retain ordered executable statements and common command invocations. Special monster hooks outside `GenerateMoveStateMachine` are also retained so phase and death logic is discoverable.

### Version snapshots by release identity and content hash

The pipeline will read `release_info.json`, hash `sts2.dll` and the PCK, and write a manifest beside each snapshot. A work cache is keyed by assembly hash. Re-running the same inputs produces equivalent content files; a diff command compares records by category and stable ID while ignoring generation-time metadata.

### Validate coverage instead of silently guessing

Validation will compare discovered subclasses, emitted records, localization matches, and monster state references. Warnings identify missing tables, missing localization, unclassified content, unresolved move methods, and unsupported branch constructs. Strict mode fails for structural loss while ordinary mode completes with warnings for newly introduced patterns.

## Risks / Trade-offs

- **Game updates introduce new C# constructs** → Preserve raw expressions/statements, emit diagnostics, and add regression fixtures for each newly supported pattern.
- **Localization paths or PCK mounting change** → Probe required tables explicitly and fail with the exact missing `res://` path.
- **Decompilation is slow and consumes disk** → Cache by assembly SHA-256 and provide a switch to retain or remove the work tree.
- **Generated facts can be mistaken for final runtime values** → Label static expressions, ascension conditions, and unresolved runtime modifiers; keep provenance in every snapshot.
- **Generated output may contain too much implementation text** → Emit compact per-category Markdown, while detailed statements remain in JSON for targeted LLM retrieval.
- **Legal/repository concerns around decompiled source** → Keep work trees ignored and publish only derived facts, signatures, expressions, and concise behavioral statements.

## Migration Plan

1. Add the extractor and ignore its decompile/cache directories.
2. Run self-tests against small source fixtures and then the installed v0.111.0 build.
3. Review coverage diagnostics and current snapshot counts for every required category.
4. Use generated output as an input to future `game-facts.md` maintenance; do not overwrite curated prose automatically.

Rollback consists of removing the standalone tool and generated snapshot; no game or mod runtime files are changed.

## Open Questions

- Some future localization templates may require additional SmartFormat functions. These will initially remain as templates with resolved dynamic-variable tables and explicit diagnostics.
- If event rooms are later requested as first-class content, they can be added as a separate `events` category rather than conflating event definitions with the six structural room types.
