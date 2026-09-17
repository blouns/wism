using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Tiles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Wism.Client.Core;

public sealed partial class ArmyUiInputTests
{
    [UnityTest]
    public IEnumerator CityFootprints_TestWorldHasNoOrphanCastleCells()
    {
        yield return null;
        var map = unity.WorldTilemap.GetComponent<Tilemap>();
        var expected = new HashSet<Vector3Int>();
        foreach (var city in World.Current.GetCities())
            foreach (var offset in new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.down, new Vector2Int(1, -1) })
                expected.Add(map.WorldToCell(unity.WorldTilemap.ConvertGameToUnityVector(city.X + offset.x, city.Y + offset.y)));

        var orphaned = new List<string>();
        foreach (var cell in map.cellBounds.allPositionsWithin)
        {
            if (!(map.GetTile(cell) is CityTile) || expected.Contains(cell)) continue;
            var logical = unity.WorldTilemap.ConvertUnityToGameVector(map.GetCellCenterWorld(cell));
            orphaned.Add($"{logical.x},{logical.y}:{map.GetTile(cell).name}");
        }
        Capture("city-footprint-audit");
        Assert.That(orphaned, Is.Empty, "Castle art outside logical city footprints: " + string.Join(";", orphaned));
        Assert.That(expected.All(cell => map.GetTile(cell) is CityTile), Is.True,
            "Every logical city needs all four visible quadrants.");
    }
}
