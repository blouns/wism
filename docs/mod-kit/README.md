# WISM Mod Kit

The WISM Mod Kit is the supported authoring path for small, data-only WISM
customizations. V1 supports profile and feature-pack manifests that can be
loaded by WismClient, AgentPlayground, and WismUnity without changing rules,
commands, save identity, or executable code.

For the current roadmap and Unity sidecar direction, see the
[Mod Kit Journey](journey.md).

For the long-term target, see the
[Full Moddable WISM Roadmap](full-moddable-wism-roadmap.md).

For the phase proof standard and concrete backlog, see the
[Mod Kit E2E Proof Plan](e2e-proof-plan.md) and
[Mod Kit Work Items](work-items.md).

For the Unity editor demo surface, see the
[Mod Kit Control Room Demo](control-room-demo.md).

For a hands-on author walkthrough, see the
[Customer Mod Build Guide](customer-mod-build-guide.md).

For the full sidecar map and world-builder direction, see the
[Sidecar World Builder Plan](sidecar-world-builder-plan.md).

## What You Can Build

- Visual packs: presentation catalog data for Unity, Companion, or other views.
- Flavor packs: display-name overlays for existing stable ids.
- Mode packs: launch presets that select existing worlds, seed, clan count,
  turn bounds, and scenario labels.

## What Is Not Supported Yet

- New rule systems, command types, or battle logic.
- External code plugins.
- AssetBundles, Addressables, or downloadable packages.
- Changing stable ids such as `ShortName`.
- Mutating world coordinates or production values through flavor overlays.

## Authoring Layout

The source mod folder is the supported authoring location:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/<pack-id>/
```

The required manifest is:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/<pack-id>/pack.json
```

Useful templates live under `docs/mod-kit/templates/feature-pack/`.

Run authoring commands from `WismClient`. The Mod Kit CLI project lives at:

```text
WismClient/Wism.ModKit.Cli/
```

For the normal workflow, no extra path arguments are needed. The CLI finds
`WismClient/Wism.Client.Core/mod/` by default.

## Stable Id Rule

Packs may display existing data differently, but they must keep stable ids
intact. For example, a flavor pack can rename the `Sirians` clan on screen, but
it cannot rename the stable `ShortName` from `Sirians` to another value.

Stable ids are currently read from:

- `WismClient/Wism.Client.Core/mod/Clan.json`
- `WismClient/Wism.Client.Core/mod/Army.json`
- `WismClient/Wism.Client.Core/mod/Artifact.json`

## Local Validation

Run the Mod Kit CLI for author-facing validation:

```powershell
dotnet run --project Wism.ModKit.Cli -- validate profile=classic-warlords packs=<pack-id>
dotnet run --project Wism.ModKit.Cli -- validate profile=classic-warlords packs=<pack-id> --json
```

Run the focused validator tests after adding or changing a pack:

```powershell
dotnet test Wism.Client.Test\Wism.Client.Test.csproj --filter ModKitValidatorTests -v:minimal
dotnet test Wism.Client.Test\Wism.Client.Test.csproj --filter ModularProfileCatalogTests -v:minimal
```

Run an AgentPlayground smoke for the selected profile and pack set:

```powershell
dotnet run --project Wism.Agent.Playground -- world profile=classic-warlords packs=<pack-id> --quiet
```

Unity validation is optional for quick local iteration on pure data-only pack
edits, but a phase or release claim is not complete without the Unity proof
required by the [Mod Kit E2E Proof Plan](e2e-proof-plan.md).

Unity reads copied plugin mod data from
`WismUnity/Assets/Plugins/WismClient/Mods/`. Keep
`WismClient/Wism.Client.Core/mod/` as the source of truth and update the Unity
plugin copy through the normal build/publish flow.

Create a proof bundle after CLI, AgentPlayground, and Unity evidence exist:

```powershell
dotnet run --project Wism.ModKit.Cli -- proof profile=classic-warlords packs=<pack-id> unityStatusManifest=<path-to-unity-status-manifest.json> unityManifest=<path-to-unity-runtime-manifest.json>
```

## Release Checklist

- `pack.json` id matches the feature-pack folder name.
- `displayName` and `kind` are present.
- Flavor overlay short names resolve to existing stable ids.
- Visual presentation catalog exists and parses as JSON.
- Mode launch world exists and has `City.json` and `Location.json`.
- Dependencies exist locally and conflicts are intentional.
- No executable code, private notes, local paths, or third-party asset copies
  are included in the pack.
