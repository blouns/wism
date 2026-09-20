using System.Collections;
using System.Linq;
using Assets.Scripts.Managers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;

public sealed partial class GameSetupPointerMatrixTests
{
    [UnityTest]
    public IEnumerator Minimap_CityMarkersFollowWorldGeometryAndDoNotTakeInput()
    {
        yield return StartRoster("Mini-Illuria", null);
        var overlay = Object.FindAnyObjectByType<MinimapCityOverlay>();
        Assert.That(overlay, Is.Not.Null);
        Assert.That(overlay.raycastTarget, Is.False);
        var cities = World.Current.GetCities();
        Assert.That(overlay.MarkerCount, Is.EqualTo(cities.Count));
        var ownerNames = cities.Select(city => city.Player?.Clan.ShortName).ToArray();
        var armies = Game.Current.Players.SelectMany(player => player.GetArmies())
            .Select(army => (army.Id, army.X, army.Y, army.MovesRemaining)).ToArray();
        var gold = Game.Current.Players.Select(player => player.Gold).ToArray();
        var mesh = new Mesh();
        try
        {
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
            {
                yield return SetViewport(size.x, size.y);
                yield return null;
                overlay.Refresh();
                Canvas.ForceUpdateCanvases();
                Object.Destroy(mesh);
                mesh = Object.Instantiate(overlay.canvasRenderer.GetMesh());
                Assert.That(mesh.vertexCount, Is.EqualTo(cities.Count * 12), "Three quads per live city.");
                var manager = Object.FindAnyObjectByType<UnityManager>();
                var camera = GameObject.FindGameObjectWithTag("MinimapCamera").GetComponent<Camera>();
                for (int i = 0; i < cities.Count; i++)
                {
                    var tiles = cities[i].GetTiles();
                    var center = tiles.Select(tile => manager.WorldTilemap.ConvertGameToUnityVector(tile.X, tile.Y))
                        .Aggregate(Vector3.zero, (sum, point) => sum + point) / tiles.Length;
                    var expected = camera.WorldToViewportPoint(center);
                    var actual = (mesh.vertices[i * 12] + mesh.vertices[i * 12 + 2]) * .5f;
                    var normalized = (Vector2)actual - overlay.rectTransform.rect.min;
                    normalized = new Vector2(normalized.x / overlay.rectTransform.rect.width,
                        normalized.y / overlay.rectTransform.rect.height);
                    Assert.That(normalized.x, Is.EqualTo(expected.x).Within(.005f), cities[i].ShortName);
                    Assert.That(normalized.y, Is.EqualTo(expected.y).Within(.005f), cities[i].ShortName);
                }
                AssertMinimapFitsWorld();
            }
            yield return Click(overlay.rectTransform);
            CollectionAssert.AreEqual(ownerNames, cities.Select(city => city.Player?.Clan.ShortName));
            CollectionAssert.AreEqual(armies, Game.Current.Players.SelectMany(player => player.GetArmies())
                .Select(army => (army.Id, army.X, army.Y, army.MovesRemaining)));
            CollectionAssert.AreEqual(gold, Game.Current.Players.Select(player => player.Gold));
            yield return GameSetupModSettingsFlowTests.CaptureSetupCanvas(overlay.gameObject, "minimap-cities-camera-projection.png");
        }
        finally { Object.Destroy(mesh); }
    }

    [UnityTest]
    public IEnumerator Minimap_CaptureAndRazeRefreshTheExistingMarker()
    {
        yield return StartRoster("Mini-Illuria", null);
        var overlay = Object.FindAnyObjectByType<MinimapCityOverlay>();
        var cities = World.Current.GetCities();
        var player = Game.Current.Players[0];
        var target = cities.First(city => city.Player != player && city.GetTiles().All(tile => !tile.HasArmies()));
        int index = cities.IndexOf(target);
        var mesh = new Mesh();
        try
        {
            player.ClaimCity(target);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Object.Destroy(mesh);
            mesh = Object.Instantiate(overlay.canvasRenderer.GetMesh());
            var expected = MinimapCityOverlay.ResolveColor(player.Clan.Info.PrimaryColor ?? player.Clan.Info.Color);
            Assert.That(mesh.colors32[index * 12 + 8], Is.EqualTo(expected), "Capture must show the new clan next frame.");
            var before = mesh.vertices[index * 12];
            player.RazeCity(target);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Object.Destroy(mesh);
            mesh = Object.Instantiate(overlay.canvasRenderer.GetMesh());
            Assert.That(overlay.MarkerCount, Is.EqualTo(cities.Count), "Keep the razed location on the map.");
            Assert.That(mesh.vertexCount, Is.EqualTo(cities.Count * 12 + 4), "Razed city becomes four outline quads.");
            Assert.That(Vector3.Distance(mesh.vertices[index * 12], before), Is.LessThan(.0001f),
                "Razing must not shift the marker beyond subpixel layout precision.");
            Assert.That(mesh.colors32[index * 12], Is.EqualTo(MinimapCityOverlay.ResolveColor(null)));
            yield return GameSetupModSettingsFlowTests.CaptureSetupCanvas(overlay.gameObject, "minimap-razed-camera-projection.png");
        }
        finally { Object.Destroy(mesh); }
    }

    [TestCase("(0, 0, 0)", 0, 0, 0)]
    [TestCase("(255, 255, 255)", 255, 255, 255)]
    [TestCase("(18, 205, 79)", 18, 205, 79)]
    [TestCase(null, 160, 160, 160)]
    [TestCase("invalid", 160, 160, 160)]
    public void Minimap_UsesModColorNotAnEightClanPalette(string source, int r, int g, int b)
    {
        Assert.That(MinimapCityOverlay.ResolveColor(source), Is.EqualTo(new Color32((byte)r, (byte)g, (byte)b, 255)));
    }
}
