using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.Tiles;
using Assets.Scripts.UnityGame.Persistance.Entities;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Wism.Client.Core;

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
    }

    [UnityTest]
    public IEnumerator NewIlluriaGame_WithOmittedClan_RepaintsOmittedCapitalNeutral()
    {
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
