# WismUnity

WismUnity is the Unity frontend for WISM. It renders and drives the game through
the public WismClient engine assemblies instead of owning gameplay rules directly.

## WismClient Plugin Assemblies

WismUnity consumes WismClient through committed plugin assemblies under:

```text
Assets/Plugins/WismClient/
```

These DLLs are intentionally committed so a contributor can clone the repository,
open `WismUnity/` in Unity, and get a working editor project without running a
separate setup step first.

Treat these DLLs as generated shipped artifacts:

- Do not edit DLLs directly.
- Do not commit DLL changes caused by an incidental local build.
- Do commit DLL changes when intentionally republishing WismClient changes for
  Unity consumption.
- When DLL changes are intentional, include them in the same change as the
  source change that required the Unity plugin refresh.

## Republishing WismClient

WismClient projects import `WismClient/Build/Wism.PublishToUnity.targets`.
Release builds copy the relevant WismClient assemblies into
`WismUnity/Assets/Plugins/WismClient/`. Mod files are copied when the project
enables mod publication.

From the repository root:

```powershell
dotnet build WismClient\WismClient.sln --configuration Release
git status --short
```

Review any changes under `WismUnity/Assets/Plugins/WismClient/` before committing.
If the DLL changes were not part of the intended work, restore them before the
public commit. If the DLL changes were intended, validate Unity after publishing.

## Unity Validation

Use the Unity version noted in the root README. A local editor import is usually
enough for documentation-only changes. For plugin, package, editor bridge, or
runtime changes, run a Unity batchmode import or an equivalent Unity editor
validation before marking the change ready.

Example shape:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' `
  -batchmode `
  -quit `
  -projectPath .\WismUnity `
  -logFile .\WismUnity\Logs\unity-import.log
```

Keep Unity-generated local folders such as `Library/`, `Temp/`, `Logs/`, and
`UserSettings/` out of public commits unless a specific task explicitly owns
that artifact.
