# WismCompanion

A standalone Unity debug companion for WISM. It shows a live map + minimap, a filterable
command/event log, and click-to-inspect entity panels — using the same shared contracts the engine
already publishes.

## How it works

By default the companion connects over **WebSocket** to `Wism.SignalR.Host`, which bridges the game's
named pipe to the hub:

```text
WISM game ──named pipe "wism-commands"──> Wism.SignalR.Host ──ws :5000/gameHub──> WismCompanion
```

The transport is chosen from the connection field:

- `ws://localhost:5000/gameHub` — via the SignalR host (default)
- `pipe://wism-commands` — direct named pipe (companion becomes the pipe server; use only when the
  SignalR host is **not** running, since both own the same pipe name)

Either way it deserializes payloads with Newtonsoft (`TypeNameHandling.Auto`) and reads the shared
DTOs from `Assets/Plugins/WismShared/` (`Wism.Companion.Shared.dll` + `Wism.Client.Core.dll`),
published automatically from `WismClient` by `WismClient/Build/Wism.PublishToUnity.targets`.

## First run

1. Open the `WismCompanion` project in Unity. The UI Toolkit theme + PanelSettings are created
   automatically on editor load (or run **WISM > Create Companion Assets** to force it).
2. Press **Play**. With nothing connected you'll see the "waiting to connect" screen; it switches to
   the live view as soon as telemetry arrives.

## Connect to a game

**Default (WebSocket):**
1. Start the bridge: `dotnet run --project WismClient/Wism.SignalR.Host` (serves `:5000`).
2. Launch a WISM game (ASCII WISM `Wism.Client.Agent`, or `WismUnity`).
3. The companion auto-connects to `ws://localhost:5000/gameHub`.

**Optional (direct named pipe, no host):** stop `Wism.SignalR.Host`, set the connection field to
`pipe://wism-commands`, and click **Connect** — the companion then reads the game's pipe directly.
(Only one process can own the pipe, so this can't run at the same time as the host.)

The map, log, and channel selector update live; both transports auto-reconnect.

## Build & run as a standalone app

To run the companion outside the Editor (a normal Windows window):

**From the Editor (quickest):**

- **WISM > Build Windows Player** — builds to `Build/Win64/WismCompanion.exe` and reveals it.
- **WISM > Build and Launch** — builds, then runs the exe.

**Headless / repeatable (CI-friendly):** with the project **closed** in the Editor:

```powershell
WismCompanion\Tools\build-companion.ps1     # builds Build/Win64/WismCompanion.exe
WismCompanion\Tools\run-companion.ps1        # launches it (add -Build to build first)
```

`build-companion.ps1` finds Unity via `-UnityPath`, `$env:UNITY_EXE`, or the Hub editor matching
`ProjectVersion.txt`. The built player ships as a resizable window and keeps streaming in the
background. Build outputs land in `Build/Win64/`, which is git-ignored.

## Controls

- **Map:** drag to pan, scroll to zoom, click the minimap to recenter, click a tile to inspect it.
- **Channel:** switch between parallel game sessions via the channel dropdown.
- **Log:** select an entry to see its full detail.

## Layout

- `Assets/Scripts/Transport` — `ICompanionTransport`, `NamedPipeTransport` (default), `SignalRJsonClient` (WebSocket).
- `Assets/Scripts/State` — `CompanionState`, `LogBuffer`, `CompanionLogEntry`.
- `Assets/Scripts/UI` — `MapView` (Painter2D), `LogView`, `InspectorView`, `CompanionController`, `MapColors`.
- `Assets/Scripts/App` — `CompanionBootstrap` (auto-starts, pumps messages to the main thread).
- `Assets/Resources` — `CompanionView.uxml`, `CompanionTheme.uss`.
