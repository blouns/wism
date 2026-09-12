using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CarmelTemplePlacementTests
{
    [Serializable] private class Locations { public Location[] Items; }
    [Serializable] private class Location
    {
        public string ShortName, DisplayName, Kind, Terrain;
        public int X, Y;
    }

    [Test]
    public void BothModCopiesRetainOneTempleOfIrisAtAuthoredCarmelOffset()
    {
        var root = Directory.GetParent(Application.dataPath).Parent.FullName;
        foreach (var path in new[] {
            "WismClient/Wism.Client.Core/mod/Worlds/Illuria/Location.json",
            "WismUnity/Assets/Plugins/WismClient/Mods/Worlds/Illuria/Location.json" })
        {
            var locations = JsonUtility.FromJson<Locations>("{\"Items\":" + File.ReadAllText(Path.Combine(root, path)) + "}").Items;
            Assert.That(locations.Length, Is.EqualTo(40), "Relocate the existing site; never duplicate it.");
            var temple = locations.Single(item => item.DisplayName == "Temple of Iris");
            Assert.That(temple.ShortName, Is.EqualTo("TempleofIris"));
            Assert.That(temple.Kind, Is.EqualTo("Temple"));
            Assert.That(temple.Terrain, Is.EqualTo("Temple"));
            Assert.That(new Vector2Int(temple.X, temple.Y), Is.EqualTo(new Vector2Int(22, 66)), path);
        }
    }

    [Test]
    public void ShippedSceneHasOneVisibleTempleAtCarmelOffsetAndNoOldTempleTile()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Illuria.unity", OpenSceneMode.Additive);
        try
        {
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var carmel = transforms.Single(item => item.name == "Carmel");
            var temple = transforms.Single(item => item.name == "TempleofIris");
            var map = transforms.Where(item => item.name == "WorldTilemap").Select(item => item.GetComponent<Tilemap>()).Single(item => item != null);
            var cell = map.WorldToCell(temple.position);
            Assert.That(cell - map.WorldToCell(carmel.position), Is.EqualTo(new Vector3Int(2, 4, 0)));
            Assert.That(cell, Is.EqualTo(new Vector3Int(22, 89, 0)));
            Assert.That(map.GetTile(cell + Vector3Int.down).GetType().Name, Is.EqualTo("RoadTile"), "The temple sits above, not on, the road.");
            Assert.That(map.GetTile(cell), Is.Not.Null);
            Assert.That(map.GetSprite(cell), Is.Not.Null);
            Assert.That(map.GetTile(cell).GetType().Name, Is.EqualTo("TempleTile"));
            Assert.That(map.GetTile(new Vector3Int(45, 125, 0)).GetType().Name, Is.Not.EqualTo("TempleTile"));
            var marker = temple.GetComponent("LocationEntry");
            Assert.That(marker, Is.Not.Null);
            Assert.That(new SerializedObject(marker).FindProperty("locationShortName").stringValue, Is.EqualTo("TempleofIris"));
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }
}
