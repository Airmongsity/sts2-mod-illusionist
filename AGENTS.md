# AGENTS.md

This file provides context for AI coding assistants (Claude Code, Cursor, GitHub Copilot, Codex, etc.) working with the Sts2-Illusionist-mod repository.

## Project Overview

Sts2-Illusionist-mod is an mod for Slay the Spire II.

- Repository: None yet
- Documentation: None yet
- License: None yet

## Repository Structure

Empty for now

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