# WismUnity SME Runtime Map

Purpose: capture the live architecture of WismUnity as a frontend over WismClient, and keep the next research passes focused on code paths that preserve that boundary.

## Design Boundary

WismClient is the core rules engine. Game state, commands, controllers, combat, movement, production, pathing, persistence entities, AI services, and world/model concepts should stay there so Unity version churn cannot rewrite the game.

WismUnity is the rich visual frontend. It owns scene bootstrapping, Unity tilemaps, cameras, panels, input modes, visual GameObjects, command presentation processors, save/load UI, minimap interaction, and frontend-specific state such as selected boxes and camera position.

WismAgent / ASCII is a sibling frontend path over the same WismClient model. Treat it as an independent proof surface for game rules and command behavior, not as a Unity replacement. When WismUnity behavior differs from WismAgent, first ask whether the difference is UI presentation or real rules drift.

Do not make original Warlords-in-DOSBox automation a dependency for WISM work. It can remain an observation/calibration lane only.

## Current Known-Good Baseline

- Unity editor: 6000.0.34f1.
- Unity packages restored for compilation:
  - `com.unity.ugui`
  - `com.unity.test-framework`
  - `com.unity.nuget.newtonsoft-json`
- Legacy Unity UI is still intentional.
- Plastic SCM JSON references were removed from application code.
- GLES2 was removed from Android graphics APIs.
- WismClient build/test validation passed after the repair pass.
- Unity import/compile passed; runtime was manually confirmed by opening and running the project.

## Primary Boot Path

1. `UnityManager.Start()` calls `Initialize(gameSettings)`.
2. If no external settings exist, `UnityGameFactory.CreateDefaultGameSettings()` supplies a default new game.
3. `UnityManager.IntializeWismApi()` gets `GameManager`, initializes WismClient controllers and the in-memory command repository, initializes `DebugManager`, and keeps the `ControllerProvider`.
4. `UnityManager.InitializeCommandProcessors()` installs Unity-specific processors first, then WismClient `StandardProcessor` as the catch-all executor.
5. `UnityManager.InitializeUI()` sets timing/cameras and binds key panel prefabs/components.
6. `UnityManager.InitializeWismGame()` asks `UnityGameFactory` to create or load game state.
7. `UnityGameFactory.CreateGame()` builds a `GameEntity`, creates players, creates a world from the Unity scene, then calls `GameManager.NewGame(settings)`.
8. `UnityManager.FixedUpdate()` advances through `Bootstrap`, `Starting`, and `Running`.
9. The first turn is queued by `GameManager.StartTurn(Game.Current.GetCurrentPlayer())`.

## Command Pipeline

The runtime is command-driven:

1. User input, AI, load/save, or UI panel choice calls a `GameManager` method.
2. `GameManager` creates WismClient commands and adds them to `CommandController`.
3. `UnityManager.DoTasks()` checks `LastCommandId + 1`.
4. The first `ICommandProcessor` whose `CanExecute()` matches executes the command.
5. Unity-specific processors can animate, wait, show panels, update selected boxes, notify users, and then call `command.Execute()`.
6. `StandardProcessor` executes any command without a Unity-specific presentation processor.
7. `UnityManager.AdvanceCommand()` updates `LastCommandId` when the command succeeds or fails; `InProgress` commands are retried on later ticks.

Important implication: WismClient commands should remain authoritative for rules. WismUnity processors should be presentation adapters plus scene synchronization.

## WismClient Boundary Touchpoints

- `GameManager` is the main WismClient facade for Unity.
- `UnityManager` owns command scheduling and frontend reaction.
- `UnityGameFactory` turns Unity setup choices into WismClient `GameEntity` settings.
- `WorldTilemap.CreateWorldFromScene()` translates Unity tilemap terrain into WismClient `Tile[,]`.
- `UnityWorldFactory`, `UnityCityFactory`, and `UnityLocationFactory` bridge scene data into WismClient world data.
- `PersistanceManager` persists both Unity frontend snapshot data and `Game.Current.Snapshot()`.
- `InputHandler` interprets clicks against WismClient `Tile`, `Army`, `City`, `Location`, and `GameState`.
- `UnityManager.GenerateAICommands()` uses WismClient AI services to generate commands for non-human turns.
- Telemetry snapshot broadcasting is frontend-hosted but based on WismClient map/game state.

## Frontend Responsibility Areas

- Input modes: game, UI, wait-for-key, AI turn, save/load picker, item picker, location picker.
- Visual managers: armies, cities, flags, items, debug text.
- Mapping: tilemap conversion, minimap interaction, terrain/object extraction.
- UI panels: game setup, notifications, war, city production, save/load, item/army pickers, yes/no, solicit input.
- Cutscene stages: battle, location search, city build/raze, hero recruitment, production renewal.
- Persistence extras: save slot names, camera position, last command id, Unity world name.

## Current Risk Lanes

- Command/UI synchronization: `LastCommandId`, `ActionState.InProgress`, and panel-driven input modes can produce subtle stuck states.
- Save/load synchronization: Unity snapshot plus WismClient snapshot can drift.
- Scene-derived worlds: Unity tilemaps and mod JSON must agree about terrain, cities, locations, and coordinates.
- Selection semantics: WismClient moves selected armies into visiting armies; Unity selection boxes and visual stacks must track that exactly.
- Battle presentation: prepare/attack/complete is a three-command sequence with timed UI and WarPanel state.
- AI turns: WismUnity generates WismClient AI commands when no next command exists; this should be checked against WismAgent command behavior.
- Telemetry: snapshot publish timeouts are currently non-compilation issues but can hide runtime friction.
- Legacy UI scaling: the existing refactoring backlog calls out canvas scale, click targets, fixed sizing, and performance from `HardFind`/Update usage.

## Existing Test/Validation Surfaces

- Unity compile/import: Unity batchmode import should remain the first frontend health check after package or API drift.
- Unity PlayMode: `GameInitializationTests.LoadTestScenePasses` is the current smoke target, but result XML emission needs repair or a repeatable invocation.
- WismClient build: `dotnet build ..\WismClient\WismClient.sln -c Release`.
- WismClient tests: `dotnet test ..\WismClient\Wism.Client.Test\Wism.Client.Test.csproj -c Release --no-build`.
- Agent/ASCII playground: use WismAgent/AgentPlayground to compare game-rule behavior without Unity UI noise.

## SME Research Backlog

1. Startup trace: document every component required on the default scene `UnityManager` GameObject.
2. Command matrix: map each WismClient command to its Unity processor or `StandardProcessor`.
3. UI flow traces: save/load, city production, army picker, item picker, battle, search locations, end/start turn.
4. Scene data contract: compare Unity scene tilemap/object extraction with `Assets/Mod` and `Assets/Plugins/WismClient/Mods`.
5. WismAgent parity: run equivalent command flows in ASCII/AgentPlayground and compare WismClient state transitions.
6. PlayMode harness: make `LoadTestScenePasses` produce stable XML and expand it to assert first-turn initialization.
7. Known bug lanes: selection/quit/defend, save slot drift, navy/pathing edge cases, click targeting, production panel sizing.
8. Frontend modernization map: identify which Unity-heavy code is necessary UI glue and which can move behind thinner adapters.

## Working Rule

When repairing or extending WismUnity, first decide whether the change is:

- Core rule or model behavior: implement in WismClient and validate through WismClient tests plus WismAgent/ASCII where possible.
- Unity presentation or interaction: implement in WismUnity and validate through PlayMode/manual scene smoke tests.
- Boundary drift: fix the contract in both places with a small adapter or explicit data/entity change.
