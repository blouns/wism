# WISM Agent Playground

`Wism.Agent.Playground` is a headless playground for agent-driven WISM development. It exists so autonomous agents can run the game, inspect state, prove win/loss flows, and plan isolated worktrees without depending on the interactive ASCII console.

## Commands

```powershell
dotnet run --project Wism.Agent.Playground -- sample
dotnet run --project Wism.Agent.Playground -- win
dotnet run --project Wism.Agent.Playground -- lose
dotnet run --project Wism.Agent.Playground -- parallel agents=4
dotnet run --project Wism.Agent.Playground -- companion scenario=win delayMs=300
dotnet run --project Wism.Agent.Playground -- companion scenario=win delayMs=300 channel=playground:demo
dotnet run --project Wism.Agent.Playground -- world world=TestWorld
dotnet run --project Wism.Agent.Playground -- world world=Mini-Illuria
dotnet run --project Wism.Agent.Playground -- campaign seed=20260601 clans=2 maxTurns=12
dotnet run --project Wism.Agent.Playground -- campaign seed=20260601 clans=2 maxTurns=12 scenario=capture-pressure
dotnet run --project Wism.Agent.Playground -- campaign seed=20260601 clans=2 maxTurns=12 scenario=ruin-search
dotnet run --project Wism.Agent.Playground -- campaign seed=20260601 clans=2 maxTurns=12 scenario=production-economy
dotnet run --project Wism.Agent.Playground -- eval seed=20260608 cases=1 maxTurns=1 scenarios=classic-ai-production-vectoring clans=2 sizes=medium --quiet
dotnet run --project Wism.Agent.Playground -- campaign seed=20260601 clans=2 maxTurns=12 delayMs=1500
dotnet run --project Wism.Agent.Playground -- campaign seed=20260601 clans=2 maxTurns=12 channel=eval:smoke
dotnet run --project Wism.Agent.Playground -- campaign seed=5150 clans=4 maxTurns=1 size=large delayMs=1500
dotnet run --project Wism.Agent.Playground -- campaign seed=5150 clans=8 maxTurns=2 size=large
dotnet run --project Wism.Agent.Playground -- eval suite=smoke --quiet
dotnet run --project Wism.Agent.Playground -- eval suite=focused --quiet
dotnet run --project Wism.Agent.Playground -- eval suite=readiness workers=2 --quiet
dotnet run --project Wism.Agent.Playground -- eval suite=readiness cases=1440 workers=6 timeoutProfile=legacy --quiet
dotnet run --project Wism.Agent.Playground -- eval suite=readiness cases=1440 workers=6 timeoutProfile=calibrated --quiet
dotnet run --project Wism.Agent.Playground -- eval seed=20260608 cases=50 maxTurns=12 scenarios=capture-pressure,ruin-search,production-economy,road-contact,siege-defense clans=2,4 sizes=medium --quiet
dotnet run --project Wism.Agent.Playground -- jump checkpoint=<checkpoint-json>
dotnet run --project Wism.Agent.Playground -- worktrees agents=4
```

## Design Notes

- Uses the same `AsciiWorld` starting layout as `Wism.Client.Agent.UI.AsciiGame`.
- Loads full worlds from a complete mod unit: top-level mod files plus `Worlds/<name>/Map.json`, `City.json`, and `Location.json`.
- Supports both persisted test fixture shape and production mod info shape for world city/location files.
- Reports scene-backed worlds clearly when their city/location coordinates are not exported for headless loading.
- Runs without `Console.Clear`, `Console.ReadKey`, or a real terminal handle.
- Emits JSON reports so private agent automation can parse outcomes.
- Can publish command/map telemetry to Wism Companion through the existing `wism-commands` named pipe while running headless scenarios.
- Companion telemetry is opt-in. Headless tests, captures, and normal campaign runs do not require Companion, SignalR, or a named-pipe listener.
- Telemetry supports logical channels through `channel=<id>`, allowing simultaneous Playground, ASCII, Unity, and sim sessions to share the same transport without mixing maps.
- Campaigns generate deterministic 2-4 clan worlds from a seed, validate the world with `WorldValidator`, drive turns through public commands, and save checkpoints under `artifacts/campaigns` unless `out=<path>` is supplied.
- Campaign `scenario=capture-pressure` adds owned-but-empty outpost cities near opposing roads so capture behavior is exercised.
- Campaign `scenario=ruin-search` places searchable locations near capitals and routes hero-led stacks through real search commands.
- Campaign `scenario=production-economy` adds same-owner delivery outposts and holds combat movement so routed production delivery is exercised without destination capture noise.
- Eval `scenario=classic-ai-production-vectoring` drives the Warlords Classic AI command provider long enough to prove routed production selection with a `production-vectoring-signal` gate.
- Campaign `size=large` uses a 94x80 Mini-Illuria-style stress map with the real capital anchors for Marthos, Bane's Citadel, Kor, Elvallie, Stormheim, Khamar, Enmouth, and Dunethal.
- Campaign `delayMs=<milliseconds>` enables Companion telemetry and pauses after map snapshots, which makes move-by-move and turn-by-turn viewing practical.
- Campaigns default to deterministic channel IDs such as `playground:capture-pressure:20260601` when Companion telemetry or capture metadata is enabled.
- `jump checkpoint=<path>` loads a saved campaign checkpoint through `LoadGameCommand` for debugging a recorded moment.
- `eval` runs deterministic campaign batches, writes `eval-run.json`, `eval-case-result.jsonl`, `scorecard.json`, `learning-ledger.jsonl`, and `eval-summary.md`, and fails unless required scenario-family signals are present.
- `eval` defaults to process-isolated cases. Each case runs through an `eval-case` child process with a hard wall-clock timeout, so one stuck large scenario becomes a structured failed row instead of blocking the whole batch.
- Eval results are appended incrementally to `eval-case-result.jsonl`; `scorecard.partial.json` is updated during the run and removed after final `scorecard.json` and `eval-summary.md` are written.
- `workers=N` enables bounded parallel eval execution. Keep routine development at `workers=1`; use small bounded pools such as `workers=2` for readiness slices after smoke/focused pass.
- `eval suite=smoke|focused|readiness|marathon` selects standard Classic AI probe mixes. Use smoke/focused for development, readiness for release-facing confidence, and marathon only after readiness is already green.
- `timeoutProfile=legacy|calibrated` makes budget comparisons repeatable. Use `legacy` for pre-calibration baselines and `calibrated` for current large-map probe budgets.
- Eval scorecards include hard correctness gates, checkpoint loadability, Classic AI readiness gates, per-case first-turn quality metrics, and command-efficiency signal.
- Eval case rows include compact `Telemetry` for long-haul diagnosis: runtime seconds, timeout budget used, seconds/turn, commands/turn, meaningful events/turn, map dimensions, tile count, final army/city counts, command type counts, timeout kind, and last moment kind. Treat this as the default signal for scale questions before enabling verbose traces.
- Timeout classes are intentionally distinct: `campaign-timeout` means the campaign exceeded its wall-clock budget while still running, `command-timeout` means a buffered command made no bounded progress, and `case-timeout` means the isolated child process had to be killed.
- Failed board-state invariant cases write `debug-packets.jsonl` beside the campaign manifest with tile, army, owner, checkpoint, suspected subsystem, and one-case repro context.
- WismCompanion is optional debug/viewer infrastructure for evals. WismUnity remains the human-facing game surface and should be validated separately for release or demo readiness.
- Keeps public WISM worktree creation as an explicit plan by default. The command output names separate branch/worktree pairs rooted at `HEAD`, which should be the committed playground scaffold.
- Parallel experiments use child processes because the current game runtime has static `Game.Current` and `World.Current` state.

## Campaign Expectations

- V1 campaign generation is intentionally simple: medium maps, border mountains, carved road corridors, owned starting capitals, center-area searchables, and legal starting stacks.
- Large map generation is a visual/testing preset inspired by the classic Warlords 40x40 tile/minimap presentation. It preserves the Mini-Illuria strategic capital layout but still generates terrain procedurally for testing.
- V1 drivers are deterministic heuristics, not LLM turn players. They start turns, move the largest available stack toward a scenario mission, capture adjacent empty cities, search reachable locations when requested, attack adjacent blockers/cities, and end turns.
- Outcomes are reported as victory or bounded stalemate after `maxTurns`.
- Checkpoints are full game snapshots named by moment, such as `setup`, `turn-start`, `pre-move`, `pre-battle`, `battle`, `turn-end`, `victory`, or `stalemate`.
- Searchable exploration is deliberate for search-oriented scenarios and still opportunistic elsewhere.

## Baseline

The pre-playground baseline tag is:

```text
wism-agent-playground-baseline-20260531
```
