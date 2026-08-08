# Release Surface Workflow

User-facing release notes describe the net difference between the last published version and the next release. Intermediate development designs belong in Git and OpenSpec, not in the release notes.

## Files

- `release-baselines/vX.Y.Z.json`: immutable public surface captured from a published release.
- `sts2_illusionist/tools/release_surface.ps1`: snapshot, comparison, Git trailer, and self-test tool.
- `release-notes/generated/`: ignored temporary JSON and Markdown drafts.

The public surface covers registered cards, relics, potions, powers, and afflictions. Entities are keyed by stable ID. Card snapshots include constructor fields, dynamic-variable declarations, upgrade code, keyword references, English and Simplified Chinese localization, and a comment/whitespace-insensitive implementation fingerprint.

## During Development

No release-note file needs to be updated for ordinary balance iterations. For a cross-cutting bug fix or behavior change that cannot be inferred from a registered entity, add one or more commit-message lines:

```text
Release-Note: 修复最后一名敌人死亡后镜像继续执行导致的卡死。
```

Repeated identical trailers are deduplicated when the draft is generated.

## Generate a Draft

From the repository root, compare the working source with the last published baseline:

```powershell
$PreviousVersion = "0.5.4"
powershell -NoProfile -ExecutionPolicy Bypass -File .\sts2_illusionist\tools\release_surface.ps1 diff `
  -BaselinePath ".\release-baselines\v$PreviousVersion.json" `
  -BaseRef "v$PreviousVersion" `
  -JsonOutputPath .\release-notes\generated\current-diff.json `
  -MarkdownOutputPath .\release-notes\generated\current-draft.md
```

Only added, removed, or net-changed stable IDs appear. Multiple edits to one card collapse into one baseline-to-current comparison. A change that is reverted to the baseline disappears.

The Markdown output is a review draft. Replace raw expressions with polished player-facing wording where appropriate; omit implementation-only review candidates that do not affect players.

## Publish a New Baseline

After the release contents, version, and notes are final, export the new immutable baseline:

```powershell
$NewVersion = "0.5.5"
powershell -NoProfile -ExecutionPolicy Bypass -File .\sts2_illusionist\tools\release_surface.ps1 snapshot `
  -OutputPath ".\release-baselines\v$NewVersion.json"
```

Do not overwrite an older published baseline. Commit the new versioned file with the release.

## Verify the Tool

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\sts2_illusionist\tools\release_surface.ps1 selftest
```

The self-test checks deterministic exports, an empty baseline comparison, a single-entity change, a reverted change, duplicate stable IDs, malformed JSON, and an invalid Git base.
