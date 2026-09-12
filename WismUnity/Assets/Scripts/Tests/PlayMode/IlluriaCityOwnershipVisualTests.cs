using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.Tiles;
using Assets.Scripts.UnityGame.Persistance.Entities;
using Assets.Scripts.UnityGame.ModKit;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Wism.Client.Core;
using Wism.Client.Modules;

[TestFixture]
public sealed class IlluriaCityOwnershipVisualTests
{
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (SceneManager.GetActiveScene().name == "Illuria")
        {
            SceneManager.LoadScene("GameSetup", LoadSceneMode.Single);
            yield return null;
        }

        UnityManager.SetNewGameSettings(null);
        Game.Unload();
    }

    private IEnumerator LoadIlluria()
    {
        Game.Unload();
        UnityModKitRuntimeSelection.Clear();
        ModFactory.ModPath = GameManager.DefaultModPath;
        ModFactory.WorldPath = "Illuria";
        ModFactory.ActiveFeaturePackIds = new System.Collections.Generic.List<string>();
        ModFactory.ResetCache();
        UnityManager.SetNewGameSettings(new UnityNewGameEntity
        {
            InteractiveUI = false,
            IsNewGame = true,
            Players = new[]
            {
                new UnityPlayerEntity { ClanName = "Sirians", IsHuman = true },
                new UnityPlayerEntity { ClanName = "LordBane", IsHuman = false }
            },
            RandomSeed = 1990,
            RandomStartLocations = false,
            WorldName = "Illuria"
        });

        SceneManager.LoadScene("Illuria", LoadSceneMode.Single);

        yield return new WaitUntil(() =>
            SceneManager.GetActiveScene().name == "Illuria" &&
            Game.IsInitialized() &&
            World.Current != null &&
            World.Current.Name == "Illuria");
        yield return null;
    }

    [UnityTest]
    public IEnumerator NewIlluriaGame_WithOmittedClan_RepaintsOmittedCapitalNeutral()
    {
        yield return LoadIlluria();
        var omittedCapital = World.Current.GetCities()
            .Single(city => city.ShortName == "Khamar");
        Assert.That(omittedCapital.Clan.ShortName, Is.EqualTo("Neutral"));

        var cityManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<CityManager>();
        var neutralCityTile = GetNeutralCityTile(cityManager);
        var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
            .GetComponent<WorldTilemap>();
        var tilemap = worldTilemap.GetComponent<Tilemap>();

        Assert.That(GetCityFootprintTiles(worldTilemap, tilemap, omittedCapital.Tile.X, omittedCapital.Tile.Y),
            Is.All.SameAs(neutralCityTile));
    }

    [UnityTest]
    public IEnumerator NewIlluriaGame_TempleOfIrisRendersBlessesAndSurvivesSnapshotReload()
    {
        yield return LoadIlluria();
        var unity = GameObject.FindGameObjectWithTag("UnityManager").GetComponent<UnityManager>();
        unity.enabled = false;
        var tile = World.Current.Map[22, 89];
        var temple = tile.Location;
        Assert.That(temple, Is.Not.Null);
        Assert.That(temple.ShortName, Is.EqualTo("TempleofIris"));
        Assert.That(temple.DisplayName, Is.EqualTo("Temple of Iris"));
        Assert.That(temple.Kind, Is.EqualTo("Temple"));
        Assert.That(World.Current.Map[45, 125].Location, Is.Null, "The old placement cannot retain a second temple.");
        var map = unity.WorldTilemap.GetComponent<Tilemap>();
        var cell = map.WorldToCell(unity.WorldTilemap.ConvertGameToUnityVector(tile.X, tile.Y));
        Assert.That(map.GetTile(cell), Is.TypeOf<TempleTile>());
        Assert.That(map.GetSprite(cell), Is.Not.Null);

        var camera = Camera.main;
        var position = camera.transform.position;
        var size = camera.orthographicSize;
        try
        {
            camera.transform.position = new Vector3(21, 87, position.z);
            camera.orthographicSize = 6;
            var root = System.IO.Path.Combine(Application.dataPath, "../Library/WismUiCaptures");
            System.IO.Directory.CreateDirectory(root);
            var bridge = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("WismUnity.Playground.UnityPlaygroundCli")).First(type => type != null);
            var path = (string)bridge.GetMethod("CaptureScreenshot", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { root, "carmel-temple-of-iris" });
            Assert.That(new System.IO.FileInfo(path).Length, Is.GreaterThan(1000));
        }
        finally { camera.transform.position = position; camera.orthographicSize = size; }

        var player = Game.Current.GetCurrentPlayer();
        var hero = player.HireHero(tile);
        var armies = new System.Collections.Generic.List<Wism.Client.MapObjects.Army> { hero };
        var provider = unity.GameManager.ControllerProvider;
        Assert.That(provider.LocationController.SearchTemple(armies, temple, out var blessed), Is.True);
        Assert.That(blessed, Is.EqualTo(1));
        var filename = "temple-regression-" + System.Guid.NewGuid().ToString("N") + ".json";
        var savePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
        try
        {
            PersistanceManager.Save(filename, "Temple regression", unity);
            var snapshot = PersistanceManager.LoadEntities(savePath, unity);
            var reload = new Wism.Client.Commands.Games.LoadGameCommand(provider.GameController, snapshot.WismGameEntity);
            Assert.That(reload.Execute(), Is.EqualTo(Wism.Client.Controllers.ActionState.Succeeded));
        }
        finally { if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath); }
        var restored = World.Current.Map[22, 89].Location;
        Assert.That(restored.ShortName, Is.EqualTo("TempleofIris"));
        Assert.That(restored.Kind, Is.EqualTo("Temple"));
        Assert.That(restored.Searched, Is.True);
        Assert.That(map.GetTile(cell), Is.TypeOf<TempleTile>());
    }

    private static CityTile GetNeutralCityTile(CityManager cityManager)
    {
        var field = typeof(CityManager).GetField("neutralCityTile", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        var tile = field.GetValue(cityManager) as CityTile;
        Assert.That(tile, Is.Not.Null);
        return tile;
    }

    private static TileBase[] GetCityFootprintTiles(WorldTilemap worldTilemap, Tilemap tilemap, int x, int y)
    {
        return new[]
        {
            GetTile(worldTilemap, tilemap, x, y),
            GetTile(worldTilemap, tilemap, x + 1, y),
            GetTile(worldTilemap, tilemap, x, y - 1),
            GetTile(worldTilemap, tilemap, x + 1, y - 1)
        };
    }

    private static TileBase GetTile(WorldTilemap worldTilemap, Tilemap tilemap, int x, int y)
    {
        var worldPosition = worldTilemap.ConvertGameToUnityVector(x, y);
        return tilemap.GetTile(tilemap.WorldToCell(worldPosition));
    }
}
