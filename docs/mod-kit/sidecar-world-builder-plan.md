# Sidecar World Builder Plan

This plan defines how the Mod Kit sidecar grows from validation and pack
authoring into a full map and world builder. The sidecar should let authors
create, inspect, validate, preview, play, and package worlds without making
WismUnity scenes the source of truth.

## Goal

A mod author should be able to build a complete playable world from the
sidecar:

- Draw or generate terrain.
- Place cities, locations, searchables, roads, bridges, and starting armies.
- Assign clan ownership and starting conditions.
- Validate reachability, placement, ownership, and loadability.
- Preview in WismUnity without accidental scene mutation.
- Run AgentPlayground smoke and campaign checks.
- Package the world with metadata, provenance, and compatibility proof.

## Ownership Boundaries

- `WismClient` owns world schemas, stable ids, validation, deterministic
  generation, loadability checks, and scenario/campaign contracts.
- `Wism.ModKit.Cli` exposes validation, generation, proof, and package commands
  for automation and sidecar use.
- The sidecar owns the author workflow, editing surface, preview queue, proof
  dashboard, and package flow.
- `WismUnity` owns read-only visual preview first, then explicit preview,
  backup, apply, and revalidate flows for scene-affecting operations.
- AgentPlayground owns playable smoke, campaign coverage, and regression
  captures for generated worlds.

The first stable source of truth is data under the mod workspace. Unity scenes
are previews or deliberately applied outputs, not the primary authoring record.

## Authoring Data Model

The sidecar should operate on a versioned workspace:

```text
<mod-workspace>/
  mod.json
  profiles/
  feature-packs/
  worlds/<world-id>/
    world.json
    Map.json
    City.json
    Location.json
    Scenario.json
    Campaign.json
    presentation/
    assets/
  proofs/
```

V1 can preserve existing WISM JSON filenames while adding optional manifests
around them. New manifests should be additive and versioned.

Core contracts:

- `world.json`: id, display name, dimensions, base rules profile, author,
  license, seed, compatibility, notes.
- `Map.json`: terrain grid with explicit terrain ids and optional road/bridge
  overlays.
- `City.json`: 2x2 city footprint, owner, income, production options, defense,
  stable id.
- `Location.json`: ruins, temples, libraries, tombs, sages, searchables, and
  rewards or encounter hints.
- `Scenario.json`: clans, human/computer flags, starts, initial armies,
  alliances, fog/settings, turn bound, victory mode.
- `Campaign.json`: ordered scenarios, branching metadata, or campaign pack
  grouping.

## Builder Modes

### Inspect

Read an existing world and show:

- Dimensions, terrain mix, city count, location count, clan starts.
- Validation issues and warnings.
- Reachability graph between starts, cities, and searchables.
- Scene-vs-MOD drift from Unity read-only reports when available.

This mode is read-only and should be the first shippable sidecar world-builder
surface.

### Draft

Create a deterministic world draft from templates and a seed:

- Small, medium, and large map sizes.
- Terrain bands and regions.
- Road skeletons and bridge placement.
- Legal city footprints.
- Minimum clan starts.
- Required searchables.

The draft generator should optimize for valid, playable worlds rather than
beautiful maps. Author editing and later balancing can improve the result.

### Edit

Edit world data in the sidecar:

- Paint terrain and overlays.
- Place and move city footprints.
- Place and move locations/searchables.
- Assign owners and start cities.
- Add starting armies and production options.
- Edit display metadata and provenance.

Every edit updates the validation report incrementally. Invalid intermediate
states are allowed while editing, but cannot be packaged or marked playable.

### Balance

Compute map and scenario quality signals:

- Start distance and contact time.
- Terrain accessibility and choke points.
- City income distribution.
- Reachable neutral cities/searchables.
- Clan survival spread in campaign smoke.
- Expected turns to first battle and first capture.

Balance checks should produce suggestions, not automatic rewrites in V1.

### Preview

Send the selected world to WismUnity for read-only preview:

- Report what Unity would load.
- Render or launch without saving scenes.
- Return console errors, warnings, dirty scenes, selected profile/packs, and
  world summary.
- Link proof artifacts back into the sidecar.

Mutation-capable Unity operations remain a later explicit workflow.

### Apply

Apply is allowed only after preview is mature:

1. Validate.
2. Preview changed files and scene impact.
3. Create backup bundle.
4. Apply.
5. Revalidate.
6. Run Unity proof.
7. Save recovery instructions.

Apply must never be hidden behind an automatic preview or smoke command.

## Validation Contract

`WISM-WORLDKIT-001` should become the contract used by CLI, sidecar,
AgentPlayground, and Unity reports.

Required validation categories:

- Schema and JSON parseability.
- Terrain references and map dimensions.
- City 2x2 placement, overlap, owner, income, and production references.
- Location/searchable placement, overlap, and terrain legality.
- Clan start viability: every active clan has a legal city and army stack.
- Reachability through contiguous legal paths.
- Roads and bridges connect legal terrain.
- Map size is within supported bounds.
- Stable ids are unique and reference valid game data.
- Loadability through existing WismClient factories.
- Compatibility with WismUnity plugin data profile.

Validation output should include:

- `schemaVersion`
- `status`
- `worldId`
- `issueCount`
- `issues[]` with severity, code, message, path, and coordinate when available
- `coverage` with city/location/start/reachability counts
- `proofHints` naming the next useful smoke or preview command

## Sidecar Architecture

Use a thin UI over durable command and service contracts:

- `Wism.ModKit.Core`: reusable world contracts, validators, generators,
  report models, and package rules.
- `Wism.ModKit.Cli`: command surface for validate, generate, preview request,
  proof, and package.
- `Wism.ModKit.Sidecar`: desktop authoring app.
- `WismUnity` bridge: read-only status and preview adapters.
- `AgentPlayground`: campaign smoke and replay capture adapter.

The sidecar should call the same validators and generators as CLI tests. It
should not fork map rules or invent Unity-only validation.

## UX Shape

Recommended sidecar layout:

- Left rail: Workspace, Profiles, Packs, Worlds, Scenarios, Campaigns, Proofs.
- Center: map canvas with zoom, pan, grid, terrain brush, object layers.
- Right inspector: selected tile/city/location/scenario properties.
- Bottom panel: validation issues, proof runs, command log, and artifact links.
- Top toolbar: Validate, Generate Draft, Preview in Unity, Run Smoke, Package.

Map canvas requirements:

- Layer toggles for terrain, roads, cities, locations, armies, starts, and
  validation overlays.
- Minimap for large maps.
- Coordinate readout.
- Undo/redo for sidecar data edits.
- Brush palette for terrain and overlays.
- Object palette for cities, locations, searchables, and starts.

## Phased Roadmap

### Phase 1: World Validation Contract

Deliver `WISM-WORLDKIT-001`.

- Add world validation models and report output.
- Validate known-good and deliberately invalid worlds.
- Expose validation through CLI JSON.
- Keep Unity read-only and unchanged.

Proof:

- Focused WismClient tests.
- CLI validation of known-good and invalid worlds.
- AgentPlayground load of a valid world.
- Unity read-only report or status proof for the valid world.

### Phase 2: Sidecar Shell

Deliver the first sidecar app skeleton.

- Open a mod workspace.
- List profiles, packs, and worlds.
- Run validation.
- Show structured issues.
- Import Unity read-only report artifacts.
- Create proof bundles.

Proof:

- Sidecar opens a temporary workspace.
- Validation displays pass and fail states.
- CLI and sidecar reports match.
- Unity status proof is linked when available.

### Phase 3: Draft Generator

Deliver deterministic world draft creation.

- Generate small and medium playable worlds from seed.
- Place terrain, cities, locations, starts, and searchables.
- Save existing WISM JSON plus `world.json`.
- Validate immediately after generation.

Proof:

- Same seed produces same files.
- Generated world validates.
- AgentPlayground loads it.
- Unity previews or reports it without dirty scenes.

### Phase 4: Map Editor

Deliver interactive sidecar editing.

- Terrain paint.
- City and location placement.
- Owner/start assignment.
- Incremental validation.
- Undo/redo.
- Save workspace changes.

Proof:

- Create or edit a world by UI flow.
- Validation catches an intentional invalid placement.
- Fix the issue through UI.
- AgentPlayground smoke passes.
- Unity status proof passes.

### Phase 5: Scenario And Campaign Builder

Deliver scenario/campaign setup on top of worlds.

- Player/clan selection.
- Initial armies and starts.
- Victory and stalemate bounds.
- Scenario manifests.
- Campaign manifests.

Proof:

- Generated scenario validates.
- AgentPlayground bounded campaign smoke runs.
- Unity launches the selected scenario or reports unsupported details
  precisely.

### Phase 6: Unity Preview And Apply

Deliver mature Unity integration.

- Read-only preview is default.
- Scene-vs-MOD drift report is visible in sidecar.
- Apply is explicit and backup-backed.
- Revalidation is mandatory after apply.

Proof:

- Preview produces no dirty scenes.
- Apply creates backup and changed-file audit.
- Revert/recovery instructions work.
- Unity proof passes after apply.

### Phase 7: Packaging And Sharing

Deliver stable package workflow.

- Package layout and manifest.
- Compatibility metadata.
- Provenance and license checklist.
- Install/uninstall validation.

Proof:

- Package a sidecar-created world.
- Install into a temporary mod root.
- Validate, AgentPlayground smoke, and Unity smoke pass.
- Uninstall leaves no unmanaged source changes.

## Work Items To Add

### WISM-WORLDKIT-003: Sidecar World Inspection

Build the read-only sidecar world inspection page over `WISM-WORLDKIT-001`.

### WISM-WORLDKIT-004: Deterministic Draft Generator

Generate small and medium playable worlds from seed and templates.

### WISM-WORLDKIT-005: Sidecar Map Canvas

Add the interactive map canvas, layer toggles, minimap, object palette, and
incremental validation overlays.

### WISM-WORLDKIT-006: Sidecar Scenario Builder

Add scenario setup for clans, starts, armies, turn bounds, and victory mode.

### WISM-WORLDKIT-007: Unity Preview Adapter

Connect sidecar preview requests to WismUnity read-only status and smoke
manifests.

### WISM-WORLDKIT-008: Preview, Backup, Apply

Add mutation-capable Unity apply workflow with preview, backup, revalidation,
changed-file audit, and recovery instructions.

## Demo Ladder

Each phase should have a visible demo:

1. Inspect an existing world and show validation/reachability.
2. Generate a seeded world and prove it loads.
3. Edit terrain/city placement and watch validation update.
4. Preview the world in Unity without dirty scenes.
5. Run a bounded campaign smoke from the sidecar.
6. Package and reinstall the world into a temporary mod root.

## Open Design Questions

- Which desktop stack should the sidecar use first: WinForms/WPF for speed,
  Avalonia for cross-platform, or a local web UI for rich canvas tooling?
- Should large-map editing store sparse patches or full terrain grids?
- Which Unity preview should come first: status-only report, screenshot render,
  or interactive launch?
- How much balancing should be deterministic scoring versus agent-assisted
  suggestions?
- What is the first public example world we are comfortable shipping?
