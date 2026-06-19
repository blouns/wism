# WismUnity Runtime Performance Audit

Date: 2026-06-19
Scope: `WismUnity/Assets/Scripts/UnityGame/`

## Inventory

- Lookup calls: 110 `GameObject`/scene/object lookup call sites in UnityGame C# scripts.
- Tick methods: 18 `Update`, `FixedUpdate`, or `LateUpdate` methods in UnityGame scripts.
- Broad scene scans:
  - `UnityUtilities.GameObjectHardFind` walks all scene objects.
  - `ArmyManager.RefreshTowerOwnershipVisuals` calls `FindObjectsOfType<TowerOwnershipVisual>(true)`.
  - `ArmyPicker.Initialize` calls `GameObject.FindObjectsOfType<Button>()`.
- Runtime map scans:
  - `ArmyManager.DrawArmyGameObjects` scans every tile to decide top-army visuals.
  - `ItemManager.GetItemsOnTiles` scans every tile to find ground artifacts.
  - `CityManager.DrawCities` redraws owned city tiles on every physics tick.

## Startup And Editor Costs

- Factory, editor, picker, and setup code use broad lookup calls while creating or configuring scenes.
- EditorBridge and Playground code allocate LINQ projections and arrays for diagnostics. These are acceptable for editor tooling and proof capture.
- `UnityUtilities.GameObjectHardFind` remains useful for startup wiring but should not be used in per-frame paths.

## Runtime Per-Frame Costs

- `CityManager.FixedUpdate` called `GameObject.FindGameObjectWithTag("WorldTilemap")` through `DrawCities()` every physics tick before repainting city tiles.
- `ArmyManager.FixedUpdate` scans players, armies, the whole map, and all tower ownership visuals.
- `ItemManager.FixedUpdate` scans the whole map and allocates a list of current tile artifacts before cleanup.
- UI `LateUpdate` and `Update` handlers mostly drive selection, camera, notification, and cursor state.

## Ranked Candidates

1. Cache `CityManager` world tilemap lookup.
   - Value: low to medium, removes a per-physics scene lookup.
   - Risk: low, the same cached field was already used by city tile writes.
   - Status: implemented in this tranche.
2. Cache or event-refresh `TowerOwnershipVisual` references in `ArmyManager`.
   - Value: medium, removes a broad scene scan from the army drawing loop.
   - Risk: medium, tower visuals may be recreated when worlds or scenes reload.
3. Replace `ArmyManager` full-map top-army scan with a stack-position index.
   - Value: high on large maps.
   - Risk: medium to high, must stay correct for visiting armies, selection, battles, and movement.
4. Add an artifact index for `ItemManager`.
   - Value: medium on item-heavy maps.
   - Risk: medium, inventory transfers and location rewards must update the index.

## Tranche Cleanup

`CityManager.DrawCities()` now resolves `WorldTilemap` only when the cached field is missing. `Reset()` uses the same resolver, so reload paths can still recover the reference after manager reset without paying the lookup every physics tick.
