# Contributing

WISM is under active development. Contributions should keep the core game rules testable outside Unity and preserve the command-driven state model.

## Build And Test

From the repository root:

```powershell
dotnet build WismClient\WismClient.sln --configuration Release
dotnet test WismClient\WismClient.sln --configuration Release
```

Unity work should use Unity `6000.6.0f1` and open `WismUnity/` as the project.

## Project Boundaries

- Put gameplay rules, deterministic state changes, commands, replay, and shared contracts in `WismClient`.
- Put rendering, input, panels, scene authoring, and editor inspection in `WismUnity`.
- Keep Companion and visualization surfaces downstream of WismClient contracts.
- Do not commit private notes, credentials, local paths, copied commercial assets, or unlicensed reference material.

See [Public Boundary](docs/public-boundary.md) and [Asset Provenance](docs/asset-provenance.md).
