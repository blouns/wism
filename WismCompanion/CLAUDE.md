# WismCompanion Claude Bootstrap

`WismCompanion` is a public Unity companion shell for WISM. It should consume shared contracts and game state rather than duplicating core rules.

## Agent Bootstrap

Codex should use the user-level `wism-workspace` skill when available. Claude should use this file plus the public repo root `CLAUDE.md`.

## Rules

- Keep game rules in WismClient.
- Keep Unity-specific behavior in Unity-facing code.
- Do not mutate scenes or prefabs without explicit user intent and backup.
- Do not add private orchestration, credentials, local paths, or private telemetry details to public files.
