## Why

Release notes should describe only the net player-visible difference from the previous public version. Maintaining notes after every development edit repeats discarded designs, while manually reviewing the full content pool at release time wastes effort on unchanged content.

## What Changes

- Add a deterministic exporter for the public surface of cards, relics, potions, powers, afflictions, and relevant localization.
- Add a comparator that keys entities by stable ID and reports only added, removed, or net-changed fields between a published baseline and the current snapshot.
- Add Git `Release-Note:` trailer collection for behavior changes that cannot be inferred from public metadata.
- Generate a Markdown release-note draft for human review without exposing intermediate development iterations.
- Save the v0.5.3 public surface as the first comparison baseline.

## Capabilities

### New Capabilities

- `release-surface-diff`: Export versioned public-content snapshots and generate stable-ID-based net release differences plus explicit Git release-note trailers.

### Modified Capabilities

None.

## Impact

- Adds release tooling and a generated baseline under the repository's tooling/release metadata areas.
- Reads C# registrations, constructors, upgrades, dynamic variables, localization JSON, the manifest version, and Git history.
- Does not change runtime card behavior, stable IDs, localization, build contents, or art handling.
