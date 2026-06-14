# WISM Eval Hub

WISM evals are deterministic gameplay evidence. They are not demos, screenshots,
or claims that a process completed. A Green eval means the scorecard met its
gates and produced enough artifact context to reproduce failures.

## Standard Artifacts

An eval run writes:

- `eval-run.json`: run metadata, seed, scenario mix, driver profile, and output
  paths.
- `eval-case-result.jsonl`: one row per case with seed, scenario family, clan
  count, size, counters, telemetry, debug packet path, and failure class.
- `scorecard.json`: hard gates, Classic AI readiness gates, counters, and
  aggregate status.
- `learning-ledger.jsonl`: durable observations and recommended follow-up.
- `eval-summary.md`: human-readable summary.

Failed board-state invariant cases also write `debug-packets.jsonl` beside the
campaign manifest. Those packets include seed, scenario, turn, clan, command
index, tile, army ids, owners, checkpoint, suspected subsystem, and a rerun
command.

## Human-Readiness Suite

The `human-readiness` suite is the default public contract for approximating a
human-vs-computer readiness panel. It is still headless and deterministic; Unity
manual play remains a separate human-facing validation surface.

Run:

```powershell
dotnet run --project Wism.Agent.Playground -- eval suite=human-readiness workers=2 --quiet
```

Routine focused proof:

```powershell
dotnet run --project Wism.Agent.Playground -- eval seed=20260701 cases=20 maxTurns=80 scenarios=classic-ai-capture-pressure,classic-ai-ruin-search,classic-ai-production-vectoring clans=4 sizes=medium --quiet
```

The suite covers:

| Human-readiness behavior | Scenario family |
| --- | --- |
| Tutorial survival and bounded legal turns | `classic-ai-road-contact`, `classic-ai-target-captured-recovery` |
| Neutral expansion | `classic-ai-neutral-expansion` |
| Road contact and first battle pressure | `classic-ai-road-contact` |
| Ruin race and hero search | `classic-ai-ruin-search` |
| Defended-city siege | `classic-ai-defended-siege` |
| Production economy and reinforcement flow | `classic-ai-production-economy`, `classic-ai-production-vectoring` |
| Recovery after changed state | `classic-ai-lost-battle-recovery`, `classic-ai-target-captured-recovery` |
| Multi-clan conquest pressure | `classic-ai-conquest` |

## Green Criteria

Hard correctness gates:

- 0 crashes.
- 0 unclassified timeouts.
- 0 invalid Classic AI commands.
- 100 percent parseable artifacts.
- Checkpoints load when checkpoint evidence is present.
- 0 mixed hostile stacks, stale visiting armies, or ghost armies.

Strategic pressure gates:

- Capture-focused cases produce captures.
- Search-focused cases produce legal searches.
- Production-focused cases produce starts, deliveries, or routed vectors.
- Contact cases produce battle or capture.
- Siege cases produce battle and capture.
- Recovery cases avoid invalid/no-progress turns and keep useful pressure.
- Conquest cases produce victory, viable-clan reduction, or material dominance.
- Command efficiency remains above the scorecard threshold.

## Interpretation

Signal:

- A fixed seed set improves a failed gate without weakening hard correctness.
- A failed case includes replay/checkpoint/debug packet evidence.
- A run changes the trend for a named scenario family.

Noise:

- Long-running cases with no captures, searches, production, battle, or
  conquest pressure.
- A single lucky victory.
- Visual confirmation without matching `scorecard.json`.
- LLM commentary that does not change deterministic behavior or triage.

