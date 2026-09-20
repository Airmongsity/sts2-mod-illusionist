# CLAUDE.md

This file provides context for AI coding assistants (Claude Code, Cursor, GitHub Copilot, Codex, etc.) working with the Sts2-Illusionist-mod repository.

## Project Overview

Sts2-Illusionist-mod is a playable character for Slay the Spire 2, built arround mirrors, foresight, and illusion. Fully localized in English and Simplified Chinese

- Repository: https://github.com/Airmongsity/sts2-mod-illusionist
- License: MIT LICENSE

## Repository Structure

/illusionist-workshop  files upload to Steam workshop
/sts2_illusionist      mod source

### Key Directories

## Communicating with User

- Report bugs/errors to user without concealment after discovery. Unless user requests otherwise.
- Ask user to clarify when user's request is unclear, correct the user when user's expression is incorrect.
- Test codes before responding to user, ensure the test is time-invariant

### File Naming Conventions

- Source files: `snack_case.example` (e.g., `cohere_reranker.py`)
- Test files: `test_<module>.example` (e.g., `test_main.ts`)
- Config/Manifest files: `kebab-case` (e.g., `openclaw.plugin.json`)

## Task Completion Guidelines

These guidelines outline typical artifacts for different task types. Use judgment to adapt based on scope and context.

### Bug fixes

1. **Unit tests**: Add tests that would fail without the fix (regression tests)
2. **Implementation**: Fix the bug
3. **Manual verification**: Run the relevant test suite to confirm the fix
4. **Lint**: Run the appropriate linter for the package you modified

### New Features

1. **Implementation**: Build the feature following existing patterns
2. **Unit tests**: Comprehensive test coverage for new functionality
3. **Documentation**: Update relevant docs in `docs/` for public APIs
4. **Examples**: Add usage examples if the feature introduces new user-facing behavior

### Refactoring / Internal Changes

- Unit tests for any changed behavior
- No documentation needed for internal-only changes
- Ensure all existing tests still pass

### When to Deviate

These are guidelines, not rigid rules. Adjust based on:

- **Scope**: Trivial fixes (typos, comments) may not need tests
- **Visibility**: Internal changes may not need documentation
- **Context**: Some changes span multiple categories — use judgment

When uncertain about expected artifacts, ask for clarification.

## Do NOT

- Commit `.env` files, API keys, or credentials
- Skip pre-commit hooks
- Change public APIs without updating documentations in `docs/`

## Illusionist Release Workflow

Treat `AGENTS.md` as the authoritative project instructions. For every release:

1. Compare the working source with the last published `release-baselines/vX.Y.Z.json` by running `sts2_illusionist\tools\release_surface.ps1 selftest` and then `diff` with the matching Git tag as `-BaseRef`.
2. Publish only the final net player-visible difference. Collapse repeated redesigns of one card into its baseline-to-release result, remove reverted changes, and manually add cross-cutting bug or compatibility fixes that the checker cannot infer.
3. Keep the version in `sts2_illusionist\mod_manifest.json` and `illusionist-workshop\workshop.json` `changeNote` aligned. `next-mirror.md` is the author's design document, not a changelog — read it, never edit it. Export a new immutable release baseline after those contents are final.
4. Run `sts2_illusionist\build-illusionist-windows.ps1` without `-SkipInstallCopy`; verify the installed DLL/PCK/manifest and the synchronized `illusionist-workshop/content/` payload.
5. If a GitHub archive is requested, package the built DLL, PCK, and manifest beneath `illusionist/`. Packed art remains in the PCK.
6. Explicitly stage only code, localization, configuration, documentation, and baseline files. Never commit source art or resources (`*.png`, `*.jpg`, `*.jpeg`, `*.webp`, `*.gif`, `*.svg`, `*.skel`, `*.atlas`, `*.spine`, `*.psd`, `*.import`) or `.tscn` files. Review the staged file list and diff before commit/push.
7. Create and push the matching version tag when finalizing a release. Leave the Steam upload command to the user unless they explicitly ask the assistant to run it.
