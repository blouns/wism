# WismUnity Production Pipeline

WismUnity uses Unity CLI and `com.unity.pipeline` as its editor, build, and test automation surface. The deprecated in-editor MCP server from `com.unity.ai.assistant` is not a project dependency.

## Operating Model

Use direct structured commands whenever the caller can execute a shell:

- `unity doctor --ci` for machine, license, editor, and network preflight.
- `unity pipeline list` for running editor, Pipeline version, reachability, and Safe Mode state.
- `unity status` and `unity command` for a warm connected editor.
- `unity run --command <name>` for a one-shot headless project command.
- `unity test` for EditMode and PlayMode verdicts.
- `unity build` for guarded builds and provenance manifests.

The Unity CLI `unity mcp` adapter is compatibility-only for clients that cannot execute shell commands. WismUnity does not require MCP for normal automation.

## WISM Commands

Project-owned commands live in `Assets/Scripts/UnityGame/EditorBridge/WismUnityPipelineCommands.cs`. They are read-only and registered through `Unity.Pipeline.Commands.CliCommandAttribute`:

- `wism_project_status`
- `wism_package_status`
- `wism_scene_summary`
- `wism_console_summary`
- `wism_game_view_metadata`
- `wism_world_builder_summary`
- `wism_validate_world_contract`
- `wism_world_builder_risk_report`
- `wism_world_state`
- `wism_legal_actions`
- `wism_evaluate_board`
- `wism_ai_turn_preview`
- `wism_modkit_status`

Discover the current command surface from a connected editor with `unity list`. Invoke a warm command with `unity command <name>`, or a one-shot command with:

```powershell
unity run .\WismUnity --command wism_project_status --format ndjson --non-interactive
```

## Validation Contract

1. Run `unity doctor --ci --format json --non-interactive`.
2. Confirm `com.unity.pipeline` is installed and `com.unity.ai.assistant` is absent.
3. Use an isolated worktree for headless import and tests when another editor may be open.
4. Treat `unity test` exit `0` as pass, exit `8` as a completed failing verdict, and other nonzero exits as invalid or infrastructure failure.
5. Preserve NUnit/JUnit XML, Editor log, and the isolated-run manifest as proof.
6. Run focused tests before broad PlayMode or full-project validation.

The isolated runner at `scripts/run-unity-validation-isolated.ps1` uses this contract and never treats launcher text as stronger evidence than the final reports.

## Mutation Policy

Keep project commands read-only by default. A future command that changes scenes, prefabs, tilemaps, world data, or MOD JSON must provide preview, backup, explicit apply intent, and post-change validation. Runtime game commands must route through WismClient rules rather than directly mutating Unity objects.

## Maintenance

- Refresh Unity CLI and its Codex skill together.
- Review Pipeline package release notes before version changes.
- Re-run command discovery after package or custom command changes.
- Keep external Unity MCP projects as research references, not runtime dependencies.
