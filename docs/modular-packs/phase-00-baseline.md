# WISM Modular Packs Phase 00 Baseline

Branch: codex/modular-packs
Base commit: 09a3c59

## Purpose

This checkpoint records the starting point for modular reskinning and simple modes.
The default playable game remains the classic WISM flow with Mini-Illuria as the
Unity default and existing WismClient mod data unchanged.

## Baseline Commands

Run from `WismClient` unless otherwise noted:

```powershell
dotnet test Wism.Client.Test\Wism.Client.Test.csproj --filter PlaygroundScenarioRunnerTests -v:minimal
dotnet run --project Wism.Agent.Playground -- sample --quiet
dotnet run --project Wism.Agent.Playground -- win --quiet
dotnet run --project Wism.Agent.Playground -- world world=TestWorld --quiet
```

Unity smoke surface:

```powershell
WismUnity.GetProjectStatus
WismUnity.GetPackageStatus
WismUnity.GetSceneSummary
WismUnity.GetConsoleSummary
WismUnity.GetGameViewMetadata
```

## Phase Rule

Each later major phase must leave a playable game and receive a git tag before
the next major behavior change starts.
