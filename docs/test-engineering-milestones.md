# WISM Test Engineering Milestones

This document defines falsifiable milestones for WISM test quality, coverage,
Unity validation, scenario/eval tooling, and agent supportability.

The intent is simple: every milestone has a numeric target, an objective proof,
and a clear failure condition. A test process is not green because it ran; it is
green only when the required evidence exists and passes its gates.

## Measurement Rules

- Test counts are discovered from the relevant test runner output, not estimated.
- Coverage gates use line and branch coverage where the .NET runner can collect
  it. Unity coverage may use smoke and scenario gates until Unity coverage
  collection is wired.
- Runtime/eval gates require durable artifacts, not console output alone.
- Unity scene, prefab, tilemap, city/location, and MOD data validation must be
  read-only unless a work item explicitly authorizes mutation.
- Public repository tests must not require private infrastructure.

## Milestone T0: Baseline Truth

Target date: immediate

Goal: make the current test surface measurable and repeatable.

Quantifiable targets:

- WismClient test suite reports at least 180 discovered tests.
- WismClient test suite has 0 failures.
- WismUnity has a documented Unity test inventory with PlayMode and EditMode
  counts.
- WismCompanion has an explicit test inventory, even if the first count is 0.
- WISM Agent Playground can run 1 bounded campaign scenario and produce a
  campaign artifact directory.
- Unity Playground smoke attempts write a structured launch report whether they
  pass or fail.

Proof commands:

```powershell
dotnet test WismClient\Wism.Client.Test\Wism.Client.Test.csproj -c Release -v:minimal
dotnet run --project WismClient\Wism.Agent.Playground -- campaign seed=20260606 clans=2 maxTurns=4 scenario=capture-pressure --quiet
```

Proof artifacts:

- WismClient test runner summary.
- `artifacts/campaigns/<run>/campaign.json`.
- Unity Playground `launch-report.json` for a smoke attempt.
- A checked-in or generated inventory listing Unity and Companion test counts.

Failure conditions:

- WismClient discovers fewer than 180 tests.
- Any WismClient test fails.
- Campaign command exits non-zero.
- Campaign artifact is missing or unparseable.
- Unity smoke failures do not produce a structured launch report.

## Milestone T1: Fast Gate

Target date: next public stabilization pass

Goal: establish a fast, reliable pre-commit quality gate for normal changes.

Quantifiable targets:

- WismClient fast gate completes in under 60 seconds on the primary dev machine.
- WismClient fast gate reports 0 failures across at least 180 tests.
- Public boundary scan reports 0 private-context findings.
- Agent Playground smoke runs at least 2 scenario families:
  `capture-pressure` and `ruin-search`.
- Each smoke scenario produces at least 1 checkpoint and a parseable
  `campaign.json`.
- Template tests are removed or replaced; Unity EditMode has 0 placeholder
  tests.

Proof commands:

```powershell
dotnet test WismClient\Wism.Client.Test\Wism.Client.Test.csproj -c Release -v:minimal
dotnet run --project WismClient\Wism.Agent.Playground -- campaign seed=20260606 clans=2 maxTurns=4 scenario=capture-pressure --quiet
dotnet run --project WismClient\Wism.Agent.Playground -- campaign seed=20260607 clans=2 maxTurns=4 scenario=ruin-search --quiet
```

Proof artifacts:

- Test runner summary.
- Two campaign artifact directories.
- Boundary scan output.
- Unity test inventory showing no placeholder test files.

Failure conditions:

- Any gate exceeds the time budget by more than 20 percent.
- Any command exits non-zero.
- Scenario output lacks checkpoints.
- Placeholder Unity tests remain in the inventory.

## Milestone T2: Core Rules Coverage

Target date: after T1 is stable

Goal: make WismClient the trusted rules safety net.

Quantifiable targets:

- WismClient line coverage is at least 65 percent for `Wism.Client.Core`.
- WismClient branch coverage is at least 50 percent for `Wism.Client.Core`.
- Command/controller tests cover at least these command families:
  game, player turn, army movement, army battle, city production, city capture,
  location search, hero item transfer, save/load.
- Each command family has at least 2 tests: one success and one rejected or
  edge-case path.
- Scenario tests include at least 5 named scenarios.
- Captured replay tests include at least 2 committed captures.

Proof commands:

```powershell
dotnet test WismClient\Wism.Client.Test\Wism.Client.Test.csproj -c Release --collect:"XPlat Code Coverage"
```

Proof artifacts:

- Coverage report in Cobertura or equivalent format.
- Command-family coverage matrix.
- Scenario manifest listing named scenario tests and replay captures.

Failure conditions:

- Coverage report is missing.
- Any threshold is below target.
- Any required command family lacks success or edge coverage.
- Replay captures are not reproducible from committed fixtures.

## Milestone T3: Unity Reliability Gate

Target date: after Unity package/import instability is fixed

Goal: make Unity validation boring, repeatable, and non-destructive.

Quantifiable targets:

- Unity target version is pinned and identical for WismUnity and WismCompanion.
- WismUnity batchmode import succeeds 3 consecutive times from a clean isolated
  project copy.
- Unity Playground smoke succeeds 3 consecutive times for `TestWorld`.
- Unity Playground produces a manifest on every successful run.
- Unity Playground manifest includes run id, Unity version, scene path, world
  name, scenario, status, player count, city count, and log path.
- Direct live-checkout Unity execution is blocked when `Temp\UnityLockfile`
  exists.
- Unity smoke leaves 0 tracked scene, prefab, MOD, or plugin-DLL changes unless
  the work item explicitly authorizes generated artifact updates.

Proof commands:

```powershell
dotnet run --project Wism.DevAgent.Host -- playground unity smoke execute write runId=<id> world=TestWorld scenario=smoke maxTicks=16 isolate=true advanceBootstrap=true timeout-seconds=900
```

Proof artifacts:

- Three Unity Playground `launch-report.json` files.
- Three Unity Playground `manifest.json` files.
- Unity logs for each run.
- Git status before and after the runs.

Failure conditions:

- Any run exits non-zero.
- Any successful run lacks a manifest.
- Any run mutates tracked Unity scene/world data.
- The launcher attempts direct execution against a locked live checkout.

## Milestone T4: WismCompanion Confidence

Target date: before treating Companion as a dependable dev tool

Goal: prove the Companion can observe gameplay without destabilizing tests.

Quantifiable targets:

- WismCompanion has at least 20 automated tests.
- State and log-buffer tests cover at least 90 percent of non-Unity pure C#
  Companion code.
- Transport tests cover SignalR JSON parsing and named-pipe fallback behavior.
- Companion player build succeeds on the pinned Unity version.
- Companion runtime smoke starts and writes a non-empty player log.
- Agent Playground can publish at least 1 campaign to the Companion telemetry
  channel without requiring Companion for core pass/fail.

Proof commands:

```powershell
dotnet test WismClient\Wism.Client.Test\Wism.Client.Test.csproj -c Release -v:minimal
powershell -NoProfile -ExecutionPolicy Bypass -Command "& $env:UNITY_EXE -batchmode -quit -projectPath WismCompanion -executeMethod WismCompanion.Editor.CompanionTestBatch.RunEditModeTests -testResults WismCompanion\Logs\companion-editmode-results.xml -logFile WismCompanion\Logs\companion-editmode-batch.log"
powershell -NoProfile -ExecutionPolicy Bypass -File WismCompanion\Tools\build-companion.ps1
dotnet run --project WismClient\Wism.Agent.Playground -- campaign seed=20260608 clans=2 maxTurns=4 scenario=capture-pressure channel=t4-companion-smoke delayMs=1 --quiet
```

Proof artifacts:

- Companion test runner summary.
- Companion build log.
- Companion player log.
- Agent Playground campaign artifact with telemetry channel metadata.

Current proof:

- `WismCompanion` EditMode tests: 42 discovered, 42 passed, 0 failed,
  0 skipped.
- State, log-buffer, transport, and UI-helper behavior is covered by focused
  EditMode tests; Unity line-coverage instrumentation is not yet wired for the
  numeric 90 percent proof.
- `WismClient` tests: 180 discovered, 180 passed, 0 failed, 0 skipped.
- Companion build log: `WismCompanion\Build\build.log`.
- Companion player log: non-empty startup log under the Unity player log
  location.
- Agent Playground artifact:
  `artifacts\campaigns\campaign-20260608-2clans\campaign.json`, including
  Companion telemetry metadata for channel `t4-companion-smoke`.

Failure conditions:

- Companion has fewer than 20 automated tests.
- Pure state/transport behavior requires Unity scenes to test.
- Companion build fails.
- Companion runtime smoke does not create a non-empty player log.
- Companion becomes a dependency for non-Companion gameplay tests.

## Milestone T5: Scenario And Eval Flywheel

Target date: after T2 and T3 are usable

Goal: turn scenarios into learning evidence and bug-finding pressure.

Quantifiable targets:

- Eval batch runs at least 50 deterministic campaign cases.
- Batch includes at least 5 scenario families:
  capture pressure, ruin search, production economy, road contact, siege or
  city defense.
- Every scenario family has a scenario contract row that names its generator
  mutation, driver mission, required signals, required state invariants, and
  alias status.
- At least 5 scenario families are semantically distinct; documented aliases do
  not count toward the 5-family target.
- Scorecard records counts for crashes, timeouts, validation failures, victories,
  bounded stalemates, city captures, searches, production starts, production
  deliveries, battles, save/load success, and stuck/no-op turns.
- Scorecard counter semantics have focused tests for positive, negative, noisy,
  and duplicate-looking event/moment strings.
- Green scorecard requires:
  - 0 crashes.
  - 0 unclassified timeouts.
  - At least 90 percent parseable case artifacts.
  - At least 1 city capture in capture-focused cases.
  - At least 1 real search in search-focused cases.
  - At least 1 production delivery in production-focused cases.
- Campaign status alone is never sufficient proof; a scenario pass requires
  scorecard gates plus the scenario contract's state invariants.
- Playground regression tests include state-invariant assertions for at least:
  capture ownership, searched-location state, routed production delivery, and
  deterministic replay shape.
- Capture replay tests verify more than package shape: expected command/event
  sequence, final player summaries, and final map or equivalent state digest.
- Capture replay either replays the captured command sequence from the starting
  snapshot or compares a deterministic final-state digest recorded with the
  capture.
- Scenario equivalence coverage includes positive, negative, boundary, and
  artifact-failure cases for campaign, eval, record, jump, world, companion, and
  parallel modes.
- CLI/process coverage proves exit codes and parseable output for sample, win,
  lose, campaign, eval, jump, parallel, record, world, companion, and unknown
  command modes.
- Negative and boundary coverage includes invalid world, invalid mod root,
  missing checkpoint, corrupt checkpoint or manifest, zero and clamped counts,
  oversized turns/clans, bad CSV values, and unwritable output root where the
  host environment can prove it safely.
- Test names and assertions match exactly; no test may claim "each clan" or
  "all players" behavior while asserting only an aggregate or "any player"
  condition.
- Every new failure class creates either a linked bug/work item or an explicit
  no-action record.

Proof commands:

```powershell
dotnet run --project WismClient\Wism.Agent.Playground -- eval seed=20260608 cases=50 maxTurns=12 scenarios=capture-pressure,ruin-search,production-economy,road-contact,siege-defense clans=2,4 sizes=medium --quiet
```

Proof artifacts:

- `eval-run.json`.
- `eval-case-result.jsonl`.
- `scorecard.json`.
- `learning-ledger.jsonl`.
- `eval-summary.md`.
- Playground equivalence-class matrix.
- Scenario contract matrix.
- Capture replay verification report.
- Capture final-state digest report.
- State-invariant assertion inventory for capture, search, production,
  determinism, and eval status gates.
- Counter semantics test report.
- CLI exit-code and output-shape report.
- Negative and boundary test report.

Failure conditions:

- Scorecard is missing or unparseable.
- Required counters are absent.
- Any crash is untriaged.
- Any new failure class lacks a follow-up decision.
- A green result is claimed without meeting scenario-family gates.
- A signal-only test claims rule correctness without a matching state invariant.
- A capture replay verifies only file existence, command counts, or final status.
- An eval-focused test omits `result.Status == "Passed"` when its purpose is to
  prove the scenario or scorecard gate is healthy.
- A named scenario family is counted as unique when it is only an alias for
  another generator path.
- A counter can be satisfied by unrelated substring noise or by accidental
  double-counting between moments and events.
- A CLI mode lacks process-level exit-code proof.
- Campaign status is treated as sufficient proof for a scenario/eval pass.

Current proof:

- Agent Playground eval batch: 50 deterministic cases, 50 passed, 0 failed.
- Scenario families covered: `capture-pressure`, `ruin-search`,
  `production-economy`, `road-contact`, and `siege-defense`.
- Parseable artifacts: 50/50, 100 percent.
- Scorecard gates: 0 crashes, 0 timeouts, capture signal present, search signal
  present, production-delivery signal present.
- Observed signals: 14 victories, 36 bounded stalemates, 152 city captures,
  120 searches, 588 production starts, 20 production deliveries, 360 battles,
  0 stuck/no-op turns.
- Learning ledger: no new failure class observed in the passing batch.
- Current quality assessment: Playground tests provide useful harness and smoke
  coverage, but several are signal-level rather than rule-equivalence tests.
  Known upgrade targets are state assertions for capture/search/production,
  stronger deterministic replay comparison, stricter capture replay equivalence,
  and complete status assertions on eval tests.
- Deeper probe assessment: current Playground coverage is strong at harness,
  artifact, and deterministic pressure, but weaker at semantic equivalence.
  `road-contact` is currently not proven as a distinct generator class, and
  `siege-defense` currently maps through the capture/pressure path unless a
  distinct defense contract proves otherwise. Capture replay verifies the
  envelope and starting snapshot load, but does not yet replay commands or
  compare final-state digest. CLI coverage is currently mostly indirect through
  runner tests, leaving exit-code and bad-input behavior underproved.

## Milestone T6: Agent Development Supportability

Target date: after T5 has produced at least one eval batch

Goal: make agent-driven development safer and easier to review.

Quantifiable targets:

- Every autonomous work item has:
  - target repo classification,
  - scope paths,
  - expected verification commands,
  - public/private boundary classification,
  - rollback or recovery notes,
  - proof artifact requirements.
- At least 90 percent of completed work items include a verification artifact.
- 100 percent of public-repo mutation work items run a public boundary gate.
- 100 percent of Unity mutation work items include pre-change status and
  post-change status.
- 100 percent of scene/world mutations include an explicit backup or isolated
  copy proof.
- Agent-facing failure reports include exact command, exit code, artifact path,
  and reproduction seed when applicable.

Proof artifacts:

- Work item backlog export.
- Verification reports.
- Boundary gate reports.
- Unity launch reports or MCP read-only reports.
- Eval learning ledger.

Failure conditions:

- Any public mutation lacks boundary proof.
- Any Unity scene/world mutation lacks backup/isolation proof.
- Any completed item lacks verification evidence without an explicit waiver.
- Any failure report omits reproduction information.

## Milestone T7: Release Candidate Gate

Target date: before public release tagging

Goal: define a release candidate bar that outsiders can trust.

Quantifiable targets:

- WismClient: 0 failing tests, at least 200 discovered tests.
- WismClient Core: at least 75 percent line coverage and 60 percent branch
  coverage.
- WismUnity: 0 Unity compile errors on pinned Unity version.
- WismUnity PlayMode: at least 30 discovered tests, 0 failures.
- WismCompanion: at least 20 discovered tests, 0 failures.
- Unity Playground: 5 consecutive successful isolated smoke runs.
- Agent Playground: 100-case eval batch with 0 crashes and 0 untriaged
  timeouts.
- Public boundary scan: 0 findings.
- Public repository status is clean at the release commit.

Proof artifacts:

- Release validation report.
- Test summaries for WismClient, WismUnity, and WismCompanion.
- Coverage reports.
- Unity Playground manifests.
- Eval scorecard and learning ledger.
- Boundary scan output.
- Git commit hash and clean status record.

Failure conditions:

- Any target is missed.
- Any proof artifact is missing.
- Any release gate is waived without a written owner and expiration.

## Dashboard Metrics

Track these over time:

- WismClient discovered tests.
- WismClient Core line coverage.
- WismClient Core branch coverage.
- Unity PlayMode discovered tests.
- Unity PlayMode pass rate.
- WismCompanion discovered tests.
- Unity Playground consecutive-pass streak.
- Agent Playground eval cases per batch.
- Crash count per eval batch.
- Timeout count per eval batch.
- Untriaged failure count.
- Public boundary findings.
- Dirty tracked files after validation runs.

## First Backlog Items

1. Split private live-repository health tests from fixture-based unit tests.
2. Fix isolated Unity PackageCache `EPERM` failures before treating Unity
   Playground as a release gate.
3. Replace Unity template EditMode tests with meaningful editor validation.
4. Add WismCompanion state, transport, and bootstrap tests.
5. Add coverage collection and threshold enforcement for WismClient.
6. Build a command-family coverage matrix.
7. Promote Agent Playground campaign artifacts into Eval Hub scorecards.
8. Tighten Playground assertions so every signal test also proves the expected
   state transition.
9. Expand capture replay verification from envelope checks to behavioral
   equivalence checks.
10. Add a Playground equivalence-class matrix covering CLI modes, boundary
    values, negative artifact cases, parallel execution, telemetry-optional
    runs, and manual-parity scenarios.
11. Add a scenario contract matrix and make `road-contact` and `siege-defense`
    either distinct scenario families or documented aliases that do not count
    toward the distinct-family target.
12. Add counter semantics tests for scorecard gates, including noisy substrings
    and moment/event double-count risk.
13. Add process-level CLI tests for success, failure, usage, quiet output, and
    artifact-writing modes.
14. Add capture final-state digest verification, or full command replay from
    the recorded starting snapshot when command replay becomes stable.
15. Add negative and boundary tests for invalid inputs, clamped values, corrupt
    artifacts, and output-write failures where safe to simulate.
