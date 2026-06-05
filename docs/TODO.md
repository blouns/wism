# TODO

## Public Readiness

- Audit asset provenance for Unity sprites, tiles, audio, prefabs, scenes, and MOD data.
- Replace any unclear or copied commercial assets with original or clearly licensed assets.
- Add screenshots or a short gameplay GIF to the README after asset provenance is clean.
- Keep the public contributor guide current as build and Unity validation change.

## Build And Packaging

- Document exactly how WismClient DLLs are built and copied into `WismUnity/Assets/Plugins/WismClient/`.
- Decide whether Unity plugin DLLs should remain committed or become generated artifacts in CI/release packaging.
- Add a repeatable Unity batchmode validation command for public contributors.

## Naming And Style Cleanup

- Rename misspelled public code paths carefully, with Unity serialization compatibility in mind.
- Known candidates:
  - `Persistance` -> `Persistence`
  - `ImportCitesFromTilemap` -> `ImportCitiesFromTilemap`
  - `IsInitalized`/similar spellings -> `IsInitialized`
- Avoid renaming serialized fields without migration/backward-compatibility checks.

## World Builder

- Define the world-builder contract in tests.
- Add read-only tools for game state and legal actions.
- Add mutation-capable world repair only after backup, preview, approval, and validation are implemented.
