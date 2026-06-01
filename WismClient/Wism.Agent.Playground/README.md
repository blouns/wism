# WISM Agent Playground

`Wism.Agent.Playground` is a headless playground for agent-driven WISM development. It exists so autonomous agents can run the game, inspect state, prove win/loss flows, and plan isolated worktrees without depending on the interactive ASCII console.

## Commands

```powershell
dotnet run --project Wism.Agent.Playground -- sample
dotnet run --project Wism.Agent.Playground -- win
dotnet run --project Wism.Agent.Playground -- lose
dotnet run --project Wism.Agent.Playground -- parallel agents=4
dotnet run --project Wism.Agent.Playground -- companion scenario=win delayMs=300
dotnet run --project Wism.Agent.Playground -- world world=TestWorld
dotnet run --project Wism.Agent.Playground -- world world=Mini-Illuria
dotnet run --project Wism.Agent.Playground -- campaign seed=20260601 clans=2 maxTurns=12
dotnet run --project Wism.Agent.Playground -- campaign seed=20260601 clans=2 maxTurns=12 delayMs=1500
dotnet run --project Wism.Agent.Playground -- jump checkpoint=<checkpoint-json>
dotnet run --project Wism.Agent.Playground -- worktrees agents=4
```

## Design Notes

- Uses the same `AsciiWorld` starting layout as `Wism.Client.Agent.UI.AsciiGame`.
- Loads full worlds from a complete mod unit: top-level mod files plus `Worlds/<name>/Map.json`, `City.json`, and `Location.json`.
- Supports both persisted test fixture shape and production mod info shape for world city/location files.
- Reports scene-backed worlds clearly when their city/location coordinates are not exported for headless loading.
- Runs without `Console.Clear`, `Console.ReadKey`, or a real terminal handle.
- Emits JSON reports so wism-agents can parse outcomes.
- Can publish command/map telemetry to Wism Companion through the existing `wism-commands` named pipe while running headless scenarios.
- Campaigns generate deterministic 2-4 clan worlds from a seed, validate the world with `WorldValidator`, drive turns through public commands, and save checkpoints under `artifacts/campaigns` unless `out=<path>` is supplied.
- Campaign `delayMs=<milliseconds>` enables Companion telemetry and pauses after map snapshots, which makes move-by-move and turn-by-turn viewing practical.
- `jump checkpoint=<path>` loads a saved campaign checkpoint through `LoadGameCommand` for debugging a recorded moment.
- Keeps public WISM worktree creation as an explicit plan by default. The command output names separate branch/worktree pairs rooted at `HEAD`, which should be the committed playground scaffold.
- Parallel experiments use child processes because the current game runtime has static `Game.Current` and `World.Current` state.

## Campaign Expectations

- V1 campaign generation is intentionally simple: medium maps, border mountains, carved road corridors, owned starting capitals, center-area searchables, and legal starting stacks.
- V1 drivers are deterministic heuristics, not LLM turn players. They start turns, move the largest available stack toward an enemy city, attack adjacent blockers/cities, and end turns.
- Outcomes are reported as victory or bounded stalemate after `maxTurns`.
- Checkpoints are full game snapshots named by moment, such as `setup`, `turn-start`, `pre-move`, `pre-battle`, `battle`, `turn-end`, `victory`, or `stalemate`.
- Searchables are placed and validated, but deliberate searchable exploration is not yet part of the v1 driver.

## Baseline

The pre-playground baseline tag is:

```text
wism-agent-playground-baseline-20260531
```
