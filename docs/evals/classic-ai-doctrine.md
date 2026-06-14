# Warlords Classic AI Doctrine

This note defines the public, deterministic AI doctrine used by WISM evals. The
goal is a credible human-vs-computer opponent: legal turns, visible pressure,
and strategic behavior that can be measured from replayable artifacts.

## Doctrine

The Classic AI should win by playing the map, not by hidden bonuses or random
model decisions.

1. Expand early.
   - Capture nearby neutral and undefended cities quickly.
   - Prefer high-income or forward cities when several targets are reachable.
   - Do not over-garrison during the opening unless an owned city is under
     immediate threat.

2. Produce continuously.
   - Review production at the start of each AI turn.
   - Start production in idle cities.
   - Prefer efficient units for local pressure and mobile units for long routes
     or routed production.
   - Route production toward useful front cities when that materially shortens
     pressure distance.

3. Search with appropriate units.
   - Heroes prioritize ruins, tombs, sages, and libraries.
   - Temples are useful, but should not distract the strategic profile from
     expansion and conquest.
   - Search is a pressure mechanic only when it creates useful capability or
     map tempo.

4. Attack with waves.
   - Capture empty cities immediately.
   - Attack defended cities when the odds are acceptable or when endgame
     pressure justifies a lower threshold.
   - After partial losses, re-evaluate from live state instead of continuing a
     stale plan.

5. Defend selectively.
   - Reinforce cities with nearby enemy pressure.
   - Keep enough defense to avoid free losses, but favor action and pressure
     over static city filling.

6. Eliminate weak clans.
   - Prioritize cities that would remove or materially weaken a viable opponent.
   - Convert local advantage into city-count, income, and army-count pressure.

7. Preserve deterministic execution.
   - Turn execution is command-driven and reproducible from seed, checkpoint,
     and scenario family.
   - LLMs may analyze artifacts or suggest tuning, but they do not decide
     production AI pass/fail and do not execute ordinary turns.

## Current Module Map

The current implementation maps this doctrine to these public modules:

| Doctrine area | Current module or gate |
| --- | --- |
| Strategic desired state | `ClassicStrategicPlanner` creates `Defend`, `Search`, `Expand`, `Siege`, and `Produce` objectives. |
| Bid weighting and allocation | `ClassicStrategicModule` weights tactical bids against active objectives and reserves armies for accepted bids. |
| Capture and siege | `CaptureModule` scores capturable cities, captures adjacent empty cities, attacks city tiles above threshold, and lowers the threshold for endgame assault. |
| Enemy pressure | `ExterminationModule` attacks or moves toward enemy armies when useful. |
| Search | `SearchModule` routes capable armies and heroes to searchable locations and issues search commands. |
| Defense | `CityDefenseModule` and `GarrisonPolicy` keep threatened cities from becoming free captures. |
| Rally | `RallyModule` moves idle forces toward useful pressure when no higher bid wins. |
| Production | `ProductionModule` reviews, renews, starts, and vectors production toward pressure targets. |
| Scorecard gates | `EvalBatchRunner.BuildScorecard` and `BuildClassicAiReadinessScorecard` enforce invariants, strategic signals, and command efficiency. |

## Scorecard Gates

Green AI evidence requires hard correctness plus strategic signal.

Hard correctness gates:

- No crashes.
- No unclassified timeouts.
- No invalid AI commands.
- Parseable eval artifacts.
- Checkpoint loadability when checkpoints are present.
- No mixed hostile stacks, stale visiting armies, or ghost armies.

Strategic signal gates:

- Capture pressure: capture scenarios must produce city captures.
- Search pressure: search scenarios must produce searches by legal units.
- Production economy: production scenarios must start, vector, deliver, or
  otherwise prove useful production lifecycle.
- Contact pressure: road/contact scenarios must produce battle or meaningful
  enemy pressure within bounded turns.
- Siege pressure: defended-city scenarios must stage, attack, or capture with
  acceptable risk.
- Victory pressure: conquest scenarios must win, reduce viable clans, or show
  material city/income/army pressure.
- Strategic planning: strategic-profile runs must persist active or stale
  objectives, proving the reconciler observed and responded to state.
- Command efficiency: repeated no-ops, invalid commands, and excessive path
  waste are regressions even if the run completes.

## Signal vs Noise

Signal:

- A fixed seed set improves a gate without weakening hard correctness.
- A failure includes case id, seed, scenario, turn, clan, command index, tile,
  armies, checkpoint, and rerun command.
- The same checkpoint can be loaded and continued deterministically.
- A scenario family exercises the mechanic it claims to test.

Noise:

- Long runs with no captures, searches, production, battle, or viable-clan
  pressure.
- One lucky win without repeatable seed coverage.
- Companion or Unity visuals without matching headless evidence.
- LLM commentary that does not change deterministic behavior or triage.

## Known Gaps

These gaps should drive the next AI work:

- Board mutation regression coverage is still too thin for the amount of
  pressure the AI can create.
- Strategic objectives exist, but objective traces are not yet a first-class
  artifact for every accepted bid.
- Search is useful but needs stronger hero-item payoff measurement.
- Production can start and vector units, but role selection and upkeep pressure
  need more scenario-specific gates.
- The readiness suite needs a human-facing panel that approximates tutorial
  survival, neutral expansion, road contact, ruin race, defended siege,
  production economy, and multi-clan chaos.

