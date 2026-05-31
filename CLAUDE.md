# WISM — Repo Overview for Claude Code

A from-scratch C# + Unity clone of **Warlords (1990, SSG)**. Core design principle: all game logic lives outside Unity, compiled to DLLs and consumed by the view layer.

## Repo Layout

```
wism/
├── WismClient/          # C# game engine — Core, Api, AI, Agent, tests, Companion, SignalR
├── WismUnity/           # Unity project — primary game UI (view/input only)
├── WismCompanion/       # Unity companion app — live map replica
├── Prototype/           # Old prototypes (WismClientPrototype, WismCloudPrototype)
├── warlords_manual.pdf  # Original game manual (authoritative rules reference)
└── warlords_manual_extracted.txt  # Searchable text version of the manual
```

## How the Solutions Relate

```
WismClient (C#, no Unity)
  └─ builds DLLs → copied to WismUnity/Assets/Plugins/WismClient/
                           and WismCompanion/Assets/Plugins/WismClient/
```

- **WismClient** is the source of truth for all game rules and logic
- **WismUnity** and **WismCompanion** are view/controller shells — they reference WismClient DLLs only
- Never add Unity-specific code to WismClient projects

## Active Development

Primary workspace: `WismClient/` — see `WismClient/CLAUDE.md` for full build commands, architecture, and conventions.

## Game Reference

- Rules: `warlords_manual.pdf` / `warlords_manual_extracted.txt`
- Key principle: all game state changes go through Command objects and are replayable deterministically
- Mod system: JSON files in `WismClient/Wism.Client.Core/mod/`
