# Mod Kit Work Items

These work items turn the Mod Kit roadmap into executable slices. Each item has
an author flow, a Unity proof requirement, and a durable evidence requirement.

## WISM-MODKIT-E2E-001: Proof Harness

Status: implemented for CLI validation, AgentPlayground smoke, Unity manifest
ingestion, and Red/Yellow/Green proof summaries.

Objective:

Create the common proof harness and artifact layout used by all Mod Kit phases.

Implementation shape:

- Add a repeatable command or script that creates a proof bundle directory.
- Capture command lines, git SHA, selected profile, enabled packs, world, test
  logs, CLI reports, AgentPlayground reports, and Unity smoke manifests.
- Keep artifacts outside source folders unless a small checked-in fixture is
  deliberately added.

Acceptance:

- A single work item can produce `proof-summary.json`.
- Proof summary links to all generated evidence files.
- Missing Unity proof marks the item Yellow, not Green.
- Harness records skipped checks with explicit reasons.

E2E proof:

- Run a zero-pack proof bundle.
- Run a proof-pack proof bundle.
- Verify both include WismClient, AgentPlayground, and Unity evidence.

## WISM-MODKIT-CLI-001: Command-Line Validation

Status: implemented as `Wism.ModKit.Cli validate`.

Objective:

Create a public command-line validation entry point for profiles, packs, and
selected combinations.

Implementation shape:

- Reuse `ModKitValidator` and profile catalog loading.
- Support human-readable output for authors.
- Support JSON output for sidecar and Unity tooling.
- Return nonzero on validation errors.

Acceptance:

- Runs without NUnit.
- Emits human-readable and JSON report output.
- Validates explicit profile and pack selections.
- Returns nonzero on validation errors.
- Does not duplicate validator rules.

E2E proof:

- Validate `classic-warlords` with zero packs.
- Validate `classic-warlords` with proof packs.
- Validate a deliberately invalid pack and capture actionable diagnostics.
- Include proof output in the common proof bundle.

## WISM-MODKIT-UNITY-001: Unity Read-Only Mod Kit Report

Status: implemented as `WismUnity.GetModKitStatus`, the shared Unity Mod Kit
selection inspection helper, and the Unity Mod Kit Control Room editor window.

Objective:

Add a read-only Unity Mod Kit report that sidecar and editor tooling can call
without mutating scenes.

Implementation shape:

- Add a Unity editor menu report.
- Expose the same report through the Unity tooling bridge when available.
- Report current mod root, profiles, packs, selected profile/packs, validator
  status, active scene, dirty-scene state, and scene-vs-MOD drift.
- Keep response shapes structured and versioned.

Acceptance:

- Report works with the current default `Assets\Mod` behavior.
- Report works with `classic-warlords` and proof packs.
- Report does not save, reset, import, export, or mutate scene objects.
- Dirty scene state is visible in the report.
- Missing optional tooling produces a clear unavailable status.

E2E proof:

- Run report in Unity against zero packs.
- Run report in Unity against proof packs.
- Capture Unity console output and report JSON.
- Verify `dirtyScenes` does not grow because of the report.

## WISM-MODKIT-UNITY-002: Unity Selection And Launch

Status: implemented for Unity Playground explicit profile/pack selection while
preserving default `Assets\Mod` behavior when no selection is provided.

Objective:

Add Unity profile/pack selection settings and apply them before game creation.

Implementation shape:

- Store `modRoot`, `profileId`, `enabledPackIds`, and optional `worldId`.
- Preserve current defaults when settings are unset.
- Apply selection before `UnityGameFactory.CreateGame()`.
- Set `GameManager.ModPath`, `GameManager.WorldName`,
  `ModFactory.ModPath`, and `ModFactory.ActiveFeaturePackIds`.
- Reset `ModFactory` caches after selection changes.

Acceptance:

- Default Unity launch behavior is unchanged.
- Zero-pack Unity smoke passes.
- Proof-pack Unity smoke passes.
- Smoke manifest records selected profile and enabled packs.
- No scene save is required to launch a selected pack.

E2E proof:

- Run isolated Unity smoke for default settings.
- Run isolated Unity smoke for `classic-warlords` plus proof packs.
- Capture manifest, console log, and dirty-scene state.

## WISM-WORLDKIT-001: World Validation Contract

Status: implemented for WismClient and `Wism.ModKit.Cli world validate`;
Unity read-only preview/report integration remains in WISM-WORLDKIT-003 and
WISM-WORLDKIT-007.

Objective:

Make world validity a first-class, reusable contract before sidecar world
authoring mutates or generates data.

Implementation shape:

- Validate map dimensions and terrain references.
- Validate city 2x2 placement and overlap.
- Validate location and searchable placement.
- Validate clan ownership and start viability.
- Validate reachability between starting cities, cities, and important
  searchables.
- Validate loadability through existing factories.

Acceptance:

- Runs outside Unity.
- Produces actionable issue codes and messages.
- Supports JSON report output.
- Can be called by CLI, sidecar, AgentPlayground, and Unity report tooling.

E2E proof:

- Validate at least one known-good world.
- Validate deliberate invalid worlds for placement, overlap, ownership, and
  start viability.
- Load the valid world through AgentPlayground.
- Preview or report the valid world through Unity without scene mutation.

Current proof:

- `dotnet test Wism.Client.Test\Wism.Client.Test.csproj -c Release --filter WorldKitValidatorTests`
- `dotnet run --project Wism.ModKit.Cli -- world validate world=TestWorld players=2 clans=Sirians,LordBane --json`
- `dotnet run --project Wism.ModKit.Cli -- world validate world=TestWorld clans=Sirians,StormGiants`

## WISM-SIDECAR-001: Sidecar Skeleton

Objective:

Create the first sidecar app skeleton for Mod Kit author workflows.

Implementation shape:

- Open a mod workspace.
- List profiles and feature packs.
- Create a new feature pack from templates.
- Run CLI validation and show structured results.
- Create proof bundles for sidecar actions.
- Detect whether Unity proof is available, unavailable, or blocked.

Acceptance:

- Sidecar does not require Unity to be open for basic authoring.
- Sidecar can create a valid feature pack from templates.
- Sidecar can run validation and display errors.
- Sidecar can request or import a Unity read-only report when available.
- Sidecar cannot save Unity scenes or mutate Unity project data in V1.

E2E proof:

- Create a new pack under the selected source mod root.
- Validate it with the CLI.
- Load it through AgentPlayground.
- Run Unity read-only report or Unity smoke against the generated pack.
- Save proof bundle with created files, reports, and Unity evidence.

## WISM-WORLDKIT-002: World Draft Creation

Objective:

Add sidecar and CLI support for creating a small playable world draft.

Implementation shape:

- Generate deterministic template worlds from a seed.
- Include map, cities, locations, clan starts, and basic ownership.
- Keep generated worlds small enough for quick smoke tests.
- Leave terrain editing and advanced balancing for later work.

Acceptance:

- Same seed recreates the same draft.
- Generated draft passes world validation.
- Every active clan has a legal start.
- Generated draft can be loaded by existing factories.

E2E proof:

- Generate a world draft from a fixed seed.
- Validate it.
- Run AgentPlayground smoke.
- Preview or report it in Unity.

## WISM-WORLDKIT-003: Sidecar World Inspection

Objective:

Add the first read-only sidecar world-builder surface over the world validation
contract.

Implementation shape:

- Open a mod workspace.
- List worlds and scenario files.
- Show map dimensions, terrain mix, city count, location count, clan starts,
  validation status, and reachability summary.
- Import or request Unity read-only status reports when available.
- Link proof artifacts for each inspected world.

Acceptance:

- Does not mutate world data or Unity scenes.
- Uses the same validation contract as CLI and tests.
- Shows actionable validation issues with coordinates when available.
- Can compare sidecar world data to Unity read-only report output.

E2E proof:

- Inspect one known-good world.
- Inspect one deliberately invalid world.
- Verify sidecar and CLI validation reports agree.
- Capture Unity status proof for the known-good world.

## WISM-WORLDKIT-004: Deterministic Draft Generator

Objective:

Generate valid small and medium playable worlds from templates and a seed.

Implementation shape:

- Generate terrain, cities, locations, searchables, clan starts, and initial
  ownership.
- Save existing WISM JSON files plus optional `world.json` metadata.
- Validate immediately after generation.
- Keep output deterministic for the same seed and template.

Acceptance:

- Same seed recreates the same world files.
- Generated worlds pass validation.
- Every active clan has a legal start.
- Generated worlds load through existing WismClient factories.

E2E proof:

- Generate a world from a fixed seed.
- Validate it through CLI and sidecar.
- Load it through AgentPlayground.
- Preview or report it through Unity without dirty scenes.

## WISM-WORLDKIT-005: Sidecar Map Canvas

Objective:

Add interactive map editing to the sidecar.

Implementation shape:

- Add zoom, pan, minimap, coordinate readout, terrain brush, object palette,
  layer toggles, and selection inspector.
- Support terrain, road, bridge, city, location, searchable, start, and army
  layers.
- Add undo and redo for sidecar data edits.
- Run incremental validation after edits.

Acceptance:

- Invalid intermediate states are visible but cannot be marked playable.
- Validation overlays point to placement and reachability issues.
- Save writes only sidecar world data unless an explicit Unity apply workflow
  is invoked.
- Unity scene mutation remains unavailable in this item.

E2E proof:

- Edit terrain and object placement through the sidecar.
- Create and then fix a validation issue.
- Save the world.
- Run CLI validation, AgentPlayground smoke, and Unity read-only proof.

## WISM-WORLDKIT-006: Sidecar Scenario Builder

Objective:

Add scenario setup for playable worlds.

Implementation shape:

- Edit clans, human/computer flags, starts, initial armies, alliances, turn
  bounds, and victory mode.
- Validate each active clan has legal start conditions.
- Save scenario manifests next to the world.
- Feed scenario selection into AgentPlayground smoke.

Acceptance:

- Scenario manifests are versioned and additive.
- Invalid starts or missing cities block playable status.
- Bounded campaign smoke can run from generated scenario data.

E2E proof:

- Create a scenario for a generated world.
- Validate it.
- Run bounded AgentPlayground campaign smoke.
- Run Unity launch or read-only unsupported-feature report.

## WISM-WORLDKIT-007: Unity Preview Adapter

Objective:

Connect sidecar preview requests to WismUnity read-only status and smoke
manifests.

Implementation shape:

- Request or import Unity Mod Kit status reports.
- Request Unity Playground smoke when Unity proof is available.
- Display console errors, warnings, dirty scenes, selected profile/packs, and
  world summary.
- Store preview artifacts under the sidecar proof bundle.

Acceptance:

- Preview is read-only by default.
- Missing Unity availability is reported as unavailable, not failure.
- Dirty scenes or console errors block Green proof.

E2E proof:

- Preview a sidecar-created world.
- Capture Unity status and runtime manifests.
- Verify zero dirty scenes and zero console errors.

## WISM-WORLDKIT-008: Preview, Backup, Apply

Objective:

Add mutation-capable Unity apply workflow after read-only preview is mature.

Implementation shape:

- Validate.
- Preview changed files and scene impact.
- Create backup bundle.
- Apply approved changes.
- Revalidate.
- Capture changed-file audit and recovery instructions.

Acceptance:

- Apply is explicit and cannot be triggered by preview or smoke.
- Backups are created before mutation.
- Recovery instructions are written with every apply proof.
- Revalidation is mandatory after apply.

E2E proof:

- Apply a small world change to an isolated Unity project copy.
- Verify changed-file audit and backup.
- Revalidate and run Unity proof.
- Restore from backup or verify recovery instructions.

## WISM-MODKIT-PACKAGE-001: Package And Compatibility Matrix

Objective:

Define the durable package/install shape for sharing mods.

Implementation shape:

- Define package folder layout.
- Add compatibility metadata.
- Add install and uninstall checks.
- Add provenance and license checklist.

Acceptance:

- Package validation detects missing manifests and missing assets.
- Compatibility matrix names supported WISM client surfaces.
- Public example package can be installed and removed cleanly.

E2E proof:

- Package a proof mod.
- Validate package.
- Install into an isolated test copy of the source mod root.
- Launch through AgentPlayground and Unity.
- Uninstall and verify no unmanaged source changes remain.
