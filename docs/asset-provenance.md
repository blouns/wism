# Asset Provenance

Asset provenance is a lightweight inventory that records where visible assets and redistributed reference materials came from, whether WISM has the right to ship them, and what cleanup action is needed.

This matters because public repositories should make asset ownership boring and obvious.

## Status Key

- `Owned`: created for WISM or by the project owner.
- `Licensed`: third-party asset with a redistributable license.
- `Package`: installed package asset governed by its package license.
- `Unknown`: source is not yet documented.
- `Do not ship`: remove from public distribution until rights are clear.

## Inventory

| Area | Path | Status | Notes | Action |
| --- | --- | --- | --- | --- |
| WISM notes | `wism_manual.md` | Owned | WISM-specific notes/manual content. | Keep. |
| Unity scenes | `WismUnity/Assets/Scenes/` | Unknown | Hand-authored Unity scene/world state. | Audit visible art, sprites, tile assets, and embedded references. |
| Unity prefabs | `WismUnity/Assets/Prefab/` | Unknown | Hand-authored prefab state. | Audit visible assets and dependencies. |
| Unity MOD data | `WismUnity/Assets/Mod/` | Owned/Unknown | JSON world data used by WISMUnity. Names and maps should be reviewed for originality. | Keep, audit names/maps. |
| WismClient MOD data | `WismUnity/Assets/Plugins/WismClient/Mods/` | Owned/Unknown | Copied game data consumed by Unity. | Keep, audit names/maps. |
| Newtonsoft package | `WismUnity/Assets/Packages/Newtonsoft.Json.13.0.3/` | Package | Package includes its own license file. | Keep if still required; prefer Unity package reference where possible. |
| NuGet docs | `WismUnity/Assets/NuGet/README.pdf` | Package | NuGetForUnity documentation/package artifact. | Keep if required by package; otherwise consider package cleanup later. |
| Classic-game manual PDF | root PDF formerly committed | Do not ship | Commercial game manual/reference material should not be redistributed in this public repo. | Removed; do not restore to the public repo. |

## Rules

1. Do not add copied commercial game manuals, maps, sprites, sounds, or UI art.
2. New third-party assets must include license/source notes in this inventory.
3. If source is unclear, mark it `Unknown` and do not claim it is original.
4. If an asset is needed only for private research, keep it out of the public repo.
5. Prefer original WISM assets or clearly licensed placeholders.
