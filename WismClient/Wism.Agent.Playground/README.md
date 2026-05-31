# WISM Agent Playground

`Wism.Agent.Playground` is a headless playground for agent-driven WISM development. It exists so autonomous agents can run the game, inspect state, prove win/loss flows, and plan isolated worktrees without depending on the interactive ASCII console.

## Commands

```powershell
dotnet run --project Wism.Agent.Playground -- sample
dotnet run --project Wism.Agent.Playground -- win
dotnet run --project Wism.Agent.Playground -- lose
dotnet run --project Wism.Agent.Playground -- parallel agents=4
dotnet run --project Wism.Agent.Playground -- world world=TestWorld
dotnet run --project Wism.Agent.Playground -- world world=Mini-Illuria
dotnet run --project Wism.Agent.Playground -- worktrees agents=4
```

## Design Notes

- Uses the same `AsciiWorld` starting layout as `Wism.Client.Agent.UI.AsciiGame`.
- Loads full worlds from a complete mod unit: top-level mod files plus `Worlds/<name>/Map.json`, `City.json`, and `Location.json`.
- Supports both persisted test fixture shape and production mod info shape for world city/location files.
- Reports scene-backed worlds clearly when their city/location coordinates are not exported for headless loading.
- Runs without `Console.Clear`, `Console.ReadKey`, or a real terminal handle.
- Emits JSON reports so wism-agents can parse outcomes.
- Keeps public WISM worktree creation as an explicit plan by default. The command output names separate branch/worktree pairs rooted at `HEAD`, which should be the committed playground scaffold.
- Parallel experiments use child processes because the current game runtime has static `Game.Current` and `World.Current` state.

## Baseline

The pre-playground baseline tag is:

```text
wism-agent-playground-baseline-20260531
```
