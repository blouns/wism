# Customer Mod Build Guide

This guide walks through building a small WISM data-only mod pack. It is meant
for a mod author who wants to make a real change, validate it, try it locally,
and collect proof that it works.

V1 Mod Kit packs are intentionally conservative. They can change presentation,
flavor text, and launch presets. They cannot add executable code or rewrite game
rules.

## Supported Authoring Location

Create Mod Kit packs in the WismClient source mod root:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/<pack-id>/
```

This is the supported V1 workflow. It keeps the pack next to the base WISM data
that validation needs, and it matches the default `Wism.ModKit.Cli` behavior.

Use this location when you are building a pack that should become part of the
project, an example pack, or a reviewed mod contribution.

## What You Can Build Today

Start with one of these pack types:

- `Flavor`: rename existing clans, armies, or artifacts for display.
- `Visual`: provide presentation catalog data for clients that use it.
- `Mode`: define a launch preset using an existing world, seed, clan count,
  turn bound, and scenario label.

Good first mod:

```text
pack-my-first-flavor
```

It renames a few existing display names without changing stable ids, stats,
maps, commands, or save contracts.

## Before You Start

Recommended tools:

- .NET SDK that can build `WismClient`.
- Unity only if you want Unity preview/proof.
- A text editor.

Start from the `WismClient` folder when running .NET commands:

```powershell
cd WismClient
```

## How The CLI Finds Your Pack

`Wism.ModKit.Cli` is a tool project in the `WismClient` solution. When you run
it from inside the repo, it finds the repository root and uses this mod root by
default:

```text
WismClient/Wism.Client.Core/mod/
```

So this command:

```powershell
dotnet run --project Wism.ModKit.Cli -- validate profile=classic-warlords packs=pack-my-first-flavor
```

looks for:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/pack-my-first-flavor/pack.json
```

You do not need to pass a path for the normal V1 workflow.

## Step 1: Pick A Pack Id

Use a lowercase folder-friendly id:

```text
pack-my-first-flavor
```

Keep the folder name and `pack.json` `id` exactly the same.

## Step 2: Create The Pack Folder

Create:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/pack-my-first-flavor/
```

For a flavor pack, also create:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/pack-my-first-flavor/overlays/
```

## Step 3: Add `pack.json`

Create:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/pack-my-first-flavor/pack.json
```

Use this starter content:

```json
{
  "id": "pack-my-first-flavor",
  "displayName": "My First Flavor Pack",
  "kind": "Flavor",
  "description": "Display-name changes for existing WISM stable ids.",
  "dependencies": [],
  "conflicts": [],
  "overlay": "overlays/mod-overlay.json"
}
```

## Step 4: Add The Flavor Overlay

Create:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/pack-my-first-flavor/overlays/mod-overlay.json
```

Use existing stable ids in `shortName`, and only change `displayName`:

```json
{
  "clans": [
    {
      "shortName": "Sirians",
      "displayName": "Sirians of the Bright Coast"
    },
    {
      "shortName": "LordBane",
      "displayName": "The Black Citadel"
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

Stable ids are case-sensitive. If validation says an id is missing, check the
base data:

```text
WismClient/Wism.Client.Core/mod/Clan.json
WismClient/Wism.Client.Core/mod/Army.json
WismClient/Wism.Client.Core/mod/Artifact.json
```

## Step 5: Validate The Pack

From `WismClient`:

```powershell
dotnet run --project Wism.ModKit.Cli -- validate profile=classic-warlords packs=pack-my-first-flavor
```

For structured output:

```powershell
dotnet run --project Wism.ModKit.Cli -- validate profile=classic-warlords packs=pack-my-first-flavor --json
```

Expected result:

```text
Mod Kit validation: Passed
```

If validation fails, fix the reported path and issue before moving on.

## Step 6: Run The Focused Tests

After adding or changing a pack, run the Mod Kit tests:

```powershell
dotnet test Wism.Client.Test\Wism.Client.Test.csproj --filter ModKitValidatorTests -v:minimal
dotnet test Wism.Client.Test\Wism.Client.Test.csproj --filter ModularProfileCatalogTests -v:minimal
```

These tests catch common pack shape and catalog resolution mistakes.

## Step 7: Try It In AgentPlayground

Run a lightweight game-load smoke:

```powershell
dotnet run --project Wism.Agent.Playground -- world profile=classic-warlords packs=pack-my-first-flavor --quiet
```

This proves the pack can be selected by a WismClient-based runtime without
opening Unity.

## Step 8: Preview In Unity

Unity preview is optional for quick editing, but required for a strong release
or phase proof.

Open WismUnity, then open:

```text
WISM / Mod Kit / Control Room
```

Use:

```text
Profile: classic-warlords
Packs: pack-my-first-flavor
World Override: TestWorld
Mod Root: Assets\Plugins\WismClient\Mods
```

Click:

1. `Refresh Status`
2. `Write Status Manifest`

The Control Room is read-only. It does not save scenes, import maps, export MOD
files, or mutate scene objects.

## Step 9: Copy To Unity Plugin Mods When Needed

Unity reads copied WismClient mod data from:

```text
WismUnity/Assets/Plugins/WismClient/Mods/
```

If your new pack exists only under `WismClient/Wism.Client.Core/mod`, Unity will
not see it until the Unity plugin mod data is updated. Use the existing project
build or publish flow for that copy. Do not hand-edit generated plugin DLLs.

After copying mod data for Unity, rerun the Unity Control Room status check.

## Step 10: Create A Proof Bundle

Once you have Unity status and runtime manifests, create a proof bundle:

```powershell
dotnet run --project Wism.ModKit.Cli -- proof profile=classic-warlords packs=pack-my-first-flavor unityStatusManifest=<path-to-status-manifest.json> unityManifest=<path-to-runtime-manifest.json>
```

The proof should be `Green` only when:

- CLI validation passes.
- AgentPlayground passes.
- Unity status proof passes.
- Unity runtime proof passes.
- Unity reports zero console errors.
- Unity reports zero dirty scenes.

## Building Other Pack Types

### Visual Pack

Create a `pack.json` with:

```json
{
  "id": "pack-my-visual-style",
  "displayName": "My Visual Style",
  "kind": "Visual",
  "description": "Presentation-only catalog data.",
  "dependencies": [],
  "conflicts": [],
  "presentationCatalog": "presentation-catalog.json"
}
```

Then add `presentation-catalog.json`. Start from:

```text
docs/mod-kit/templates/feature-pack/presentation-catalog.json
```

### Mode Pack

Create a `pack.json` with:

```json
{
  "id": "pack-my-quick-mode",
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

The launch world must exist and include its required world files.

## Safety Rules

- Do not change stable ids such as `ShortName`.
- Do not include executable code in a V1 data pack.
- Do not copy unclear third-party assets into a public example.
- Do not edit generated Unity plugin DLLs.
- Do not save or mutate Unity scenes as part of a data-pack edit.
- Keep pack ids lowercase, stable, and folder-friendly.

## Troubleshooting

### `profile-pack-missing`

The selected pack id does not exist under `FeaturePacks`, or the folder name and
`pack.json` id do not match.

### `selection-invalid`

The selected profile or pack set cannot be resolved. Check spelling and whether
dependencies or conflicts are involved.

### Flavor overlay id not found

The overlay references a `shortName` that does not exist in the base data. Check
`Clan.json`, `Army.json`, or `Artifact.json`.

### Unity does not show the pack

Unity may be looking at copied plugin mod data. Update
`WismUnity/Assets/Plugins/WismClient/Mods/`, then rerun the Control Room status
check.

### Proof is Yellow

Yellow usually means Unity proof is missing, incomplete, or not passed. A CLI
validation pass alone is useful, but it is not a full release proof.

## Done Checklist

- `pack.json` id matches the folder name.
- JSON files parse.
- CLI validation passes.
- Focused Mod Kit tests pass.
- AgentPlayground smoke passes.
- Unity Control Room status passes when Unity proof is needed.
- Proof bundle is Green for release or phase claims.
