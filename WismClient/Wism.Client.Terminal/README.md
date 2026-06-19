# WISM Terminal Client

`Wism.Client.Terminal` is a keyboard-first terminal surface for WISM. It loads
the same WismClient rules and mod data as the other clients, renders a viewport,
and can record/replay JSONL command sessions for deterministic inspection.

## Run

```powershell
dotnet run --project WismClient\Wism.Client.Terminal -- new world=Illuria
```

Useful options:

- `world=Illuria` selects the world.
- `profile=classic-warlords` selects the game profile.
- `packs=a,b` enables feature packs.
- `mode=detailed` changes tile rendering detail.
- `--agent` enables agent-script friendly command handling.
- `--json` emits JSONL records.
- `--no-color` disables console colors.
- `--no-animation` disables animation delays.
- `record=path` writes terminal replay records.
- `out=path` writes command output.

The terminal client is intended as an inspectable play/debug surface. WismUnity
remains the primary visual client.
