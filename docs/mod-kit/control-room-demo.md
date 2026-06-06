# Mod Kit Control Room Demo

The Unity Mod Kit Control Room is a read-only editor surface for checking what
WismUnity will load for a selected profile, feature-pack set, world, and mod
root.

Open it from:

```text
WISM / Mod Kit / Control Room
```

## Demo Flow

1. Open `WismUnity` in Unity.
2. Select `WISM / Mod Kit / Control Room`.
3. Use:
   - `Profile`: `classic-warlords`
   - `Packs`: `pack-dusklands-visual,pack-illurian-legends-flavor,pack-quick-clash-mode`
   - `World Override`: `TestWorld`
   - `Mod Root`: `Assets\Plugins\WismClient\Mods`
4. Click `Refresh Status`.
5. Click `Write Status Manifest`.
6. Click `Copy Smoke Command` if you want the matching Unity Playground
   batchmode smoke command.

The Control Room does not save scenes, import maps, export MOD files, or mutate
scene objects. It calls the same selection inspection helper used by the Unity
Playground and MCP-compatible status report.

## Proof Matrix

The Stage 4 demo matrix should cover:

| Case | Profile | Packs | Unity status | Unity runtime smoke |
| --- | --- | --- | --- | --- |
| Default | default behavior | none | required | required |
| Zero pack | `classic-warlords` | none | required | required |
| Flavor | `classic-warlords` | `pack-illurian-legends-flavor` | required | required |
| All proof packs | `classic-warlords` | `pack-dusklands-visual,pack-illurian-legends-flavor,pack-quick-clash-mode` | required | required |

Generated proof artifacts are written under:

```text
artifacts/mod-kit/stage4-night/
artifacts/mod-kit/stage4-night-proof/
```

These artifacts are local proof output and are not required source files.

## Acceptance

The demo is Green only when:

- Focused Mod Kit tests pass.
- CLI validation passes.
- AgentPlayground loads the selected profile and packs.
- Unity status manifests report `Passed`.
- Unity runtime manifests report `Passed`.
- Unity console errors are zero.
- Dirty scene count is zero.
