# WISM Agent Bootstrap

Use this file when Codex opens the public WISM repo.

## First Step

If available, use the user-level Codex skill `wism-workspace` before editing. It routes WISM work by folder and gives the current public/private boundary rules.

## Public Repo Boundary

This repo is public-facing. Do not add local machine paths, credentials, private agent orchestration details, private telemetry routing, or private planning language.

Use public names:

- `WISM`
- `WismClient`
- `WismUnity`
- `WismCompanion`
- `world builder`
- `scenario/eval tooling`
- `MCP tools`

## Repo Lanes

- `WismClient/`: core game rules, commands, AI, tests, ASCII/agent-facing runtime, shared contracts.
- `WismUnity/`: Unity game front end, UI, editor bridge, world-builder inspection, visual/manual play.
- `WismCompanion/`: Unity companion view/controller shell.

## Safety

- Inspect `git status --short --branch` before editing.
- Do not overwrite dirty files from another agent or user.
- Do not mutate Unity scenes, prefabs, tilemaps, city/location objects, or MOD world data without an explicit backup and user intent.
- Do not edit generated WismClient DLLs directly unless the task is specifically about shipped plugin artifacts.
