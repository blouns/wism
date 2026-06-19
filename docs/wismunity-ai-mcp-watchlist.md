# WISMUnity AI/MCP Watchlist

This note captures current learning from Unity AI Assistant, Unity custom MCP tools, and adjacent open-source Unity MCP projects.

## Recommendation

Use Unity's official AI Assistant MCP support as the WISMUnity foundation. Keep WISM-specific tools inside this repo as narrow, explicit, project-owned editor tools. Study external Unity MCP projects for patterns, but do not add a second MCP bridge dependency unless Unity's first-party path blocks us.

The current WISMUnity bridge follows this direction with read-only game, Mod Kit, and world-builder inspection tools.

## To Watch

### Unity AI Assistant and Custom MCP Tools

Links:
- https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.10/manual/index.html
- https://discussions.unity.com/t/tutorial-how-to-create-custom-mcp-tools-for-unity/1717182

Use as the first-party editor integration path. Unity supports registering custom MCP tools alongside built-in tools through `Unity.AI.MCP.Editor.ToolRegistry`.

Watch for:
- API stability across Unity 6000.x releases.
- Custom tool registration conventions.
- Tool permissions and enabled-by-default behavior.
- Batchmode and external-client invocation patterns.
- Domain reload behavior.

### CoplayDev/unity-mcp

Link:
- https://github.com/CoplayDev/unity-mcp

Use as a reference architecture, not a WISMUnity dependency for now. It has useful ideas around editor automation, tool groups, multi-instance routing, script validation, remote/auth modes, and test workflows.

Watch for:
- Tool-group design.
- Multi-Unity-instance routing.
- Test and validation integration.
- Security posture for write-capable tools.
- Patterns worth contributing upstream if they are generic.

### IvanMurzak/Unity-MCP

Link:
- https://github.com/IvanMurzak/Unity-MCP

Use for private comparison experiments, not as a public WISMUnity dependency yet. It is powerful and flexible, including CLI setup and broad method/tool exposure, which is useful for research but too permissive for WISMUnity's public safety posture without additional gates.

Watch for:
- CLI ergonomics.
- Any-C#-method-to-tool patterns.
- Fast editor command loops.
- Token-efficient tool schemas.
- Risks from broad reflection or dynamic code execution.

### Unity Sentis Samples

Link:
- https://github.com/Unity-Technologies/sentis-samples

Use later for local inference experiments, not current MCP bridge work.

Watch for:
- Board-game AI sample patterns.
- Local inference performance.
- Model packaging and runtime constraints.
- Whether Sentis helps WISM evaluation without overcomplicating the frontend.

### Needle Mirrors

Links:
- https://github.com/needle-mirror/com.unity.ai.assistant
- https://github.com/needle-mirror/com.unity.ai.generators

Use only as source-reading mirrors when Unity docs lag. Install official Unity packages through Package Manager, not these mirrors.

Watch for:
- Package source details not obvious in public docs.
- Changelog and API surface changes.
- Generator workflows that might help asset prototyping without becoming required infrastructure.

## WISM Tool Ladder

Start with read-only tools:
- `GetWorldState`
- `EvaluateBoard`
- `GetLegalActions`
- `ValidateScenario`
- `GetHumanPlayerStatus`

Then add runtime command tools routed through WismClient rules:
- `MoveArmy`
- `EndTurn`
- `RunAITurn`
- `CaptureCity`

Only later add editor/world-builder mutation tools:
- `GenerateScenario`
- `PreviewWorldRepair`
- `ApplyWorldRepair`

Mutation tools must require backup, dry-run/preview, explicit approval, and validation. Scenes, prefabs, tilemaps, city objects, location objects, and MOD JSON are high-value state.

## Immediate Actions

1. Keep the current first-party Unity MCP bridge and read-only inspection tools.
2. Keep the WISMUnity MCP tool doctrine split between read-only, runtime-command, and mutation-capable tools.
3. Add read-only game-contract tools before command or mutation tools.
4. Keep broad reflection/dynamic-code MCP experiments outside this public repo or in isolated worktrees.
5. Consider upstream contributions only for generic Unity MCP safety/testing patterns, not WISM-specific game logic.
