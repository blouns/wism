# WismCompanion Agent Bootstrap

Use this file when Codex opens `WismCompanion`.

## First Step

If available, use the user-level Codex skill `wism-workspace`. Load the `wism-unity` reference for Unity-shell work and the `wism-client` reference for shared-contract work.

## Lane

`WismCompanion` is a public Unity companion shell. Keep it aligned with WismClient shared contracts and avoid duplicating core rules in Unity code.

## Safety

- Inspect `git status --short --branch` before editing.
- Do not overwrite dirty files from another agent or user.
- Do not mutate Unity scenes or prefabs without explicit user intent and backup.
- Keep private orchestration and local environment details out of public files.
