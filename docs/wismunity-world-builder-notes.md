# WISMUnity World Builder Notes

WISMUnity currently uses Unity in two ways:

1. As the playable frontend.
2. As a world-builder surface for map, city, and location authoring.

That hybrid is intentional for now, but it is also the highest-risk part of the Unity project because scene state can be hard to reconstruct by hand.

## Current Contract

Important scene objects:

- `UnityManager`
- `WorldTilemap`
- `Cities`
- `Locations`

Important data sources:

- Unity scene tilemaps
- child objects under `Cities`
- child objects under `Locations`
- MOD JSON under `Assets/Mod/Worlds`
- WismClient MOD JSON copied under `Assets/Plugins/WismClient/Mods/Worlds`

## Known Risks

- Some authoring controls exist in editor inspectors and can create or reset scene objects.
- Runtime world creation reads Unity scene state and MOD JSON together.
- City and location short names must line up with MOD JSON.
- Scene changes may look small in the editor but be expensive to reconstruct.

## Safety Rules

1. Prefer read-only validation before changing scenes.
2. Back up scenes, prefabs, and MOD JSON before mutation-capable tooling.
3. Do not call import/export/reset helpers from automation unless the operation has a preview and explicit approval.
4. Do not save scenes automatically from MCP tools unless the tool is clearly mutation-capable and approved.
5. Validate scene/MOD drift after any world-builder change.

## Tooling Direction

World-builder automation should start with read-only inspection and validation. Mutation-capable tooling should come later, after backup, preview, explicit approval, and validation are available.
