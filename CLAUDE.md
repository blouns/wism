# WISM Claude Bootstrap

Use this file when Claude opens the public WISM repo.

## Orientation

WISM is a public C# and Unity strategy-game workspace. The design goal is to keep rules and deterministic game logic in `WismClient`, while Unity projects provide views, input, editor tooling, and human-facing workflows.

## Repo Layout

```text
wism/
├── WismClient/      # Core game engine, commands, AI, tests, shared contracts
├── WismUnity/       # Primary Unity game front end
├── WismCompanion/   # Unity companion shell
└── Prototype/       # Legacy prototypes; avoid extending
```

## Agent Bootstrap

- Codex should use the user-level `wism-workspace` skill when available.
- Claude should read the nearest `CLAUDE.md` in the active solution folder.
- All agents should inspect `git status --short --branch` before editing.

## Public Boundary

Do not add local paths, credentials, internal automation details, or non-public planning language to this public repo.

## Core Rule

Prefer WismClient for rules and deterministic state changes. Prefer Unity projects for presentation, editor tooling, input, and visual inspection.
