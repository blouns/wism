using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Scripts.Editors;
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
                ClearEditorTileCaches();
                PaintTerrain(tilemap, tiles);
                RebuildCityObjects(scene, tilemap, cities);
                RebuildLocationObjects(scene, tilemap, locations);
                RebuildSiteAnchorObjects(scene, siteAnchors);
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

        static void PaintTerrain(Tilemap tilemap, IReadOnlyList<JObject> tiles)
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

                tilemap.SetTile(new Vector3Int(tile.Value<int>("X"), tile.Value<int>("Y"), 0), tileAsset);
            }

            tilemap.CompressBounds();
            tilemap.RefreshAllTiles();
        }

        static void RebuildCityObjects(Scene scene, Tilemap tilemap, IReadOnlyList<JObject> cities)
        {
            var container = FindOrCreateRoot(scene, "Cities");
            ClearChildren(container);
            var tileAssets = LoadTileAssets();

            foreach (var city in cities)
            {
                var x = city.Value<int>("X");
                var y = city.Value<int>("Y");
                var clan = city.Value<string>("ClanName") ?? "Neutral";
                var shortName = city.Value<string>("ShortName") ?? "City";
                var cityTile = FindCityTile(tileAssets, clan);
                tilemap.SetTile(new Vector3Int(x, y, 0), cityTile);
                tilemap.SetTile(new Vector3Int(x, y - 1, 0), cityTile);
                tilemap.SetTile(new Vector3Int(x + 1, y, 0), cityTile);
                tilemap.SetTile(new Vector3Int(x + 1, y - 1, 0), cityTile);

                var go = new GameObject(shortName);
                go.transform.SetParent(container.transform, false);
                go.transform.position = new Vector3(x, y - 1, 0);
                go.transform.localScale = new Vector3(2f, 2f, 1f);
                go.AddComponent<CityEntry>().cityShortName = shortName;
            }
        }

        static void RebuildLocationObjects(Scene scene, Tilemap tilemap, IReadOnlyList<JObject> locations)
        {
            var container = FindOrCreateRoot(scene, "Locations");
            ClearChildren(container);
            var tileAssets = LoadTileAssets();

            foreach (var location in locations)
            {
                var x = location.Value<int>("X");
                var y = location.Value<int>("Y");
                var shortName = location.Value<string>("ShortName") ?? "Location";
                var terrain = location.Value<string>("Terrain") ?? location.Value<string>("Kind") ?? "Ruins";
                if (tileAssets.TryGetValue(terrain, out var tileAsset))
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tileAsset);
                }

                var go = new GameObject(shortName);
                go.transform.SetParent(container.transform, false);
                go.transform.position = new Vector3(x, y - 1, 0);
                go.transform.localScale = Vector3.one;
                go.AddComponent<LocationEntry>().locationShortName = shortName;
            }
        }

        static void RebuildSiteAnchorObjects(Scene scene, IReadOnlyList<JObject> siteAnchors)
        {
            var container = FindOrCreateRoot(scene, "SiteAnchors");
            ClearChildren(container);

            foreach (var anchor in siteAnchors)
            {
                var x = anchor.Value<int>("X");
                var y = anchor.Value<int>("Y");
                var anchorId = anchor.Value<string>("AnchorId") ?? anchor.Value<string>("ShortName") ?? "SiteAnchor";
                var go = new GameObject(anchorId);
                go.transform.SetParent(container.transform, false);
                go.transform.position = new Vector3(x, y - 1, 0);
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
