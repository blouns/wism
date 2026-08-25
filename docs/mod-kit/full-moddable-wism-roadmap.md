# Full Moddable WISM Roadmap

This roadmap defines the path from today's data-only Mod Kit foundation to a
fully moddable WISM where authors can create, validate, preview, play, and share
new worlds and game variants without forking the engine.

## North Star

A mod author should be able to:

- Create a new world with terrain, cities, locations, starting clans, and
  searchables.
- Customize clans, armies, artifacts, terrain presentation, flavor text, launch
  modes, and scenario setup.
- Validate the mod before opening Unity.
- Preview the mod in WismUnity without mutating scenes by accident.
- Play the mod through WismUnity, AgentPlayground, and other WismClient-based
  clients.
- Package the mod with clear metadata, versioning, dependencies, and asset
  provenance.

Full moddability does not mean arbitrary executable plugins first. The first
stable target is rich data moddability with a versioned validation and preview
pipeline. Code/rule extension can come later behind stricter compatibility and
security boundaries.

## Design Principles

- WismClient owns game rules, stable ids, validation, and mod contracts.
- WismUnity previews, inspects, and deliberately applies authored content.
- A sidecar app creates and validates mods outside the Unity editor.
- Existing Unity editor controls remain manual unless explicitly replaced.
- Every mutation-capable workflow needs preview, backup, apply, and revalidate.
- Existing saves, command contracts, and Unity-loaded binaries stay compatible.
- Public mod artifacts must avoid private notes, local paths, unclear assets, or
  unlicensed third-party content.
- A phase is not Green until the full authoring and runtime path has durable
  E2E proof, including actual Unity verification.

## Proof Standard

The Mod Kit uses a no-watermelon proof rule: do not mark a phase Green if it is
green only at the surface. Each phase needs a working author flow, validation,
runtime load or play evidence, actual Unity verification, and a proof bundle.

The detailed phase gates are defined in the
[Mod Kit E2E Proof Plan](e2e-proof-plan.md). The executable backlog is defined
in [Mod Kit Work Items](work-items.md).

The sidecar map and world-builder direction is defined in the
[Sidecar World Builder Plan](sidecar-world-builder-plan.md).

## Current Marker

Current marker: V1 data-pack foundation plus sidecar roadmap.

- `classic-warlords` profile exists.
- Visual, flavor, and mode feature packs exist.
- `ModKitValidator` validates current feature-pack shape.
- AgentPlayground and Unity Playground can record profile and pack selections.
- Unity sidecar direction is documented, with read-only-first safety gates.

## Maturity Levels

### M0: Legacy MOD Data

Status: historical baseline.

- WISM loads JSON data for core game concepts.
- Unity scenes and MOD JSON are coupled by convention.
- Editor-time controls exist but are manual and easy to misuse.

### M1: Data-Pack Foundation

Status: current.

- Profiles and feature packs are versioned concepts.
- Proof packs cover visual, flavor, and launch-mode scenarios.
- Validator catches common pack authoring mistakes.
- Docs and templates exist for data-only pack creation.

Exit criteria:

- Pack author can copy a template, edit JSON, run focused validation, and launch
  a smoke test.
- Unity proof records the selected profile and packs without dirty scene churn.

### M2: Author Feedback And Unity Read-Only Bridge

Status: next.

- Add a CLI validation command that does not require NUnit.
- Add explicit profile/pack validation reports.
- Add a read-only Unity Mod Kit report.
- Add scene-vs-MOD drift reporting for cities and locations.
- Add Unity-side settings for selected profile/packs without changing defaults.

Exit criteria:

- A sidecar or command line can ask, "Is this mod valid, and what would Unity
  load?" and receive a structured report.
- The report is proven through actual Unity read-only execution.

### M3: Sidecar Pack And World Authoring

Status: planned.

- Sidecar app creates and edits feature packs.
- Sidecar app creates world draft folders from templates.
- World validator checks terrain, city placement, locations, ownership, starting
  viability, reachability, and loadability.
- Unity previews selected mods without auto-saving scenes.
- Mutation workflows require preview and backup.

Exit criteria:

- A user can create a small playable world and launch it in WismUnity without
  hand-editing Unity scene state first.
- The sidecar-created mod has CLI, AgentPlayground, and Unity proof artifacts.

### M4: Scenario, Asset, And Campaign Modding

Status: planned.

- Scenario builder defines players, clans, starts, alliances, seed, turn bounds,
  and victory conditions.
- Asset import pipeline handles sprites, palettes, icons, and provenance.
- Campaign packs combine worlds, scenarios, and presentation.
- AgentPlayground can run campaign smoke and coverage checks against mods.

Exit criteria:

- A user can build a themed playable mod pack with custom presentation, multiple
  scenarios, validation proof, and repeatable smoke tests.
- At least one bounded campaign smoke and one Unity launch or preview proof are
  captured for the package.

### M5: Stable Mod Ecosystem

Status: long-term.

- Versioned package format for mods.
- Compatibility matrix across WismClient, WismUnity, Companion, and
  AgentPlayground.
- In-game or launcher-level mod selection.
- Upgrade/migration validators for older mods.
- Public examples, contribution guide, and release checklist.
- Optional rule-extension model evaluated separately from data mods.

Exit criteria:

- Mods can be shared, installed, validated, selected, played, and upgraded with
  predictable behavior across supported WISM clients.
- Compatibility claims include proof across WismClient, WismUnity, Companion,
  and AgentPlayground.

## Workstreams

### Mod Contracts

Owns schemas and stable data shapes.

- Version profile, pack, world, scenario, and campaign manifests.
- Define stable ids and allowed override surfaces.
- Add compatibility and minimum-engine fields.
- Preserve old JSON loading where possible.

### Validation

Owns safety and author feedback.

- Expand `ModKitValidator` from feature packs to worlds and scenarios.
- Add structured report output for CLI, Unity, and sidecar use.
- Validate dependencies, conflicts, asset references, and provenance.
- Keep validation deterministic and runnable in CI.

### Unity Bridge

Owns Unity visibility and safe preview.

- Add read-only Mod Kit status report.
- Add selected profile/pack settings.
- Apply selection before game creation.
- Compare scene objects against MOD JSON.
- Keep scene mutation explicit and recoverable.

### Sidecar App

Owns author workflow.

- Create and edit manifests.
- Browse stable ids.
- Run validation.
- Preview reports from Unity when available.
- Package mods with metadata and provenance.
- Host the map canvas and world-authoring workflow described in the
  [Sidecar World Builder Plan](sidecar-world-builder-plan.md).

### World Kit

Owns playable world creation.

- Generate world templates.
- Validate map dimensions and terrain.
- Validate city 2x2 placement and ownership.
- Validate location/searchable placement.
- Validate start viability and reachability.
- Support deterministic seeds for generated drafts.
- Feed sidecar inspection, draft generation, map editing, and Unity preview.

### Scenario And Campaign Kit

Owns playable setup beyond static maps.

- Define player/clan selection.
- Define starting cities and armies.
- Define mode presets and bounded turns.
- Define victory and stalemate conditions.
- Add AgentPlayground campaign smoke for mods.

### Asset Kit

Owns presentation assets.

- Define supported sprite/icon/palette formats.
- Track asset provenance.
- Validate referenced files exist and are importable.
- Keep copied or unclear third-party assets out of public examples.

### Distribution

Owns sharing and install shape.

- Define package folder layout.
- Add manifest metadata: id, display name, version, author, license, engine
  compatibility, dependencies, conflicts.
- Add install and uninstall guidance.
- Add upgrade checks before older mods are loaded.

## Recommended Sequence

1. Build `mod-kit validate` as a command-line entry point.
2. Build the shared proof harness and artifact layout.
3. Add Unity read-only Mod Kit report.
4. Add Unity selection settings and apply profile/packs before game creation.
5. Add world validation as a first-class contract.
6. Add sidecar app skeleton around validation and template editing.
7. Add scene-vs-MOD drift report.
8. Add world draft creation and preview.
9. Add scenario/campaign manifests and validation.
10. Add asset provenance/import validation.
11. Define package/install format and compatibility matrix.

## Near-Term Work Items

The canonical, phase-gated work-item definitions live in
[Mod Kit Work Items](work-items.md). The summary below captures the immediate
sequence.

### WISM-MODKIT-E2E-001

Create the proof harness and artifact layout used to close Mod Kit work items.

Acceptance:

- Produces `proof-summary.json`.
- Captures commands, git SHA, validation, AgentPlayground, and Unity evidence.
- Missing Unity proof marks the item Yellow rather than Green.

### WISM-MODKIT-CLI-001

Create a public command-line validation entry point for profiles, packs, and
selected combinations.

Acceptance:

- Runs without NUnit.
- Emits human-readable and JSON report output.
- Returns nonzero on validation errors.
- Uses `ModKitValidator` rather than duplicating rules.
- Produces evidence consumable by the shared proof harness.

### WISM-MODKIT-UNITY-001

Add a read-only Unity Mod Kit status report.

Acceptance:

- Reports current mod root, profiles, packs, selected profile/packs, validator
  status, active scene, and dirty-scene state.
- Does not save, reset, import, export, or mutate scene objects.
- Returns structured data through the WismUnity Production Pipeline command.
- Captures report JSON, Unity console evidence, and dirty-scene state.

### WISM-MODKIT-UNITY-002

Add Unity profile/pack selection settings and runtime application.

Acceptance:

- Defaults preserve current `Assets\Mod` behavior.
- Selected profile/packs set `GameManager`, `ModFactory`, and cache state before
  game creation.
- Zero-pack and proof-pack Unity smoke tests pass.
- Smoke manifests record selected profile, packs, world, and scene state.

### WISM-WORLDKIT-001

Add world validation contract.

Acceptance:

- Validates map, cities, locations, placement, reachability, ownership, and
  loadability.
- Produces actionable issues.
- Runs outside Unity.
- Valid worlds also load through AgentPlayground and preview or report through
  Unity.

### WISM-SIDECAR-001

Create sidecar app skeleton around existing validation and templates.

Acceptance:

- Opens a mod workspace.
- Lists profiles and packs.
- Runs validation.
- Creates a new feature pack from templates.
- Does not require Unity to be open.
- Can request or import Unity read-only proof when available.

## Key Risks

- Unity scene state and MOD JSON can drift.
- Current editor reset/import controls can mutate valuable scene state.
- Debug builds can republish Unity plugin DLLs as incidental churn.
- Full code-plugin moddability would introduce compatibility and security risks.
- Asset provenance must be clean before public examples expand.

## Definition Of Done

Full moddable WISM is done when a non-engine contributor can create a new mod,
validate it, preview it in Unity, play it through WismUnity, smoke it through
AgentPlayground, package it, and share it with clear compatibility and
provenance, without modifying WismClient or WismUnity source code.
