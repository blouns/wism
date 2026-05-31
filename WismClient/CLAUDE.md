# WismClient — Claude Code Context

C# game engine for a Warlords (1990, SSG) clone. No Unity dependency — this solution is pure game logic compiled to DLLs that the WismUnity project consumes.

## Solution Layout

```
WismClient/
├── Wism.Client.Core/        # Entities, controllers, strategies, mod system (netstandard2.1 for Unity 6)
├── Wism.Client.Api/         # Command pattern, repository, CommandProcessor (netstandard2.1 for Unity 6)
├── Wism.Client.AI/          # AiController, strategic/tactical modules, pathfinding (netstandard2.1 for Unity 6)
├── Wism.Client.Agent/       # Console entry point, DI, Serilog (net8.0)
├── Wism.Client.Test/        # NUnit tests (net8.0)
├── Wism.Companion.Shared/   # DTOs: MapSnapshot, TileDto, ArmyDto, CityDto (netstandard2.1 for Unity 6)
├── Wism.Companion.WinForms/ # Live map companion app via SignalR (net8.0-windows)
├── Wism.SignalR.Host/       # GameHub: BroadcastCommand, BroadcastMapSnapshot (net8.0)
└── Wism.Performance.Test/   # NBench benchmarks (net6.0)
```

**Dependency order:** Core → Companion.Shared → Api → AI → Agent

## Build & Run

```bash
# Build entire solution
dotnet build WismClient.sln

# Run tests
dotnet test Wism.Client.Test/Wism.Client.Test.csproj

# Run SignalR host
dotnet run --project Wism.SignalR.Host/Wism.SignalR.Host.csproj

# Run Companion app
dotnet run --project Wism.Companion.WinForms/Wism.Companion.WinForms.csproj

# Run ASCII agent — MUST run from Wism.Client.Core dir (mod/ files resolve relative to cwd)
cd Wism.Client.Core && dotnet run --project ../Wism.Client.Agent/Wism.Client.Agent.csproj
```

**Local test startup order:** SignalR Host → WinForms Companion → Agent
(Agent will throw SocketException 10061 if SignalR Host is not already running.)

## Unity DLL Deployment

After building, copy these 4 DLLs to `../WismUnity/Assets/Plugins/WismClient/`:
- `Wism.Client.Core.dll`
- `Wism.Client.Api.dll`
- `Wism.Client.AI.dll`
- `Wism.Companion.Shared.dll`

Use `/project:deploy-unity` to do this automatically.

These four projects import `Build/Wism.PublishToUnity.targets` and are directly loaded by Unity from `WismUnity/Assets/Plugins/WismClient/`. WismUnity is currently Unity 6000.0.34f1 with the .NET Standard 2.1 API profile, so keep these projects on `netstandard2.1`. Do not raise them to `net8.0` or add dependencies that require newer TFMs unless the Unity baseline is deliberately changed first. If WismUnity is migrated backward to an older Unity profile, re-check this boundary before changing dependencies. Tooling-only projects such as Agent, AgentPlayground, Companion WinForms, SignalR Host, and tests may use modern .NET.

## Key Architecture Patterns

### Command Pattern (Api layer)
All game actions are `Command` objects implementing `ICommandAction` + `IReplayableCommand`.
- Base class: `Command` (abstract)
- Commands organized in: `Wism.Client.Api/Commands/{Armies,Cities,Players,Games,Heros,Locations}/`
- To add a new action: subclass `Command`, implement `ExecuteAsync()` and `UndoAsync()`

### Strategy Pattern (Core layer)
- `IWarStrategy` — combat resolution
- `IMovementStrategy` — Standard / Naval / HeroFlight / Composite
- `ITraversalStrategy` — mirrors movement strategies

### AI Extension Points
- Add new behavior: implement `ITacticalModule` (see `CaptureModule`, `ExterminationModule`)
- Swap planning: implement `IStrategicModule`
- Wire influence maps: `IInfluenceMap` is built but not yet used
- `AssetAllocator` and `IActionExecutor` are stubs — do not rely on them

### Mod System (JSON, in `Wism.Client.Core/mod/`)
- `Army.json`, `Clan.json`, `Terrain.json`, `Hero.json`, `Artifact.json`
- `Worlds/` — Illuria (80 cities), Mini-Illuria, DevWorld, AsciiWorld, test worlds
- Each world: `City.json`, `Location.json`, `Map.json`

## What NOT to Do

- Do not edit DLLs in `../WismUnity/Assets/Plugins/WismClient/` — they are build outputs
- Do not run the Agent from the repo root — it will fail with `DirectoryNotFoundException` for `mod/Clan.json`
- Do not add Unity-specific code to WismClient projects (they must stay Unity-free)
- Do not use `Console.WriteLine` in library projects — use the injected `ILogger`

## Game Reference

- Full rules: `../warlords_manual.pdf` (or `../warlords_manual_extracted.txt` for text search)
- `Game.Current` — singleton, holds `GameState` enum
- `GameState` values: Ready, SelectedArmy, MovingArmy, AttackingArmy, CompletedBattle, etc.

## Current Quality Baseline

- Build: succeeds (warnings OK, no errors)
- Tests: 143/143 passing
- AI completeness: ~30-35% (tactical layer works; strategic layer is stub)
