# WismUnity Agent Bootstrap

Use this file when Codex opens `WismUnity`.

## First Step

If available, use the user-level Codex skill `wism-workspace`, then load its `wism-unity` reference.

## Lane

`WismUnity` is the public Unity game front end. It owns visual presentation, input, UI panels, editor bridge tooling, minimap/world-builder inspection, and manual play workflows.

## Safety

- Inspect `git status --short --branch` from the public repo root before editing.
- Do not overwrite dirty files from another agent or user.
- Do not mutate scenes, prefabs, tilemaps, city/location objects, or MOD world data without an explicit backup and user intent.
- Do not edit WismClient plugin DLLs directly unless the task is about shipped plugin artifacts.

## Validation

For code/package/editor bridge changes, prefer Unity batchmode import with the installed Unity 6000 editor. For docs/discovery-only changes, Unity import is not required.
