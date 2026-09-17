using Assets.Scripts;
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
    public IEnumerator CityEntry_SnapsAuthoringOffsetsWithoutChangingPointerCells()
    {
        var grid = new GameObject("OffsetCityGrid", typeof(Grid));
        var mapObject = new GameObject("OffsetCityMap", typeof(Tilemap), typeof(TilemapRenderer));
        var marker = new GameObject("OffsetCityMarker");
        var boundsTile = ScriptableObject.CreateInstance<Tile>();
        try
        {
            grid.transform.position = new Vector3(12, -9, 0);
            grid.transform.localScale = new Vector3(2, 2, 1);
            mapObject.transform.SetParent(grid.transform, false);
            mapObject.tag = "WorldTilemap";
            var map = mapObject.GetComponent<Tilemap>();
            var world = mapObject.AddComponent<WorldTilemap>();
            map.SetTile(new Vector3Int(-3, 4, 0), boundsTile);
            map.SetTile(new Vector3Int(9, 12, 0), boundsTile);
            map.CompressBounds();
            var entry = marker.AddComponent<CityEntry>();
            yield return null;
            foreach (var offset in new[] { -0.04f, 0f, 0.04f })
            {
                marker.transform.position = map.transform.TransformPoint(
                    map.CellToLocalInterpolated(new Vector3(3 + offset, 8 + offset, 0)));
                Assert.That(entry.GetGameCoordinates(), Is.EqualTo(new Vector2Int(5, 4)));
            }
            var insideCell = map.transform.TransformPoint(map.CellToLocalInterpolated(new Vector3(2.99f, 7.99f, 0)));
            Assert.That(world.ConvertUnityToGameVector(insideCell), Is.EqualTo(new Vector2Int(5, 3)),
                "Pointer conversion must still use the containing cell, not marker snapping.");
        }
        finally
        {
            Object.Destroy(marker);
            Object.Destroy(mapObject);
            Object.Destroy(grid);
            Object.Destroy(boundsTile);
        }
    }

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

    [UnityTest]
    public IEnumerator LocationEntry_ReturnsTileCoordinateForCenteredLocationMarker()
    {
        var grid = new GameObject("SyntheticGrid");
        var worldTilemapObject = new GameObject("SyntheticWorldTilemap");
        var marker = new GameObject("SyntheticLocationMarker");

        try
        {
            grid.AddComponent<Grid>();
            worldTilemapObject.transform.SetParent(grid.transform, false);
            worldTilemapObject.tag = "WorldTilemap";

            var tilemap = worldTilemapObject.AddComponent<Tilemap>();
            worldTilemapObject.AddComponent<TilemapRenderer>();
            worldTilemapObject.AddComponent<WorldTilemap>();

            var cell = new Vector3Int(7, 9, 0);
            var boundsTile = ScriptableObject.CreateInstance<Tile>();
            tilemap.SetTile(Vector3Int.zero, boundsTile);
            tilemap.SetTile(cell, boundsTile);

            marker.transform.position = tilemap.GetCellCenterWorld(cell);
            var locationEntry = marker.AddComponent<LocationEntry>();

            yield return null;

            var coords = locationEntry.GetGameCoordinates();
            Assert.AreEqual(cell.x, coords.x);
            Assert.AreEqual(cell.y, coords.y);
        }
        finally
        {
            Object.Destroy(marker);
            Object.Destroy(worldTilemapObject);
            Object.Destroy(grid);
        }
    }

    [UnityTest]
    public IEnumerator WorldTilemap_RoundTripsTileCentersForSelectedReticlePlacement()
    {
        var grid = new GameObject("SyntheticGrid");
        var worldTilemapObject = new GameObject("SyntheticWorldTilemap");

        try
        {
            grid.AddComponent<Grid>();
            worldTilemapObject.transform.SetParent(grid.transform, false);

            var tilemap = worldTilemapObject.AddComponent<Tilemap>();
            worldTilemapObject.AddComponent<TilemapRenderer>();
            var worldTilemap = worldTilemapObject.AddComponent<WorldTilemap>();

            var boundsTile = ScriptableObject.CreateInstance<Tile>();
            tilemap.SetTile(Vector3Int.zero, boundsTile);
            tilemap.SetTile(new Vector3Int(12, 11, 0), boundsTile);

            yield return null;

            var worldPosition = worldTilemap.ConvertGameToUnityVector(12, 11);
            var coords = worldTilemap.ConvertUnityToGameVector(worldPosition);

            Assert.AreEqual(12, coords.x);
            Assert.AreEqual(11, coords.y);
            Assert.AreEqual(tilemap.GetCellCenterWorld(new Vector3Int(12, 11, 0)).x, worldPosition.x, 0.001f);
            Assert.AreEqual(tilemap.GetCellCenterWorld(new Vector3Int(12, 11, 0)).y, worldPosition.y, 0.001f);
        }
        finally
        {
            Object.Destroy(worldTilemapObject);
            Object.Destroy(grid);
        }
    }

    [UnityTest]
    public IEnumerator WorldTilemap_ConvertsWholeVisibleTileToSameGameCoordinate()
    {
        var grid = new GameObject("SyntheticGrid");
        var worldTilemapObject = new GameObject("SyntheticWorldTilemap");

        try
        {
            grid.AddComponent<Grid>();
            worldTilemapObject.transform.SetParent(grid.transform, false);

            var tilemap = worldTilemapObject.AddComponent<Tilemap>();
            worldTilemapObject.AddComponent<TilemapRenderer>();
            var worldTilemap = worldTilemapObject.AddComponent<WorldTilemap>();

            var boundsTile = ScriptableObject.CreateInstance<Tile>();
            tilemap.SetTile(Vector3Int.zero, boundsTile);
            tilemap.SetTile(new Vector3Int(12, 11, 0), boundsTile);

            yield return null;

            var center = worldTilemap.ConvertGameToUnityVector(12, 11);
            var xInset = tilemap.cellSize.x * 0.49f;
            var yInset = tilemap.cellSize.y * 0.49f;

            AssertGameCoords(worldTilemap, center, 12, 11);
            AssertGameCoords(worldTilemap, center + new Vector3(-xInset, 0f, 0f), 12, 11);
            AssertGameCoords(worldTilemap, center + new Vector3(xInset, 0f, 0f), 12, 11);
            AssertGameCoords(worldTilemap, center + new Vector3(0f, -yInset, 0f), 12, 11);
            AssertGameCoords(worldTilemap, center + new Vector3(0f, yInset, 0f), 12, 11);
        }
        finally
        {
            Object.Destroy(worldTilemapObject);
            Object.Destroy(grid);
        }
    }

    [UnityTest]
    public IEnumerator WorldTilemap_NormalizesClickCoordinatesFromOffsetTilemapBounds()
    {
        var grid = new GameObject("SyntheticGrid");
        var worldTilemapObject = new GameObject("SyntheticWorldTilemap");

        try
        {
            grid.AddComponent<Grid>();
            worldTilemapObject.transform.SetParent(grid.transform, false);

            var tilemap = worldTilemapObject.AddComponent<Tilemap>();
            worldTilemapObject.AddComponent<TilemapRenderer>();
            var worldTilemap = worldTilemapObject.AddComponent<WorldTilemap>();

            var boundsTile = ScriptableObject.CreateInstance<Tile>();
            tilemap.SetTile(new Vector3Int(-3, 4, 0), boundsTile);
            tilemap.SetTile(new Vector3Int(2, 9, 0), boundsTile);
            tilemap.CompressBounds();

            yield return null;

            AssertGameCoords(worldTilemap, tilemap.GetCellCenterWorld(new Vector3Int(-3, 4, 0)), 0, 0);
            AssertGameCoords(worldTilemap, tilemap.GetCellCenterWorld(new Vector3Int(2, 9, 0)), 5, 5);
        }
        finally
        {
            Object.Destroy(worldTilemapObject);
            Object.Destroy(grid);
        }
    }

    [Test]
    public void UnityDefaultGameSettings_LeaveSeedUnspecified()
    {
        var settings = UnityGameFactory.CreateDefaultGameSettings();

        Assert.AreEqual(0, settings.RandomSeed);
    }

    private static void AssertGameCoords(WorldTilemap worldTilemap, Vector3 worldPosition, int expectedX, int expectedY)
    {
        var coords = worldTilemap.ConvertUnityToGameVector(worldPosition);

        Assert.AreEqual(expectedX, coords.x);
        Assert.AreEqual(expectedY, coords.y);
    }
}
