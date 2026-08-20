## 1. Pipeline shell and inputs

- [x] 1.1 Add the standalone extractor directory, documented prerequisites, ignored work/output conventions, and a versioned output schema
- [x] 1.2 Implement the PowerShell entry point with game-path validation, release metadata, hashes, cache selection, and safe read-only orchestration
- [x] 1.3 Implement headless Godot extraction of configured English localization tables from the game PCK

## 2. Semantic extraction

- [x] 2.1 Add the Roslyn analyzer project and common source, inheritance, model-ID, localization, expression, method, and provenance extraction
- [x] 2.2 Add card extraction with constructor metadata, dynamic variables, numeric upgrade pairs, behaviors, and compact base/upgraded rendering
- [x] 2.3 Add relic, potion, power, structural room, and ancient extraction with properties, localized fields, hooks, and ordered statements
- [x] 2.4 Add monster extraction for HP/difficulty expressions, move states, intents, transitions, random/conditional branches, restrictions, and ordered move actions

## 3. Outputs, validation, and update support

- [x] 3.1 Emit deterministic per-category JSON, LLM-oriented Markdown indexes, manifest, coverage summary, and actionable diagnostics
- [x] 3.2 Implement strict validation and stable snapshot-to-snapshot JSON/Markdown diffs
- [x] 3.3 Add self-test fixtures covering card upgrades and representative monster state-machine constructs

## 4. End-to-end verification

- [x] 4.1 Run self-tests and OpenSpec validation
- [x] 4.2 Extract the currently installed Slay the Spire 2 version and verify nonzero, internally consistent coverage for all seven requested categories
- [x] 4.3 Run the project-supported Illusionist build-and-install script after the completed edit batch and report its result
