# WISM

WISM, short for **War in a Senseless Mind**, is a public-source, turn-based fantasy strategy project inspired by classic 1990s strategy games.

The project is split between a reusable C# game engine and Unity frontends. The core design goal is simple: keep rules, commands, replay, AI, and simulation logic outside Unity so the game can support multiple clients and survive frontend changes over time.

## Current Status

WISM is playable and under active development. Expect a working but evolving codebase, especially around Unity scene authoring, world-building tools, and frontend polish.

Primary current target:

- Windows desktop
- Unity `6000.4.9f1`
- .NET 8 for WismClient tools and tests

## Technical Highlights

- Deterministic command stream for gameplay state changes.
- Replay-friendly command persistence and capture tests.
- Core rules isolated from Unity presentation code.
- ASCII, Companion, and Unity clients over the same engine contracts.
- Modular JSON world data and Unity world-builder inspection tools.
- Data-only profile and feature-pack authoring through the [WISM Mod Kit](docs/mod-kit/README.md).

## Repository Layout

```text
wism/
├── WismClient/      # Core game engine, API/controllers, AI, tests, and support apps
├── WismUnity/       # Unity game frontend and world-builder scene surface
├── WismCompanion/   # Companion/map-facing client experiments
├── docs/            # Public project notes and architecture guidance
├── wism_manual.md   # WISM-specific notes/manual content
└── README.md
```

## Architecture

```text
Player / AI / UI
       |
       v
WismClient API -> Command stream -> WismClient Core model
       |
       +--> WismUnity frontend
       +--> ASCII/agent-style frontends
       +--> Companion or remote-play surfaces
```

### WismClient

`WismClient` is the source of truth for game rules and state transitions.

All gameplay mutations should enter the system as command objects. A controller creates a command, the API records it in command order, and processors execute the command against the core model. Replay consumers can reapply the same ordered command stream from the same starting state and random seed.

New gameplay features should preserve this path instead of mutating core game state directly from Unity, AI, network, or companion surfaces.

### WismUnity

`WismUnity` is the main Unity frontend. It renders the map, accepts player input, drives UI panels, and presents turn-by-turn game feedback.

Unity is also currently used as a world-builder surface. Some map state is represented as Unity scenes, tilemaps, city objects, and location objects. That hybrid authoring/runtime model is intentional for now but should be handled carefully. See [World Builder Notes](docs/wismunity-world-builder-notes.md).

### WismCompanion

`WismCompanion` is an experimental companion surface for map/state visualization and related client work. It consumes WismClient artifacts rather than defining core rules.

## Build And Test

Build WismClient:

```powershell
dotnet build WismClient\WismClient.sln --configuration Release
```

Run WismClient tests:

```powershell
dotnet test WismClient\WismClient.sln --configuration Release
```

Open Unity:

1. Install Unity `6000.4.9f1`.
2. Open `WismUnity/` as the Unity project.
3. Let packages restore.
4. Open a scene such as `Assets/Scenes/Mini-Illuria.unity`.
5. Press Play.

WismUnity consumes committed WismClient plugin DLLs from
`WismUnity/Assets/Plugins/WismClient/`. They are generated shipped artifacts:
commit updates only when intentionally republishing WismClient changes for Unity,
and restore incidental DLL churn before public commits. See
[WismUnity](WismUnity/README.md).

## Unity Editor And World Builder

WismUnity is both a playable frontend and, for now, a world-builder surface. Scene state, tilemaps, city objects, location objects, and MOD JSON all matter. Treat those assets carefully and prefer read-only inspection before any automated scene or data mutation.

See [World Builder Notes](docs/wismunity-world-builder-notes.md).

## Public Repo Hygiene

This repository is public. Do not commit private automation plans, private repository names, internal codenames, secrets, tokens, local credentials, or unlicensed third-party assets.

See:

- [Public Boundary](docs/public-boundary.md)
- [Asset Provenance](docs/asset-provenance.md)
- [Architecture](docs/architecture.md)
- [WISM Mod Kit](docs/mod-kit/README.md)
- [Project TODO](docs/TODO.md)
- [Contributing](CONTRIBUTING.md)

## Inspiration And Legal Note

WISM is inspired by classic turn-based fantasy strategy games. It is not affiliated with, endorsed by, or sponsored by Strategic Studies Group, Ubisoft, or any rights holder for the Warlords series.

Do not add copyrighted game manuals, copied commercial assets, copied sound effects, copied maps, or trademarked branding unless the project has a clear redistribution right documented in [Asset Provenance](docs/asset-provenance.md).

The repository is source-available while licensing and asset provenance are being audited. See [LICENSE](LICENSE.md).

## References

- https://en.wikipedia.org/wiki/Warlords_(1990_video_game)
- http://www.ssg.com.au/
