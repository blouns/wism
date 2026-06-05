# Architecture

WISM separates game rules from presentation so the same strategy engine can be driven by multiple clients.

## Command Flow

```text
Player / AI / UI
       |
       v
WismClient API -> Command stream -> WismClient Core model
       |
       +--> Unity frontend
       +--> ASCII frontend
       +--> Companion or remote-play surfaces
```

All gameplay mutations should enter through command objects. The API records commands in order, processors apply them to the core model, and replay consumers can reapply the same command stream from the same starting state and random seed.

## Project Roles

- `WismClient`: rules, commands, deterministic state, AI, tests, scenario data, and shared contracts.
- `WismUnity`: Unity rendering, input, panels, scene/world authoring, and editor inspection.
- `WismCompanion`: map/state visualization and debugging surfaces.

## Design Constraints

- Keep Unity dependencies out of core rule projects.
- Keep Unity-loaded WismClient binaries compatible with the current Unity profile.
- Prefer additive public contracts and backwards-compatible save/mod data changes.
- Treat Unity scenes, prefabs, and world data as high-value authored assets.
