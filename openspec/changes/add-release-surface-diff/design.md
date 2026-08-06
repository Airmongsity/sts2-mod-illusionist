## Context

The project currently reconstructs release notes from development history and memory. Intermediate card experiments are intentionally preserved in Git/OpenSpec, but those iterations must not appear in user-facing notes. The repository is Windows-first, already uses PowerShell for supported build/release workflows, and cannot require the game to boot merely to prepare notes.

## Goals / Non-Goals

**Goals:**

- Produce a deterministic, machine-readable public-content snapshot keyed by registered stable ID.
- Detect only the net difference between a published snapshot and a current snapshot.
- Limit human review to changed entities and explicit behavioral notes.
- Capture card-facing fields, localization, and a whitespace/comment-insensitive implementation fingerprint.
- Work from a clean checkout with PowerShell, Git, and the existing source tree.

**Non-Goals:**

- Infer a perfect natural-language explanation for arbitrary C# behavior.
- Replace OpenSpec, Git history, or human review of release wording.
- Load art, boot Slay the Spire 2, or change runtime MOD behavior.
- Publish release notes or mutate a GitHub/Steam release automatically.

## Decisions

### Use a source-derived public surface

The exporter will inspect registered C# types and localization JSON without loading game assemblies. Cards will use `StableEntryStem`; other registered content will use the deterministic auto-registration stem derived from the registered class name. This avoids runtime/game-version dependencies and makes the tool usable in release automation.

Runtime reflection was considered, but it would require loading the game, RitsuLib, Godot, and MOD assemblies in a compatible process. That is unnecessarily fragile for release-note preparation.

### Combine structured public fields with a semantic fingerprint

The snapshot will include structured card constructor fields, dynamic-variable declarations, upgrade code, localized strings, and selected public declarations. It will also include a class-body fingerprint after comments and insignificant whitespace are removed. Structured fields yield readable before/after details; the fingerprint flags behavior changes that the extractor cannot model.

### Compare snapshots by stable ID

Entity keys will be `<kind>:<stable-id>`. Later development changes overwrite the same current state naturally. The comparator reports only added, removed, or final changed entities. A change reverted to its baseline state produces no entry.

### Keep explicit behavioral notes in Git

Commit-message lines beginning with `Release-Note:` will be collected between the base Git reference and `HEAD`. These are reserved for cross-cutting behavior, bug fixes, and presentation changes that public metadata cannot explain. They are deduplicated in first-seen order.

### Treat generated Markdown as a draft

The comparator will emit JSON for automation and Markdown for human editing. It will not modify permanent release history. Final release wording remains a deliberate release task.

## Risks / Trade-offs

- [Source parsing cannot understand every C# construct] → Preserve normalized public expressions and a semantic fingerprint; mark otherwise unexplained changes for review.
- [An implementation-only refactor can change the fingerprint] → Report it as a review candidate, not as an automatic player-facing claim; the reviewer can omit it.
- [Shared helper changes may affect multiple entities without touching their files] → Require a `Release-Note:` trailer for cross-cutting behavior and bug fixes.
- [Localization keys may be missing or intentionally internal] → Export empty localization maps without failing; missing localized content remains visible in the snapshot.
- [Generated ordering can cause noisy diffs] → Sort entities, languages, localization keys, and changed fields deterministically.

## Migration Plan

1. Add the exporter/comparator and its self-test.
2. Export the current v0.5.3 surface as the initial published baseline.
3. Verify a baseline-to-baseline comparison is empty.
4. Apply a small card balance change and verify exactly that stable ID is reported.
5. At each future release, generate notes against the tracked baseline, then replace the baseline only after the release contents are final.

## Open Questions

None for the first implementation. Additional parsers can be added when an unsupported public field first appears in a real release diff.
