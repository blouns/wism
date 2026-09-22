# Local Performance Diagnostics

Diagnostics are disabled by default. A Development Player supports opt-in
recording with `-wism-profile`; `-wism-profile-output <directory>` chooses the
parent for a unique session folder. Otherwise it uses `Diagnostics` under the
application's persistent-data directory. No network service is required.

During a diagnostic session:

- F9 bookmarks a slowdown and requests a checkpoint at the next AI decision
  boundary. Input-event time and processing time use separately identified clocks.
- F10 arms a binary Unity Profiler capture **before the next AI decision**.
  Up to three one-frame captures are available. Deep Profiling is not enabled.
- Closing the application drains the bounded output queue and writes a receipt.
  Session duration is capped at two hours. Compact JSONL output stops at 256 MiB
  or 80 GiB free disk. Raw evidence should be removed after seven days.

The recorder captures all decisions, nested phase timings, path queries and
expanded nodes, command queue/execution identities, managed allocations, GC
collections, frame intervals and supported main/render-thread counters. Null
means unavailable. `timing-alias` records duplicate an inclusive measurement for
compatibility: never sum aliases with scopes. `ExclusiveMs` on scope records
subtracts measured child scopes. Uninstrumented work remains in its parent.

Checkpoints are captured at a safe, empty-command-queue AI boundary, initially,
every ten turns, and after bookmarks (six total, eight MiB per checkpoint).
Snapshot overhead is measured separately. Checkpoints include the core RNG state
and mod selection; AI-provider-local suppression/history is not persisted.

The Playground accepts core or Unity checkpoints:

```text
Wism.Agent.Playground profile-checkpoint checkpoint=<save.json> out=<new-folder> aiProfile=tactical repetitions=3
```

It reloads the same checkpoint for one warmup and balanced recorder-on/off runs,
comparing generated command outcomes and final state hashes (excluding the save
timestamp). An empty production review and its unneeded renewal can return
`Failed` as a no-op; these are explicitly counted, not silently discarded.
Other failed commands invalidate the comparison. This is one-decision replay,
not proof of full-match parity or Windows rendering performance.

## Limits

Unity omits the optional Companion influence overlay when its socket listener
has no connected viewers. Basic map snapshots remain cached for late joiners;
the overlay resumes with the next snapshot after a viewer connects. Named-pipe
fallback keeps the overlay because its consumer demand is not known. This does
not change the AI's own influence field or simulation state.

The snapshot demand regression test accepts `WISM_SNAPSHOT_BENCHMARK_CHECKPOINT`
to compare overlay-on/off snapshot construction against a saved core or Unity
checkpoint. It verifies unchanged base-map content and game/RNG state. Its timing
output isolates snapshot work, not overall gameplay speed or rendering.

GPU time, repeated-route detection, explicit suppression/replan counters and
isolated animation-wait duration are not yet available. Path routing currently
has no cache, so cache hit/miss rates are not reported as zeros. Logging samples
cover calls through the core information logger, not all Unity internal logging.
Input bookmarks do not measure input-to-present latency.

Raw profiler capture quotas are checked at frame boundaries: a single long frame
can exceed a file-size target. A 16 MiB profiler buffer is **not** a disk quota.
Use short captures with adequate disk reserve; do not claim a hard raw-file cap.
The diagnostic Development Player is not a release-performance baseline.
Recorder overhead, representative early/middle/late campaign evidence and actual
responsiveness still need qualification; instrumentation is not a performance fix.
