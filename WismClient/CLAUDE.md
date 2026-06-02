# WismClient Claude Bootstrap

`WismClient` is the public core engine for WISM. It owns rules, commands, AI, deterministic tests, ASCII/agent-facing runtime, scenario fixtures, and shared contracts consumed by Unity front ends.

## Build And Test

Run from `WismClient/`:

```powershell
dotnet build WismClient.sln -c Release
dotnet test Wism.Client.Test\Wism.Client.Test.csproj -c Release
dotnet test WismClient.sln -c Release
```

## Architecture Rules

- Keep Unity-specific code out of WismClient.
- Prefer command/replayable flows for state changes.
- Keep rule behavior testable without Unity.
- Treat Unity plugin DLLs as generated artifacts unless the task explicitly updates shipped artifacts.

## Unity Boundary

Unity front ends consume WismClient through stable APIs and DLLs. Do not solve Unity UI/editor issues by moving Unity dependencies into core rule projects.
