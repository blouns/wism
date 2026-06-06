# WISM Modular Packs

WISM modular packs keep the default game profile clear while allowing small,
stackable reskins and launch modes. The first supported profile is
`classic-warlords`; the first supported packs are intentionally modest and do
not add new rules, commands, or executable plugins.

For author-facing templates and the supported validation checklist, see the
[WISM Mod Kit](../mod-kit/README.md).

## Supported Pack Kinds

- `Visual`: presentation catalog data for Unity or companion views.
- `Flavor`: display-name overlays for existing stable ids.
- `Mode`: launch presets that use existing worlds, seeds, players, and
  scenarios.

## Out Of Scope For This Track

- New game rules or command types.
- External code plugins.
- Downloadable mod packages.
- Addressables or AssetBundles.
- Save identity changes.

## Authoring A Simple Pack

Create packs only under the source mod folder:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/<pack-id>/
```

The required manifest is:

```text
WismClient/Wism.Client.Core/mod/FeaturePacks/<pack-id>/pack.json
```

Run validation from `WismClient`:

```powershell
dotnet run --project Wism.ModKit.Cli -- validate profile=classic-warlords packs=<pack-id>
```

Use stable ids from the base mod files. Flavor overlays may change display
names, but must not change `ShortName`, stats, terrain costs, coordinates, or
production values.

Example flavor pack:

```json
{
  "id": "pack-example-flavor",
  "displayName": "Example Flavor",
  "kind": "Flavor",
  "overlay": "overlays/mod-overlay.json"
}
```

Example mode pack:

```json
{
  "id": "pack-example-mode",
  "displayName": "Example Mode",
  "kind": "Mode",
  "launch": {
    "world": "TestWorld",
    "seed": 1990,
    "clans": 2,
    "maxTurns": 8,
    "scenario": "standard"
  }
}
```

## Verification Matrix

```powershell
dotnet test Wism.Client.Test\Wism.Client.Test.csproj --filter ModKitValidatorTests -v:minimal
dotnet test Wism.Client.Test\Wism.Client.Test.csproj --filter ModularProfileCatalogTests -v:minimal
dotnet test Wism.Client.Test\Wism.Client.Test.csproj --filter PlaygroundScenarioRunnerTests -v:minimal
dotnet run --project Wism.Agent.Playground -- world profile=classic-warlords --quiet
dotnet run --project Wism.Agent.Playground -- world profile=classic-warlords packs=pack-dusklands-visual --quiet
dotnet run --project Wism.Agent.Playground -- world profile=classic-warlords packs=pack-illurian-legends-flavor --quiet
dotnet run --project Wism.Agent.Playground -- campaign profile=classic-warlords packs=pack-quick-clash-mode --quiet
dotnet run --project Wism.Agent.Playground -- campaign profile=classic-warlords packs=pack-dusklands-visual,pack-illurian-legends-flavor,pack-quick-clash-mode --quiet
```

Unity smoke can record the same selection:

```powershell
Unity.exe -batchmode -quit -projectPath WismUnity -executeMethod WismUnity.Playground.UnityPlaygroundCli.Run -- profile=classic-warlords packs=pack-dusklands-visual,pack-illurian-legends-flavor,pack-quick-clash-mode
```
