using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Editors;
using Assets.Scripts.Managers;
using Assets.Scripts.Tiles;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace WismUnity.Playground
{
    public static class WorldKitSceneBuilder
    {
        const string SourceScenePath = "Assets/Scenes/Mini-Illuria.unity";
        const string PluginWorldsRoot = "Assets/Plugins/WismClient/Mods/Worlds";
        const string CityMarkerPrefabPath = "Assets/Prefab/Editor/City.prefab";
        const string LibraryMarkerPrefabPath = "Assets/Prefab/Editor/Library.prefab";
        const string RuinsMarkerPrefabPath = "Assets/Prefab/Editor/Ruins.prefab";
        const string SageMarkerPrefabPath = "Assets/Prefab/Editor/Sage.prefab";
        const string TempleMarkerPrefabPath = "Assets/Prefab/Editor/Temple.prefab";
        const string TombMarkerPrefabPath = "Assets/Prefab/Editor/Tomb.prefab";

        [MenuItem("WISM/World Kit/Build Near-Illuria Scene")]
        public static void BuildNearIlluriaScene()
        {
            BuildWorldScene("Near-Illuria", "Assets/Scenes/Near-Illuria.unity", true);
        }

        public static void BuildNearIlluriaSceneFromCommandLine()
        {
            RunCommandLine("Near-Illuria", "Assets/Scenes/Near-Illuria.unity");
        }

        [MenuItem("WISM/World Kit/Build Illuria Scene")]
        public static void BuildIlluriaScene()
        {
            BuildWorldScene("Illuria", "Assets/Scenes/Illuria.unity", true);
        }

        public static void BuildIlluriaSceneFromCommandLine()
        {
            RunCommandLine("Illuria", "Assets/Scenes/Illuria.unity");
        }

        public static void BuildWorldSceneFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            var world = ReadArg(args, "world", "Illuria");
            var scene = ReadArg(args, "scene", $"Assets/Scenes/{world}.unity");
            RunCommandLine(world, scene);
        }

        public static void VerifyWorldSceneFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            var world = ReadArg(args, "world", "Illuria");
            var scene = ReadArg(args, "scene", $"Assets/Scenes/{world}.unity");
            var output = ReadArg(args, "out", $"artifacts/world-kit/{world.ToLowerInvariant()}-scene-proof.json");

            try
            {
                var proof = VerifyWorldScene(world, scene);
                var outputDirectory = Path.GetDirectoryName(output);
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(output, proof.ToString(), Encoding.UTF8);
                Debug.Log($"Wrote WISM world scene proof at {output}");
                EditorApplication.Exit(proof.Value<string>("status") == "Passed" ? 0 : 1);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorApplication.Exit(1);
            }
        }

        public static void CaptureWorldSceneFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            var world = ReadArg(args, "world", "Illuria");
            var scene = ReadArg(args, "scene", $"Assets/Scenes/{world}.unity");
            var png = ReadArg(args, "png", $"artifacts/world-kit/{world.ToLowerInvariant()}-scene-capture.png");
            var manifest = ReadArg(args, "manifest", $"artifacts/world-kit/{world.ToLowerInvariant()}-scene-capture.json");
            var pixelsPerCell = ReadIntArg(args, "pixelsPerCell", 8);

            try
            {
                var proof = CaptureWorldScene(world, scene, png, pixelsPerCell);
                WriteJson(manifest, proof);
                Debug.Log($"Wrote WISM world scene capture at {png}");
                Debug.Log($"Wrote WISM world scene capture manifest at {manifest}");
                EditorApplication.Exit(proof.Value<string>("status") == "Passed" ? 0 : 1);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorApplication.Exit(1);
            }
        }

        static void RunCommandLine(string world, string scene)
        {
            try
            {
                BuildWorldScene(world, scene, false);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        static void BuildWorldScene(string world, string targetScenePath, bool interactive)
        {
            if (!File.Exists(SourceScenePath))
            {
                throw new FileNotFoundException("Source scene was not found.", SourceScenePath);
            }

            if (interactive && File.Exists(targetScenePath))
            {
                var replace = EditorUtility.DisplayDialog(
                    $"Build {world} Scene",
                    $"Replace the generated {world} scene?",
                    "Replace",
                    "Cancel");
                if (!replace)
                {
                    return;
                }
            }

            AssetDatabase.CopyAsset(SourceScenePath, targetScenePath);
            AssetDatabase.ImportAsset(targetScenePath);
            var scene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Additive);
            try
            {
                var tilemap = FindWorldTilemap(scene);
                var worldRoot = Path.Combine(PluginWorldsRoot, world);
                var tiles = LoadMapTiles(Path.Combine(worldRoot, "Map.json"));
                var cities = LoadRecords(Path.Combine(worldRoot, "City.json"));
                var locations = LoadRecords(Path.Combine(worldRoot, "Location.json"));
                var siteAnchors = LoadOptionalRecords(Path.Combine(worldRoot, "SiteAnchor.json"));

                ConfigureGeneratedContainers(scene, cities.Count, locations.Count);
                NormalizeGeneratedScene(scene, tilemap, world);
                ClearEditorTileCaches();
                var mapHeight = ComputeMapHeight(tiles);
                PaintTerrain(tilemap, tiles, mapHeight);
                RebuildCityObjects(scene, tilemap, cities, mapHeight);
                RebuildLocationObjects(scene, tilemap, locations, mapHeight);
                RebuildSiteAnchorObjects(scene, siteAnchors, mapHeight);
                EnsureBuildSettings(targetScenePath);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, targetScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Built WISM world kit scene at {targetScenePath}");
            }
            finally
            {
                if (!interactive)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static JObject VerifyWorldScene(string world, string scenePath)
        {
            if (!File.Exists(scenePath))
            {
                throw new FileNotFoundException("Scene was not found.", scenePath);
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var tilemap = FindWorldTilemap(scene);
                var worldRoot = Path.Combine(PluginWorldsRoot, world);
                var tiles = LoadMapTiles(Path.Combine(worldRoot, "Map.json"));
                var cities = LoadRecords(Path.Combine(worldRoot, "City.json"));
                var locations = LoadRecords(Path.Combine(worldRoot, "Location.json"));
                var siteAnchors = LoadOptionalRecords(Path.Combine(worldRoot, "SiteAnchor.json"));
                ConfigureGeneratedContainers(scene, cities.Count, locations.Count);
                ClearEditorTileCaches();
                var bounds = tilemap.cellBounds;
                var tileBlocks = tilemap.GetTilesBlock(bounds);
                var populatedTiles = tileBlocks.Count(tile => tile != null);
                var cityContainer = FindRoot(scene, "Cities");
                var locationContainer = FindRoot(scene, "Locations");
                var siteAnchorContainer = FindOrCreateRoot(scene, "SiteAnchors");
                var cityEntries = GetDirectChildComponents<CityEntry>(cityContainer)
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.cityShortName))
                    .ToArray();
                var locationEntries = GetDirectChildComponents<LocationEntry>(locationContainer)
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.locationShortName))
                    .ToArray();
                var siteAnchorEntries = GetDirectChildComponents<IlluriaSiteAnchorEntry>(siteAnchorContainer)
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.siteAnchorId))
                    .ToArray();
                var expectedCityNames = cities
                    .Select(city => city.Value<string>("ShortName"))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var expectedLocationNames = locations
                    .Select(location => location.Value<string>("ShortName"))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var expectedSiteAnchorIds = siteAnchors
                    .Select(anchor => anchor.Value<string>("AnchorId"))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var sceneCityNames = cityEntries
                    .Select(entry => entry.cityShortName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var sceneLocationNames = locationEntries
                    .Select(entry => entry.locationShortName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var sceneSiteAnchorIds = siteAnchorEntries
                    .Select(entry => entry.siteAnchorId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var mapHeight = ComputeMapHeight(tiles);
                var grid = scene.GetRootGameObjects()
                    .FirstOrDefault(go => string.Equals(go.name, "Grid", StringComparison.OrdinalIgnoreCase));

                var checks = new JArray();
                var issues = new JArray();
                AddSceneCheck(bounds.xMin == 0 && bounds.yMin == 0, checks, issues, "Tilemap origin is 0,0.", $"Tilemap origin is {bounds.xMin},{bounds.yMin}.");
                AddSceneCheck(bounds.size.x == 109 && bounds.size.y == 156, checks, issues, "Tilemap bounds are 109x156.", $"Tilemap bounds are {bounds.size.x}x{bounds.size.y}.");
                AddSceneCheck(populatedTiles == tiles.Count, checks, issues, $"Tilemap has {tiles.Count} populated tiles.", $"Tilemap has {populatedTiles}/{tiles.Count} populated tiles.");
                AddSceneCheck(cityEntries.Length == cities.Count, checks, issues, $"Scene has {cities.Count} city markers.", $"Scene has {cityEntries.Length}/{cities.Count} city markers.");
                AddSceneCheck(locationEntries.Length == locations.Count, checks, issues, $"Scene has {locations.Count} location markers.", $"Scene has {locationEntries.Length}/{locations.Count} location markers.");
                AddSceneCheck(siteAnchorEntries.Length == siteAnchors.Count, checks, issues, $"Scene has {siteAnchors.Count} structural site anchor markers.", $"Scene has {siteAnchorEntries.Length}/{siteAnchors.Count} structural site anchor markers.");
                AddSceneCheck(sceneCityNames.SetEquals(expectedCityNames), checks, issues, "Scene city marker names match MOD data.", "Scene city marker names do not match MOD data.");
                AddSceneCheck(sceneLocationNames.SetEquals(expectedLocationNames), checks, issues, "Scene location marker names match MOD data.", "Scene location marker names do not match MOD data.");
                AddSceneCheck(sceneSiteAnchorIds.SetEquals(expectedSiteAnchorIds), checks, issues, "Scene site anchor marker ids match MOD data.", "Scene site anchor marker ids do not match MOD data.");
                AddSceneCheck(FindSceneGameObjects(scene, "Cities").Count == 1, checks, issues, "Scene has one Cities container.", $"Scene has {FindSceneGameObjects(scene, "Cities").Count} Cities containers.");
                AddSceneCheck(FindSceneGameObjects(scene, "Locations").Count == 1, checks, issues, "Scene has one Locations container.", $"Scene has {FindSceneGameObjects(scene, "Locations").Count} Locations containers.");
                AddSceneCheck(cityContainer.GetComponent<CityContainer>() != null, checks, issues, "Cities container has CityContainer settings.", "Cities container is missing CityContainer settings.");
                AddSceneCheck(locationContainer.GetComponent<LocationContainer>() != null, checks, issues, "Locations container has LocationContainer settings.", "Locations container is missing LocationContainer settings.");
                AddSceneCheck(CityMarkersAreRuntimeReady(cityEntries), checks, issues, "City markers have City tag, CityEntry, and SpriteRenderer.", "One or more city markers are missing City tag, CityEntry, or SpriteRenderer.");
                AddSceneCheck(LocationMarkersAreRuntimeReady(locationEntries), checks, issues, "Location markers have Location tag, LocationEntry, and SpriteRenderer.", "One or more location markers are missing Location tag, LocationEntry, or SpriteRenderer.");
                AddSceneCheck(grid == null || IsZeroTransform(grid.transform), checks, issues, "Generated Grid transform is normalized.", "Generated Grid transform is not normalized; copied scene offset is still present.");
                AddSceneCheck(IsZeroLocalTransform(tilemap.transform), checks, issues, "Generated WorldTilemap local transform is normalized.", "Generated WorldTilemap local transform is not normalized.");
                AddSceneCheck(CityMarkerPositionsMatch(cityEntries, cities, mapHeight), checks, issues, "Scene city marker positions match flipped MOD coordinates.", "Scene city marker positions do not match flipped MOD coordinates.");
                AddSceneCheck(LocationMarkerPositionsMatch(locationEntries, locations, mapHeight), checks, issues, "Scene location marker positions match flipped MOD coordinates.", "Scene location marker positions do not match flipped MOD coordinates.");
                AddSceneCheck(SiteAnchorPositionsMatch(siteAnchorEntries, siteAnchors, mapHeight), checks, issues, "Scene site anchor marker positions match flipped MOD coordinates.", "Scene site anchor marker positions do not match flipped MOD coordinates.");
                AddSceneCheck(CityFootprintsArePainted(tilemap, cities, mapHeight), checks, issues, "City markers line up with painted 2x2 city footprints.", "One or more city markers do not line up with painted 2x2 city footprints.");
                AddSceneCheck(LocationTilesArePainted(tilemap, locations, mapHeight), checks, issues, "Location markers line up with painted location tiles.", "One or more location markers do not line up with painted location tiles.");
                AddSceneCheck(
                    SceneUsesGeneratedWorldDefaults(scene, world),
                    checks,
                    issues,
                    "Scene GameManager and UnityGameFactory use generated world defaults.",
                    "Scene GameManager or UnityGameFactory still points at stale world/mod defaults.");

                return new JObject
                {
                    ["schemaVersion"] = "wism.unity.world-scene-proof.v1",
                    ["generatedAtUtc"] = DateTime.UtcNow.ToString("O"),
                    ["status"] = issues.Count == 0 ? "Passed" : "Failed",
                    ["world"] = world,
                    ["scene"] = scenePath,
                    ["tilemap"] = new JObject
                    {
                        ["originX"] = bounds.xMin,
                        ["originY"] = bounds.yMin,
                        ["width"] = bounds.size.x,
                        ["height"] = bounds.size.y,
                        ["populatedTiles"] = populatedTiles,
                        ["expectedTiles"] = tiles.Count
                    },
                    ["objects"] = new JObject
                    {
                        ["cityMarkers"] = cityEntries.Length,
                        ["expectedCities"] = cities.Count,
                        ["locationMarkers"] = locationEntries.Length,
                        ["expectedLocations"] = locations.Count,
                        ["siteAnchorMarkers"] = siteAnchorEntries.Length,
                        ["expectedSiteAnchors"] = siteAnchors.Count
                    },
                    ["checks"] = checks,
                    ["issues"] = issues
                };
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static JObject CaptureWorldScene(string world, string scenePath, string pngPath, int pixelsPerCell)
        {
            if (!File.Exists(scenePath))
            {
                throw new FileNotFoundException("Scene was not found.", scenePath);
            }

            if (pixelsPerCell <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pixelsPerCell), "Pixels per cell must be positive.");
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            Camera camera = null;
            RenderTexture target = null;
            Texture2D image = null;
            GameObject cameraObject = null;
            try
            {
                var tilemap = FindWorldTilemap(scene);
                var bounds = tilemap.cellBounds;
                var renderBounds = GetRenderBounds(tilemap);
                var width = bounds.size.x * pixelsPerCell;
                var height = bounds.size.y * pixelsPerCell;
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException($"Scene tilemap bounds are invalid: {bounds.size.x}x{bounds.size.y}.");
                }

                cameraObject = new GameObject("World Kit Capture Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.aspect = (float)width / height;
                camera.orthographicSize = Mathf.Max(renderBounds.extents.y, renderBounds.extents.x / camera.aspect);
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.transform.position = new Vector3(renderBounds.center.x, renderBounds.center.y, -10f);

                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();

                var directory = Path.GetDirectoryName(pngPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(pngPath, image.EncodeToPNG());

                var fileInfo = new FileInfo(pngPath);
                return new JObject
                {
                    ["schemaVersion"] = "wism.unity.world-scene-capture.v1",
                    ["generatedAtUtc"] = DateTime.UtcNow.ToString("O"),
                    ["status"] = fileInfo.Exists && fileInfo.Length > 0 ? "Passed" : "Failed",
                    ["world"] = world,
                    ["scene"] = scenePath,
                    ["png"] = pngPath,
                    ["pngBytes"] = fileInfo.Exists ? fileInfo.Length : 0,
                    ["pixelsPerCell"] = pixelsPerCell,
                    ["image"] = new JObject
                    {
                        ["width"] = width,
                        ["height"] = height
                    },
                    ["tilemap"] = new JObject
                    {
                        ["originX"] = bounds.xMin,
                        ["originY"] = bounds.yMin,
                        ["width"] = bounds.size.x,
                        ["height"] = bounds.size.y
                    },
                    ["renderBounds"] = new JObject
                    {
                        ["centerX"] = renderBounds.center.x,
                        ["centerY"] = renderBounds.center.y,
                        ["width"] = renderBounds.size.x,
                        ["height"] = renderBounds.size.y
                    }
                };
            }
            finally
            {
                RenderTexture.active = null;
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }

                if (image != null)
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }

                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static Bounds GetRenderBounds(Tilemap tilemap)
        {
            var renderer = tilemap.GetComponent<Renderer>();
            if (renderer != null && renderer.bounds.size.x > 0f && renderer.bounds.size.y > 0f)
            {
                return renderer.bounds;
            }

            var bounds = tilemap.cellBounds;
            return new Bounds(
                new Vector3(bounds.xMin + bounds.size.x / 2f, bounds.yMin + bounds.size.y / 2f, 0f),
                new Vector3(bounds.size.x, bounds.size.y, 1f));
        }

        static Tilemap FindWorldTilemap(Scene scene)
        {
            var tilemap = scene.GetRootGameObjects()
                .SelectMany(Flatten)
                .Where(go => go.CompareTag("WorldTilemap") || string.Equals(go.name, "WorldTilemap", StringComparison.OrdinalIgnoreCase))
                .Select(go => go.GetComponent<Tilemap>())
                .FirstOrDefault(component => component != null);

            if (tilemap == null)
            {
                throw new InvalidOperationException("Could not find WorldTilemap in generated scene.");
            }

            return tilemap;
        }

        static void PaintTerrain(Tilemap tilemap, IReadOnlyList<JObject> tiles, int mapHeight)
        {
            tilemap.ClearAllTiles();
            var tileAssets = LoadTileAssets();
            foreach (var tile in tiles)
            {
                var terrain = tile.Value<string>("TerrainShortName") ?? "Grass";
                if (!tileAssets.TryGetValue(terrain, out var tileAsset))
                {
                    tileAsset = tileAssets["Grass"];
                }

                tilemap.SetTile(new Vector3Int(tile.Value<int>("X"), ToUnityY(tile.Value<int>("Y"), mapHeight), 0), tileAsset);
            }

            tilemap.CompressBounds();
            tilemap.RefreshAllTiles();
        }

        static void RebuildCityObjects(Scene scene, Tilemap tilemap, IReadOnlyList<JObject> cities, int mapHeight)
        {
            var container = EnsureSceneContainer<CityContainer>(scene, "Cities");
            ClearChildren(container);
            var tileAssets = LoadTileAssets();
            var markerPrefab = GetCityMarkerPrefab(container);

            foreach (var city in cities)
            {
                var x = city.Value<int>("X");
                var y = ToUnityY(city.Value<int>("Y"), mapHeight);
                var clan = city.Value<string>("ClanName") ?? "Neutral";
                var shortName = city.Value<string>("ShortName") ?? "City";
                var cityTile = FindCityTile(tileAssets, clan);
                tilemap.SetTile(new Vector3Int(x, y, 0), cityTile);
                tilemap.SetTile(new Vector3Int(x, y - 1, 0), cityTile);
                tilemap.SetTile(new Vector3Int(x + 1, y, 0), cityTile);
                tilemap.SetTile(new Vector3Int(x + 1, y - 1, 0), cityTile);

                var go = InstantiateMarker(markerPrefab, scene, container.transform, shortName);
                go.tag = "City";
                go.transform.position = GetCityFootprintCenter(tilemap, x, y);
                go.transform.localScale = new Vector3(2f, 2f, 1f);
                EnsureComponent<CityEntry>(go).cityShortName = shortName;
                EnsureComponent<SpriteRenderer>(go);
            }
        }

        static void RebuildLocationObjects(Scene scene, Tilemap tilemap, IReadOnlyList<JObject> locations, int mapHeight)
        {
            var container = EnsureSceneContainer<LocationContainer>(scene, "Locations");
            ClearChildren(container);
            var tileAssets = LoadTileAssets();
            foreach (var location in locations)
            {
                var x = location.Value<int>("X");
                var y = ToUnityY(location.Value<int>("Y"), mapHeight);
                var shortName = location.Value<string>("ShortName") ?? "Location";
                var terrain = location.Value<string>("Terrain") ?? location.Value<string>("Kind") ?? "Ruins";
                if (tileAssets.TryGetValue(terrain, out var tileAsset))
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tileAsset);
                }

                var go = InstantiateMarker(GetLocationMarkerPrefab(terrain), scene, container.transform, shortName);
                go.tag = "Location";
                go.transform.position = GetCellCenter(tilemap, new Vector3Int(x, y, 0));
                go.transform.localScale = Vector3.one;
                EnsureComponent<LocationEntry>(go).locationShortName = shortName;
                EnsureComponent<SpriteRenderer>(go);
            }
        }

        static void RebuildSiteAnchorObjects(Scene scene, IReadOnlyList<JObject> siteAnchors, int mapHeight)
        {
            var container = FindOrCreateRoot(scene, "SiteAnchors");
            ClearChildren(container);

            foreach (var anchor in siteAnchors)
            {
                var x = anchor.Value<int>("X");
                var y = ToUnityY(anchor.Value<int>("Y"), mapHeight);
                var anchorId = anchor.Value<string>("AnchorId") ?? anchor.Value<string>("ShortName") ?? "SiteAnchor";
                var go = new GameObject(anchorId);
                go.transform.SetParent(container.transform, false);
                go.transform.position = new Vector3(x, y, 0);
                go.transform.localScale = Vector3.one;

                var entry = go.AddComponent<IlluriaSiteAnchorEntry>();
                entry.siteAnchorId = anchorId;
                entry.mapByteOffset = anchor.Value<int?>("MapByteOffset") ?? 0;
                entry.rawWord = anchor.Value<string>("SourceRawWord") ?? string.Empty;
                entry.lowByte = anchor.Value<int?>("SourceRawLowByte") ?? 0;
                entry.highByte = anchor.Value<int?>("SourceRawHighByte") ?? 0;
                entry.terrain = anchor.Value<string>("Terrain") ?? string.Empty;
                entry.sourceNote = anchor.Value<string>("SourceNote") ?? string.Empty;
            }
        }

        static bool CityMarkerPositionsMatch(IEnumerable<CityEntry> entries, IReadOnlyList<JObject> cities, int mapHeight)
        {
            var expected = cities
                .Where(city => !string.IsNullOrWhiteSpace(city.Value<string>("ShortName")))
                .ToDictionary(
                    city => city.Value<string>("ShortName"),
                    city => GetCityFootprintCenter(city.Value<int>("X"), ToUnityY(city.Value<int>("Y"), mapHeight)),
                    StringComparer.OrdinalIgnoreCase);

            return entries.All(entry =>
                expected.TryGetValue(entry.cityShortName, out var position) &&
                PositionMatches(entry.transform.position, position));
        }

        static bool LocationMarkerPositionsMatch(IEnumerable<LocationEntry> entries, IReadOnlyList<JObject> locations, int mapHeight)
        {
            var expected = locations
                .Where(location => !string.IsNullOrWhiteSpace(location.Value<string>("ShortName")))
                .ToDictionary(
                    location => location.Value<string>("ShortName"),
                    location => new Vector2(location.Value<int>("X") + 0.5f, ToUnityY(location.Value<int>("Y"), mapHeight) + 0.5f),
                    StringComparer.OrdinalIgnoreCase);

            return entries.All(entry =>
                expected.TryGetValue(entry.locationShortName, out var position) &&
                PositionMatches(entry.transform.position, position));
        }

        static bool SiteAnchorPositionsMatch(IEnumerable<IlluriaSiteAnchorEntry> entries, IReadOnlyList<JObject> siteAnchors, int mapHeight)
        {
            var expected = siteAnchors
                .Where(anchor => !string.IsNullOrWhiteSpace(anchor.Value<string>("AnchorId")))
                .ToDictionary(
                    anchor => anchor.Value<string>("AnchorId"),
                    anchor => new Vector2(anchor.Value<int>("X"), ToUnityY(anchor.Value<int>("Y"), mapHeight)),
                    StringComparer.OrdinalIgnoreCase);

            return entries.All(entry =>
                expected.TryGetValue(entry.siteAnchorId, out var position) &&
                PositionMatches(entry.transform.position, position));
        }

        static bool CityMarkersAreRuntimeReady(IEnumerable<CityEntry> entries)
        {
            return entries.All(entry =>
                entry.CompareTag("City") &&
                entry.GetComponent<CityEntry>() != null &&
                entry.GetComponent<SpriteRenderer>() != null);
        }

        static bool LocationMarkersAreRuntimeReady(IEnumerable<LocationEntry> entries)
        {
            return entries.All(entry =>
                entry.CompareTag("Location") &&
                entry.GetComponent<LocationEntry>() != null &&
                entry.GetComponent<SpriteRenderer>() != null);
        }

        static bool CityFootprintsArePainted(Tilemap tilemap, IReadOnlyList<JObject> cities, int mapHeight)
        {
            return cities.All(city =>
            {
                var x = city.Value<int>("X");
                var y = ToUnityY(city.Value<int>("Y"), mapHeight);
                return tilemap.GetTile(new Vector3Int(x, y, 0)) is CityTile &&
                       tilemap.GetTile(new Vector3Int(x + 1, y, 0)) is CityTile &&
                       tilemap.GetTile(new Vector3Int(x, y - 1, 0)) is CityTile &&
                       tilemap.GetTile(new Vector3Int(x + 1, y - 1, 0)) is CityTile;
            });
        }

        static bool LocationTilesArePainted(Tilemap tilemap, IReadOnlyList<JObject> locations, int mapHeight)
        {
            return locations.All(location =>
                tilemap.GetTile(new Vector3Int(location.Value<int>("X"), ToUnityY(location.Value<int>("Y"), mapHeight), 0)) is LocationTile);
        }

        static bool PositionMatches(Vector3 actual, Vector2 expected)
        {
            return Mathf.Approximately(actual.x, expected.x) &&
                   Mathf.Approximately(actual.y, expected.y);
        }

        static bool IsZeroTransform(Transform transform)
        {
            return IsZeroLocalTransform(transform) &&
                   Mathf.Approximately(transform.position.x, 0f) &&
                   Mathf.Approximately(transform.position.y, 0f) &&
                   Mathf.Approximately(transform.position.z, 0f);
        }

        static bool IsZeroLocalTransform(Transform transform)
        {
            return Mathf.Approximately(transform.localPosition.x, 0f) &&
                   Mathf.Approximately(transform.localPosition.y, 0f) &&
                   Mathf.Approximately(transform.localPosition.z, 0f) &&
                   Mathf.Approximately(transform.localScale.x, 1f) &&
                   Mathf.Approximately(transform.localScale.y, 1f) &&
                   Mathf.Approximately(transform.localScale.z, 1f);
        }

        static void NormalizeGeneratedScene(Scene scene, Tilemap tilemap, string world)
        {
            var grid = scene.GetRootGameObjects()
                .FirstOrDefault(go => string.Equals(go.name, "Grid", StringComparison.OrdinalIgnoreCase));
            if (grid != null)
            {
                grid.transform.position = Vector3.zero;
                grid.transform.localPosition = Vector3.zero;
                grid.transform.localScale = Vector3.one;
                grid.transform.rotation = Quaternion.identity;
            }

            tilemap.transform.localPosition = Vector3.zero;
            tilemap.transform.localScale = Vector3.one;
            tilemap.transform.localRotation = Quaternion.identity;

            foreach (var gameManager in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameManager>(true)))
            {
                gameManager.WorldName = world;
                gameManager.ModPath = GameManager.DefaultModPath;
            }

            foreach (var gameFactory in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UnityGameFactory>(true)))
            {
                gameFactory.WorldName = world;
                gameFactory.ModPath = GameManager.DefaultModPath;
            }
        }

        static bool SceneUsesGeneratedWorldDefaults(Scene scene, string world)
        {
            var gameManagers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GameManager>(true))
                .ToArray();
            var gameFactories = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<UnityGameFactory>(true))
                .ToArray();

            return gameManagers.Length > 0 &&
                   gameManagers.All(manager =>
                       string.Equals(manager.WorldName, world, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(manager.ModPath, GameManager.DefaultModPath, StringComparison.OrdinalIgnoreCase)) &&
                   gameFactories.All(factory =>
                       string.Equals(factory.WorldName, world, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(factory.ModPath, GameManager.DefaultModPath, StringComparison.OrdinalIgnoreCase));
        }

        static int ComputeMapHeight(IReadOnlyList<JObject> tiles)
        {
            return tiles.Count == 0
                ? 0
                : tiles.Max(tile => tile.Value<int>("Y")) + 1;
        }

        static int ToUnityY(int sourceY, int mapHeight)
        {
            return mapHeight - 1 - sourceY;
        }

        static Vector3 GetCellCenter(Tilemap tilemap, Vector3Int cell)
        {
            var origin = tilemap.CellToWorld(cell);
            return new Vector3(
                origin.x + tilemap.cellSize.x * 0.5f,
                origin.y + tilemap.cellSize.y * 0.5f,
                origin.z);
        }

        static Vector3 GetCityFootprintCenter(Tilemap tilemap, int x, int topY)
        {
            var origin = tilemap.CellToWorld(new Vector3Int(x, topY, 0));
            return new Vector3(
                origin.x + tilemap.cellSize.x,
                origin.y,
                origin.z);
        }

        static Vector2 GetCityFootprintCenter(int x, int topY)
        {
            return new Vector2(x + 1, topY);
        }

        static GameObject EnsureSceneContainer<T>(Scene scene, string name) where T : Component
        {
            var matches = FindSceneGameObjects(scene, name);
            foreach (var match in matches)
            {
                UnityEngine.Object.DestroyImmediate(match);
            }

            var keeper = new GameObject(name);
            SceneManager.MoveGameObjectToScene(keeper, scene);
            keeper.AddComponent<T>();
            return keeper;
        }

        static GameObject InstantiateMarker(GameObject prefab, Scene scene, Transform parent, string name)
        {
            var marker = prefab != null
                ? PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject
                : new GameObject(name);

            marker ??= new GameObject(name);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            return marker;
        }

        static GameObject GetCityMarkerPrefab(GameObject cityContainer)
        {
            return cityContainer.GetComponent<CityContainer>()?.CityPrefab
                ?? AssetDatabase.LoadAssetAtPath<GameObject>(CityMarkerPrefabPath);
        }

        static GameObject GetLocationMarkerPrefab(string terrain)
        {
            return (terrain ?? string.Empty).ToLowerInvariant() switch
            {
                "library" => AssetDatabase.LoadAssetAtPath<GameObject>(LibraryMarkerPrefabPath),
                "sage" => AssetDatabase.LoadAssetAtPath<GameObject>(SageMarkerPrefabPath),
                "temple" => AssetDatabase.LoadAssetAtPath<GameObject>(TempleMarkerPrefabPath),
                "tomb" => AssetDatabase.LoadAssetAtPath<GameObject>(TombMarkerPrefabPath),
                _ => AssetDatabase.LoadAssetAtPath<GameObject>(RuinsMarkerPrefabPath)
            };
        }

        static T EnsureComponent<T>(GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        static List<GameObject> FindSceneGameObjects(Scene scene, string name)
        {
            var matches = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                FindNamedObjects(root.transform, name, matches);
            }

            return matches;
        }

        static void FindNamedObjects(Transform transform, string name, ICollection<GameObject> matches)
        {
            if (string.Equals(transform.name, name, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(transform.gameObject);
            }

            foreach (Transform child in transform)
            {
                FindNamedObjects(child, name, matches);
            }
        }

        static Dictionary<string, TileBase> LoadTileAssets()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Grass"] = "Assets/PalletTiles/grass.asset",
                ["Water"] = "Assets/PalletTiles/water_tile.asset",
                ["Forest"] = "Assets/PalletTiles/forest_tile.asset",
                ["Mountain"] = "Assets/PalletTiles/mountain_tile.asset",
                ["Hill"] = "Assets/PalletTiles/hill_tile.asset",
                ["Road"] = "Assets/PalletTiles/road_tile.asset",
                ["Bridge"] = "Assets/PalletTiles/bridge_tile.asset",
                ["Marsh"] = "Assets/PalletTiles/marsh.asset",
                ["Library"] = "Assets/PalletTiles/library_tile.asset",
                ["Ruins"] = "Assets/PalletTiles/ruins_tile.asset",
                ["Sage"] = "Assets/PalletTiles/sage_tile.asset",
                ["Temple"] = "Assets/PalletTiles/temple_tile.asset",
                ["Tomb"] = "Assets/PalletTiles/tomb_tile.asset",
                ["Tower"] = "Assets/PalletTiles/tower/neutral_tower.asset",
                ["Castle"] = "Assets/PalletTiles/city/neutral_castle_city_tile.asset",
                ["NeutralCastle"] = "Assets/PalletTiles/city/neutral_castle_city_tile.asset"
            };

            foreach (var clan in new[] { "Sirians", "StormGiants", "GreyDwarves", "OrcsOfKor", "Elvallie", "Selentines", "HorseLords", "LordBane" })
            {
                map[clan + "Castle"] = "Assets/PalletTiles/city/" + clan.ToLowerInvariant() + "_castle_city_tile.asset";
            }

            var assets = new Dictionary<string, TileBase>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in map)
            {
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(pair.Value);
                if (tile != null)
                {
                    assets[pair.Key] = tile;
                }
            }

            if (!assets.ContainsKey("Grass"))
            {
                throw new InvalidOperationException("Could not load grass tile asset.");
            }

            return assets;
        }

        static TileBase FindCityTile(IDictionary<string, TileBase> tileAssets, string clan)
        {
            if (!string.IsNullOrWhiteSpace(clan) && tileAssets.TryGetValue(clan + "Castle", out var tile))
            {
                return tile;
            }

            return tileAssets.TryGetValue("NeutralCastle", out var neutral)
                ? neutral
                : tileAssets["Grass"];
        }

        static GameObject FindOrCreateRoot(Scene scene, string name)
        {
            var existing = scene.GetRootGameObjects()
                .FirstOrDefault(go => string.Equals(go.name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return existing;
            }

            var created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .FirstOrDefault(go => string.Equals(go.name, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Could not find root GameObject '{name}' in scene '{scene.path}'.");
        }

        static IEnumerable<T> GetDirectChildComponents<T>(GameObject container) where T : Component
        {
            foreach (Transform child in container.transform)
            {
                var component = child.GetComponent<T>();
                if (component != null)
                {
                    yield return component;
                }
            }
        }

        static void ConfigureGeneratedContainers(Scene scene, int cityCount, int locationCount)
        {
            var cityContainer = FindOrCreateRoot(scene, "Cities");
            var citySettings = cityContainer.GetComponent<CityContainer>();
            if (citySettings != null)
            {
                citySettings.ImportCitesFromTilemap = false;
                citySettings.Reset = false;
                citySettings.TotalCities = cityCount;
            }

            var locationContainer = FindOrCreateRoot(scene, "Locations");
            var locationSettings = locationContainer.GetComponent<LocationContainer>();
            if (locationSettings != null)
            {
                locationSettings.ImportLocationsFromTilemap = false;
                locationSettings.Reset = false;
                locationSettings.TotalLocations = locationCount;
            }
        }

        static void ClearEditorTileCaches()
        {
            CityTile.ClearCityCache();
            LocationTile.ClearLocationCache();
        }

        static void ClearChildren(GameObject container)
        {
            for (var index = container.transform.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(container.transform.GetChild(index).gameObject);
            }
        }

        static IReadOnlyList<JObject> LoadMapTiles(string path)
        {
            var token = JToken.Parse(File.ReadAllText(path));
            return (token.Type == JTokenType.Array ? (JArray)token : token["Tiles"] as JArray)
                ?.OfType<JObject>()
                .ToArray()
                ?? throw new InvalidDataException("Map.json must contain a Tiles array.");
        }

        static IReadOnlyList<JObject> LoadRecords(string path)
        {
            return JArray.Parse(File.ReadAllText(path))
                .OfType<JObject>()
                .ToArray();
        }

        static IReadOnlyList<JObject> LoadOptionalRecords(string path)
        {
            return File.Exists(path)
                ? LoadRecords(path)
                : Array.Empty<JObject>();
        }

        static void WriteJson(string path, JObject json)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, json.ToString(), Encoding.UTF8);
        }

        static IEnumerable<GameObject> Flatten(GameObject root)
        {
            yield return root;
            foreach (Transform child in root.transform)
            {
                foreach (var item in Flatten(child.gameObject))
                {
                    yield return item;
                }
            }
        }

        static void EnsureBuildSettings(string scenePath)
        {
            var existing = EditorBuildSettings.scenes
                .Select(scene => scene.path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList();
            if (!existing.Contains(scenePath, StringComparer.OrdinalIgnoreCase))
            {
                existing.Add(scenePath);
            }

            EditorBuildSettings.scenes = existing
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        static void AddSceneCheck(bool passed, JArray checks, JArray issues, string check, string issue)
        {
            if (passed)
            {
                checks.Add(check);
            }
            else
            {
                issues.Add(issue);
            }
        }

        static string ReadArg(string[] args, string name, string fallback)
        {
            var prefix = name + "=";
            var value = args.FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Substring(prefix.Length);
        }

        static int ReadIntArg(string[] args, string name, int fallback)
        {
            var value = ReadArg(args, name, string.Empty);
            return int.TryParse(value, out var parsed) ? parsed : fallback;
        }
    }
}
