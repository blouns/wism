# Customer Mod Build Guide

This guide shows the supported way to create a small WISM data-only feature
pack. Keep it simple: create the pack in the WISM source mod folder, validate it
with the Mod Kit CLI, then try it in AgentPlayground or Unity when needed.

V1 packs can change presentation, flavor text, and launch presets. They cannot
add executable code, change rules, or rename stable ids.

## One Supported Location

Create feature packs here:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/<pack-id>/
```

This is the normal authoring location. Packs in this folder are visible to the
default Mod Kit CLI, tests, and WismClient-based tooling.

Use a branch if you are experimenting.

## CLI Location

The CLI project is in the WismClient solution:

```text
WismClient/Wism.ModKit.Cli/
```

Run Mod Kit commands from `WismClient`:

```powershell
cd WismClient
```

## Create A Flavor Pack

This example creates `pack-my-first-flavor`.

Create these folders:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/pack-my-first-flavor/
WismClient/Wism.Client.Core/mod/FeaturePacks/pack-my-first-flavor/overlays/
```

Create:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/pack-my-first-flavor/pack.json
```

```json
{
  "schemaVersion": 1,
  "id": "pack-my-first-flavor",
  "version": "1.0.0",
  "minWismVersion": "0.1.0",
  "displayName": "My First Flavor Pack",
  "kind": "Flavor",
  "description": "Display-name changes for existing WISM stable ids.",
  "dependencies": [],
  "conflicts": [],
  "overlay": "overlays/mod-overlay.json"
}
```

Create:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/pack-my-first-flavor/overlays/mod-overlay.json
```

```json
{
  "clans": [
    {
      "shortName": "Sirians",
      "displayName": "Sirians of the Bright Coast"
    }
  ],
  "armies": [
    {
      "shortName": "LightInfantry",
      "displayName": "Border Spears"
    }
  ],
  "artifacts": [
    {
      "shortName": "Firesword",
      "displayName": "Emberblade"
    }
  ]
}
```

Stable ids are case-sensitive. Use existing `shortName` values from:

```text
WismClient/Wism.Client.Core/mod/Clan.json
WismClient/Wism.Client.Core/mod/Army.json
WismClient/Wism.Client.Core/mod/Artifact.json
```

## Validate

From `WismClient`:

```powershell
dotnet run --project Wism.ModKit.Cli -- validate profile=classic-warlords packs=pack-my-first-flavor
```

Expected result:

```text
Mod Kit validation: Passed
```

For machine-readable output:

```powershell
dotnet run --project Wism.ModKit.Cli -- validate profile=classic-warlords packs=pack-my-first-flavor --json
```

## Try It In AgentPlayground

From `WismClient`:

```powershell
dotnet run --project Wism.Agent.Playground -- world profile=classic-warlords packs=pack-my-first-flavor --quiet
```

This confirms a WismClient runtime can load the selected profile and pack.

## Run Focused Tests

From `WismClient`:

```powershell
dotnet test Wism.Client.Test\Wism.Client.Test.csproj --filter "ModKitValidatorTests|ModularProfileCatalogTests" -v:minimal
```

These tests catch invalid manifests, broken overlay references, and catalog
loading mistakes.

## Unity Preview And Play

Unity uses copied plugin mod data from:

```text
WismUnity/Assets/Plugins/WismClient/Mods/
```

The source of truth is still:

```text
WismClient/Wism.Client.Core/mod/
```

After the Unity plugin mod data has been updated, open WismUnity and use:

```text
WISM / Mod Kit / Control Room
```

Set:

```text
Profile: classic-warlords
Packs: pack-my-first-flavor
Mod Root: Assets\Plugins\WismClient\Mods
```

Then click `Refresh Status`. The Control Room is read-only; it should not save
scenes or mutate Unity objects.

To play with the pack in Unity, open WismUnity and press Play. The splash screen
loads `ModSettings` before `GameSetup`.

In `ModSettings`:

```text
Profile: classic-warlords
World: TestWorld
Selected packs: pack-my-first-flavor
```

Click `Refresh` if you have copied new plugin mod data while Unity is open.
`Continue` is enabled only when the profile, pack stack, world data, Unity
scene, version fields, dependencies, conflicts, and compatibility fingerprint
are Green.

After `Continue`, the normal `GameSetup` scene starts a new game using that
locked stack. Saves record the exact profile, pack ids, pack versions, world,
and content fingerprint. Loading a save uses the saved stack, not the current
Mod Settings selection. If required pack data is missing or the fingerprint no
longer matches, loading is blocked with a clear error. Legacy saves with no mod
metadata still load with the default no-pack behavior and a warning.

## Other Pack Kinds

### Visual

Use `kind: "Visual"` and point to a presentation catalog:

```json
{
  "schemaVersion": 1,
  "id": "pack-my-visual-style",
  "version": "1.0.0",
  "minWismVersion": "0.1.0",
  "displayName": "My Visual Style",
  "kind": "Visual",
  "description": "Presentation-only catalog data.",
  "dependencies": [],
  "conflicts": [],
  "presentationCatalog": "presentation-catalog.json"
}
```

Start from:

```text
docs/mod-kit/templates/feature-pack/presentation-catalog.json
```

### Mode

Use `kind: "Mode"` and provide a launch preset:

```json
{
  "schemaVersion": 1,
  "id": "pack-my-quick-mode",
  "version": "1.0.0",
  "minWismVersion": "0.1.0",
  "displayName": "My Quick Mode",
  "kind": "Mode",
  "description": "Launch preset using an existing world.",
  "dependencies": [],
  "conflicts": [],
  "launch": {
    "world": "TestWorld",
    "seed": 1990,
    "clans": 2,
    "maxTurns": 8,
    "scenario": "standard"
  }
}
```

## Rules Of Thumb

- Keep the folder name and `pack.json` `id` identical.
- Use lowercase, folder-friendly pack ids.
- Include `schemaVersion`, `version`, and `minWismVersion` so the pack can be
  Green verified rather than merely legacy-loadable.
- Keep stable ids such as `ShortName` unchanged.
- Do not add executable code to a V1 pack.
- Do not hand-edit generated Unity plugin DLLs.
- Keep the pack under `WismClient/Wism.Client.Core/mod/FeaturePacks`.

## Troubleshooting

`profile-pack-missing`: the selected pack id does not exist under
`FeaturePacks`, or the folder name and `pack.json` id do not match.

`selection-invalid`: the selected profile or pack set cannot be resolved. Check
spelling, dependencies, and conflicts.

Flavor overlay id not found: the overlay references a `shortName` that does not
exist in the base data.

Unity does not show the pack: Unity is reading copied plugin mod data. Update
`WismUnity/Assets/Plugins/WismClient/Mods/`, then refresh the Control Room.

Unity `Continue` stays disabled: check the status panel in `ModSettings`.
Common causes are missing version metadata, a missing scene for the selected
world, an unsupported WISM version range, unresolved dependencies, or conflicts.

## Done Checklist

- Pack lives under `WismClient/Wism.Client.Core/mod/FeaturePacks/<pack-id>/`.
- `pack.json` id matches the folder name.
- JSON files parse.
- CLI validation passes.
- Focused Mod Kit tests pass.
- AgentPlayground smoke passes when runtime proof is needed.
- Unity Control Room status passes when Unity proof is needed.
- Unity `ModSettings` shows Green and `Continue` starts `GameSetup`.
