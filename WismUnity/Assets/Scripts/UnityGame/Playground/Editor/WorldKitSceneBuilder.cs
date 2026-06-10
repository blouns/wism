using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Editors;
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
        const string TargetScenePath = "Assets/Scenes/Near-Illuria.unity";
        const string WorldRoot = "Assets/Plugins/WismClient/Mods/Worlds/Near-Illuria";

        [MenuItem("WISM/World Kit/Build Near-Illuria Scene")]
        public static void BuildNearIlluriaScene()
        {
            BuildNearIlluriaSceneInternal(true);
        }

        public static void BuildNearIlluriaSceneFromCommandLine()
        {
            try
            {
                BuildNearIlluriaSceneInternal(false);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        static void BuildNearIlluriaSceneInternal(bool interactive)
        {
            if (!File.Exists(SourceScenePath))
            {
                throw new FileNotFoundException("Source scene was not found.", SourceScenePath);
            }

            if (interactive && File.Exists(TargetScenePath))
            {
                var replace = EditorUtility.DisplayDialog(
                    "Build Near-Illuria Scene",
                    "Replace the generated Near-Illuria scene?",
                    "Replace",
                    "Cancel");
                if (!replace)
                {
                    return;
                }
            }

            AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath);
            AssetDatabase.ImportAsset(TargetScenePath);
            var scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            var tilemap = FindWorldTilemap(scene);
            var tiles = LoadMapTiles(Path.Combine(WorldRoot, "Map.json"));
            var cities = LoadRecords(Path.Combine(WorldRoot, "City.json"));
            var locations = LoadRecords(Path.Combine(WorldRoot, "Location.json"));

            PaintTerrain(tilemap, tiles);
            RebuildCityObjects(scene, tilemap, cities);
            RebuildLocationObjects(scene, tilemap, locations);
            EnsureBuildSettings(TargetScenePath);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TargetScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Built WISM world kit scene at {TargetScenePath}");
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
                go.transform.position = new Vector3(x, y, 0);
                go.transform.localScale = new Vector3(1f, 1f, 1f);
                go.AddComponent<LocationEntry>().locationShortName = shortName;
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
    }
}
