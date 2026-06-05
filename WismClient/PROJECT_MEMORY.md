# WismClient Project Memory (Working Notes)

Last updated: 2026-02-28

## Project purpose
- Building a C# clone of classic 1990s **Warlords**.
- This repo (`WismClient`) is the main game engine/runtime.
- Companion app exists to test outside both ASCII CLI and Unity app.

## Solution shape
- Core engine/domain: `Wism.Client.Core` (`netstandard2.1`)
- API/commands/controllers: `Wism.Client.Api` (`netstandard2.1`)
- AI layer: `Wism.Client.AI` (`netstandard2.1`)
- ASCII agent runtime: `Wism.Client.Agent` (`net8.0`)
- Companion shared contracts: `Wism.Companion.Shared` (`netstandard2.1`)
- Companion UI: `Wism.Companion.WinForms` (`net8.0-windows`)
- SignalR bridge host: `Wism.SignalR.Host` (`net8.0`)
- Tests: `Wism.Client.Test` (`net8.0`), performance tests (`net6.0`)

## Runtime integration (validated)
- End-to-end flow works:
  - Agent CLI -> named pipe (`wism-commands`) -> SignalR host -> Companion WinForms.
- Companion connection error `SocketException 10061` happens if host is not running.
- Correct startup order for local testing:
  1. Start `Wism.SignalR.Host`
  2. Start `Wism.Companion.WinForms`
  3. Start `Wism.Client.Agent`

## Important launch commands
- Build solution:
  - `dotnet build .\WismClient.sln`
- Run tests:
  - `dotnet test .\Wism.Client.Test\Wism.Client.Test.csproj`
- Run SignalR host:
  - `dotnet run --project .\Wism.SignalR.Host\Wism.SignalR.Host.csproj`
- Run companion app:
  - `dotnet run --project .\Wism.Companion.WinForms\Wism.Companion.WinForms.csproj`
- Run ASCII agent (from Core folder so `mod` files resolve):
  - `Push-Location .\Wism.Client.Core; dotnet run --project ..\Wism.Client.Agent\Wism.Client.Agent.csproj`

## Current quality signals
- Build succeeded with warnings (no errors).
- Tests passed: 143/143.
- Coverage sample: `Wism.Client.Core/Data/GamePersistance.cs` ~79.44%.

## Known issues / caveats
- Agent can fail with `DirectoryNotFoundException` for `mod\Clan.json` if launched from wrong cwd.
- Companion UX is “a little buggy but passable” for now.
- SignalR host appears to be a bridge for Companion (and any client), not Companion-exclusive.

## Product direction notes
- Companion app intended for:
  - Record/replay sessions
  - Save reusable test cases
  - Faster validation outside CLI + Unity

