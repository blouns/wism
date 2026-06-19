using Assets.Scripts.Editors;
using Assets.Scripts.Tilemaps;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class CityEntryCoordinateTests
{
    [UnityTest]
    public IEnumerator CityEntry_ReturnsTopLeftCoordinateForCenteredCityMarker()
    {
        var grid = new GameObject("SyntheticGrid");
        var worldTilemapObject = new GameObject("SyntheticWorldTilemap");
        var marker = new GameObject("SyntheticCityMarker");

        try
        {
            grid.AddComponent<Grid>();
            worldTilemapObject.transform.SetParent(grid.transform, false);
            worldTilemapObject.tag = "WorldTilemap";

            var tilemap = worldTilemapObject.AddComponent<Tilemap>();
            worldTilemapObject.AddComponent<TilemapRenderer>();
            worldTilemapObject.AddComponent<WorldTilemap>();

            var topLeft = new Vector3Int(5, 4, 0);
            var boundsTile = ScriptableObject.CreateInstance<Tile>();
            tilemap.SetTile(Vector3Int.zero, boundsTile);
            tilemap.SetTile(topLeft, boundsTile);
            tilemap.SetTile(new Vector3Int(topLeft.x + 1, topLeft.y, 0), boundsTile);
            tilemap.SetTile(new Vector3Int(topLeft.x, topLeft.y - 1, 0), boundsTile);
            tilemap.SetTile(new Vector3Int(topLeft.x + 1, topLeft.y - 1, 0), boundsTile);

            marker.transform.position = tilemap.CellToWorld(topLeft) + new Vector3(tilemap.cellSize.x, 0f, 0f);
            var cityEntry = marker.AddComponent<CityEntry>();

            yield return null;

            var coords = cityEntry.GetGameCoordinates();
            Assert.AreEqual(topLeft.x, coords.x);
            Assert.AreEqual(topLeft.y, coords.y);
        }
        finally
        {
            Object.Destroy(marker);
            Object.Destroy(worldTilemapObject);
            Object.Destroy(grid);
        }
    }
}
