# WismUnity Playground

`WismUnity.Playground` is a non-destructive Unity-side smoke harness for WISM scenarios. It is the Unity adapter for the same scenario/eval direction as the core agent playground.

## Smoke Command

Run with the Unity editor closed:

```powershell
& '<Unity.exe>' `
  -batchmode -quit `
  -projectPath '<path-to-WismUnity>' `
  -executeMethod WismUnity.Playground.UnityPlaygroundCli.Run `
  -logFile '<path-to-WismUnity>\Logs\unity-playground-smoke.log' `
  -- world=TestWorld runId=smoke-001 out=artifacts\unity-playground\smoke
```

The runner writes `manifest.json` under the requested artifact root and restores the original editor scene setup without saving.
Add `advanceBootstrap=true` only when you intentionally want the runner to tick the runtime bootstrap path in editor/batchmode.

Use `scenario=viewport-input-proof` with `screenWidth=<w>` and
`screenHeight=<h>` to capture beta-readiness evidence for a specific supported
desktop/windowed target. The manifest records the supported viewport list, known
fixed-size assumptions, MinimapPanel geometry, screen-to-map conversion, minimap
coordinate projection, and an optional screenshot when `screenshot=true`.
Add `requireActualResolution=true` when the proof must fail unless either the
runtime `Screen.width`/`Screen.height` or the selected Editor GameView fixed
resolution matches the requested target. In non-batch Editor runs, the manifest
records both values because edit-mode `Screen` dimensions can differ from the
selected GameView size.

Add `profile=<id>` and `packs=a,b` to exercise the Mod Kit selection path. When
these arguments are present, the runner resolves the profile from
`Assets\Plugins\WismClient\Mods`, applies the selected packs to `ModFactory`,
sets `GameManager.ModPath` and `GameManager.WorldName`, and records the
selection report in the manifest. When no profile, pack, or `modRoot` argument
is supplied, the runner preserves the default `Assets\Mod` behavior.

Use `command=modkit-status` to generate only the read-only Mod Kit status
manifest without loading the smoke scene or applying the selection.

## Safety Contract

- Do not mutate scenes, prefabs, MOD JSON, save slots, or plugin DLLs.
- Write artifacts outside `Assets`.
- Load the target scene additively.
- Initialize `UnityManager` with deterministic non-interactive settings.
- Do not advance runtime ticks by default.
- Fail the run if newly loaded scenes become dirty.
- Use one Unity process per isolated project checkout for parallel runs.
