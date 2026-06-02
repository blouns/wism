# WismClient Agent Bootstrap

Use this file when Codex opens `WismClient`.

## First Step

If available, use the user-level Codex skill `wism-workspace`, then load its `wism-client` reference.

## Lane

`WismClient` is the public source of truth for rules, commands, AI, deterministic tests, ASCII/agent-facing runtime, scenario fixtures, and shared contracts consumed by Unity front ends.

## Commands

Run from `WismClient/` unless the task says otherwise:

```powershell
dotnet build WismClient.sln -c Release
dotnet test Wism.Client.Test\Wism.Client.Test.csproj -c Release
dotnet test WismClient.sln -c Release
```

## Rules

- Keep Unity-specific code out of WismClient.
- Prefer command/replayable flows for state changes.
- Keep Unity-compatible libraries compatible with the Unity API profile in use.
- Treat DLLs copied into Unity plugin folders as generated artifacts unless the task explicitly updates shipped artifacts.
