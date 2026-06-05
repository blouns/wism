Build WismClient and deploy the output DLLs to WismUnity and WismCompanion.

Steps:
1. Run `dotnet build WismClient.sln -c Release` — stop and report if this fails
2. Copy these 4 DLLs from their respective `bin/Release/netstandard2.1/` output folders to `../WismUnity/Assets/Plugins/WismClient/`:
   - `Wism.Client.Core/bin/Release/netstandard2.1/Wism.Client.Core.dll`
   - `Wism.Client.Api/bin/Release/netstandard2.1/Wism.Client.Api.dll`
   - `Wism.Client.AI/bin/Release/netstandard2.1/Wism.Client.AI.dll`
   - `Wism.Companion.Shared/bin/Release/netstandard2.1/Wism.Companion.Shared.dll`
3. Check if `../WismCompanion/Assets/Plugins/WismClient/` exists — if so, copy the same 4 DLLs there too
4. Report which files were copied and their timestamps

Note: WismUnity and WismCompanion may need to be closed in the Unity Editor before overwriting DLLs, or Unity will detect the change and reimport automatically.
