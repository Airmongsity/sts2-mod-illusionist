# Slay the Spire 2 base-game data extractor

This read-only Windows pipeline converts an installed Slay the Spire 2 build into:

- deterministic JSON for cards, relics, potions, monsters, powers, rooms, and ancients;
- compact English Markdown intended for retrieval by LLMs;
- coverage/parser diagnostics and build provenance;
- a stable diff between two extracted game versions.

It does not execute `sts2.dll`, modify the game, or publish the raw ILSpy output. Static expressions and ordered statements are deliberately retained whenever a construct cannot safely be reduced to a numeric fact.

## Prerequisites

- Windows PowerShell 5.1 or PowerShell 7
- .NET SDK 9 or later
- [`ilspycmd`](https://github.com/icsharpcode/ILSpy) available on `PATH`
- Godot 4.5.1 console/headless executable (the repository copy is detected automatically)

The analyzer selects the highest installed .NET SDK 9+ and targets that SDK's runtime while referencing its bundled Roslyn assemblies. Repeated extraction therefore does not depend on NuGet availability.

## Extract the installed game

From the repository root:

```powershell
.\tools\sts2-data-extractor\extract-sts2-data.ps1
```

The default installation is `D:\Program Files\Steam\steamapps\common\Slay the Spire 2`. Override it when necessary:

```powershell
.\tools\sts2-data-extractor\extract-sts2-data.ps1 `
  -Sts2Root 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2' `
  -Strict
```

Snapshots are written to `game-data/<game-version>/`. Cached decompilation and temporary localization live under `.sts2-data-extractor/`, keyed by the SHA-256 of `sts2.dll`. Both directories are intentionally ignored by Git.

Useful switches:

- `-ForceDecompile`: discard the matching cached source and decompile again.
- `-Strict`: return a failure when structural coverage is lost. Missing optional localization remains a warning.
- `-OutputRoot <path>` and `-WorkRoot <path>`: relocate generated snapshots or caches.
- `-GodotBin`, `-IlspyCmd`, `-DotnetBin`: override tool discovery.

## Self-test

```powershell
.\tools\sts2-data-extractor\extract-sts2-data.ps1 -SelfTest
```

The fixture checks every required category, a `2(1)c` / `6(9)` card upgrade, a random monster branch with cooldown semantics, a conditional branch, and ordered move commands.

## Compare two versions

```powershell
.\tools\sts2-data-extractor\extract-sts2-data.ps1 -Diff `
  -Before .\game-data\v0.110.1 `
  -After .\game-data\v0.111.0 `
  -DiffOutput .\game-data\v0.110.1-to-v0.111.0
```

Diff comparison ignores decompiler source-line and syntax-span positions while retaining pool/encounter ownership and behavioral expressions, preventing harmless line shifts from appearing as gameplay changes.

## Output schema (`sts2-base-game-facts/v1`)

Each category JSON is a sorted array. Common fields include:

- `id`, `entry`, `class`, `namespace`, `source`
- English `localization` keys matching the model entry
- declarative `properties`, `fields`, `constructors`, and relevant `methods`
- `canonicalVars` with base/upgraded numeric pairs where statically recoverable
- category-specific normalized data

Monster records additionally contain a `stateMachine` object with states, intents, transition edges, branch arguments and interpreted cooldown/weight/repeat fields, initial state, referenced move methods, and ordered command expressions. The exact source expression remains beside normalized fields, so a future parser gap is visible to both humans and tools.

`diagnostics.json` distinguishes warnings from structural errors. `coverage.json` reports discovered and emitted counts. `manifest.json` records the release, hashes, localization resource paths, schema, and extractor version.

## Update workflow

After a game update, run the same extract command. If strict mode reports a new construct, inspect its preserved expression in JSON, add a small fixture, extend the relevant Roslyn pattern, run `-SelfTest`, and extract again. Do not copy the raw cache into source control.
