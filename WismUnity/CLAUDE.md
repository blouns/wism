# WismUnity Claude Bootstrap

`WismUnity` is the public Unity game front end for WISM. It should stay focused on presentation, input, UI, editor tooling, visual inspection, and manual play.

## Agent Bootstrap

Codex should use the user-level `wism-workspace` skill when available. Claude should use this file plus the public repo root `CLAUDE.md`.

## Safety

- Inspect `git status --short --branch` before editing.
- Do not overwrite dirty files from another agent or user.
- Do not mutate scenes, prefabs, tilemaps, city/location objects, or MOD world data without an explicit backup and user intent.
- Treat WismClient plugin DLLs as generated artifacts unless the task explicitly updates shipped artifacts.

## Unity Notes

Use Unity 6000-line tooling for package/API validation. Legacy Unity UI is still intentional unless a task explicitly asks for a UI framework migration.
