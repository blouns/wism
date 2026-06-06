# WISM Mod Kit Journey

This roadmap tracks the Mod Kit path from safe data-only packs toward a Unity
sidecar authoring workflow. The guiding rule is that WismClient owns stable
game data and validation, while WismUnity previews, inspects, and deliberately
applies authored content.

For the long-term target and maturity model, see the
[Full Moddable WISM Roadmap](full-moddable-wism-roadmap.md).

For the proof standard used to close each phase, see the
[Mod Kit E2E Proof Plan](e2e-proof-plan.md). For the executable backlog, see
the [Mod Kit Work Items](work-items.md).
For the full sidecar map and world-builder plan, see the
[Sidecar World Builder Plan](sidecar-world-builder-plan.md).

## Current Marker

Current marker: V1 data-pack foundation.

- Profiles and feature packs are supported in WismClient.
- `classic-warlords` is the first profile.
- Visual, flavor, and mode packs are represented as JSON manifests.
- `ModKitValidator` validates pack shape, stable id references, dependencies,
  conflicts, referenced files, and launch worlds.
- AgentPlayground and Unity Playground can record profile and pack selections.
- The Unity Mod Kit Control Room gives authors a read-only editor status and
  demo surface.
- Existing Unity editor-time city/location controls were not changed by the
  modular-pack work.

The next marker is not reached until the relevant work item has a full E2E
proof bundle, including actual Unity verification. A command that merely runs
or launches Unity is not enough.

## Unity Sidecar Direction

The intended sidecar should create and validate mod content outside the Unity
editor, then ask Unity to preview or apply it through explicit, reviewable
operations. Unity should not become the source of truth for game rules or pack
semantics.

The sidecar-facing Unity surface should start read-only:

- Report current mod root, available profiles, available packs, selected
  profile, selected packs, and validator status.
- Report active scene, `UnityManager`, `WorldTilemap`, `Cities`, and
  `Locations` status.
- Compare scene-derived city/location objects against MOD JSON.
- Report whether any loaded scenes are dirty.
- Avoid scene saves, resets, imports, exports, or data writes unless the action
  is explicitly mutation-capable and approved.

## Unity Touches Needed

1. Add a Unity Mod Kit status surface.
   - Editor menu and MCP-compatible read-only report.
   - Include profile/pack catalog, validation result, scene summary, and
     scene/MOD drift summary.

2. Add Unity-side selection settings.
   - Store `modRoot`, `profileId`, `enabledPackIds`, and optional `worldId`.
   - Default to today's behavior when unset.
   - Keep settings separate from scene data unless the user deliberately saves
     scene changes.

3. Apply profile and pack selection before game creation.
   - Set `GameManager.ModPath`.
   - Set `GameManager.WorldName`.
   - Set `ModFactory.ModPath`.
   - Set `ModFactory.ActiveFeaturePackIds`.
   - Call `ModFactory.ResetCache()` after selection changes.

4. Preserve existing editor controls.
   - City and location reset toggles remain manual.
   - Tilemap import toggles remain manual.
   - `Tools/Export World` remains explicit and should gain validation before
     writing.
   - No automated scene save from sidecar, MCP, or Playground code.

5. Add preview-before-mutation workflow.
   - Validate.
   - Preview changed files and scene impact.
   - Back up affected scene/MOD files.
   - Apply.
   - Revalidate.

## Phases

### V1A: Data-Pack Foundation

Status: complete.

- Profile and feature-pack manifests.
- Proof packs.
- Pack selection accepted by Playground surfaces.
- Mod Kit templates and authoring docs.
- `ModKitValidator` and focused tests.

### V1B: Author Feedback

Status: next.

- Add a command-line validation entry point.
- Validate an explicit profile and pack selection without requiring NUnit.
- Print friendly validation reports for pack authors.
- Keep the validator usable by Unity and sidecar tools.
- Produce a proof bundle with CLI validation, focused tests, and the applicable
  Unity report or explicit Unity-blocked status.

### V1C: Unity Read-Only Bridge

Status: planned.

- Add a read-only Unity Mod Kit report.
- Show selected profile/packs and resolved launch world.
- Show validator results inside Unity-facing tooling.
- Compare scene objects against MOD JSON without changing scenes.
- Prove read-only behavior with Unity report output, console evidence, and
  dirty-scene state.

### V1D: Unity Selection Plumbing

Status: planned.

- Add explicit profile/pack selection settings in Unity.
- Apply selection before `UnityGameFactory.CreateGame()`.
- Preserve current default `Assets\Mod` behavior.
- Validate Unity startup with zero packs and with proof packs.
- Capture Unity smoke manifests for both zero-pack and proof-pack launch flows.

### V2: World Kit

Status: planned.

- Validate worlds as units: map, cities, locations, reachability, ownership,
  start viability, and JSON loadability.
- Add world templates.
- Add read-only scene-vs-MOD drift reports.
- Add sidecar world inspection, deterministic draft generation, and eventually
  an interactive map canvas.
- Prove valid worlds through validator, AgentPlayground load, and Unity preview
  or Unity read-only report.

### V3: Sidecar Authoring

Status: planned.

- Sidecar creates or edits data-only packs.
- Unity previews pack and world impact.
- Mutation-capable workflows require preview, backup, apply, and revalidate.
- Scene mutation remains explicit and recoverable.
- Prove the sidecar can create, validate, load, and Unity-preview a generated
  pack or world through a durable proof bundle.
