# TODO

## Public Readiness

- Audit asset provenance for Unity sprites, tiles, audio, prefabs, scenes, and MOD data.
- Replace any unclear or copied commercial assets with original or clearly licensed assets.
- Add screenshots or a short gameplay GIF to the README after asset provenance is clean.
- Keep the public contributor guide current as build and Unity validation change.

## Build And Packaging

- Keep WismClient plugin DLLs committed in `WismUnity/Assets/Plugins/WismClient/`
  as generated shipped artifacts so Unity opens after clone.
- Do not commit incidental DLL churn from local WismClient builds; commit plugin
  DLL updates only when intentionally republishing WismClient changes for Unity.
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

## Manual Parity

- Classic Rules Parity Audit And Completion:
  - Current learnings:
    - WismClient already covers core armies, clans, cities, production, search, artifacts, save/load, basic combat, and command flow.
    - WismUnity and ASCII expose many core commands, but not the full classic menu and report surface.
    - Playground is strong for smoke and eval pressure, but needs targeted parity scenarios rather than only "game runs" signals.
  - Combat parity:
    - Implement and prove DFCM for city defense, tower defense, special terrain, and defending stack bonuses.
    - Fix flying AFCM behavior and add regression tests.
    - Finish army terrain modifiers in combat strength calculation.
    - Reconcile clan terrain modifier data with actual terrain names.
    - Restore and prove neutral-city combat behavior.
  - Movement parity:
    - Add per-army terrain movement costs.
    - Prove stack movement uses the slowest army and highest terrain cost.
    - Prove hero-with-flyer and navy transport edge cases.
    - Add deterministic movement and pathing tests.
  - Economy and production parity:
    - Implement and prove clan-specific starting gold.
    - Correct routed production delivery timing.
    - Enforce and prove routed production receiver limits.
    - Add upkeep pressure plus disband support.
  - Orders, reports, and diplomacy:
    - Add missing commands or explicit deferrals for disband, surrender, reports, hatred/diplomacy, build tower, raze tower, inventory, and find.
    - Keep WismClient authoritative for rules and expose through ASCII and WismUnity after core tests pass.
  - Playground and eval proof:
    - Add manual-parity scenario families for combat modifiers, movement costs, neutral cities, economy/production, and reports/orders.
    - Require parseable artifacts and counters before marking any parity lane complete.
