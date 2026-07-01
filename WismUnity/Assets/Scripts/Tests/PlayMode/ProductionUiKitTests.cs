using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

public sealed class ProductionUiKitTests
{
    [Test]
    public void WismUiFactory_CreatesClassicButtonWithMinimumHitTarget()
    {
        var host = new GameObject("host", typeof(RectTransform));

        try
        {
            var panel = WismUiFactory.CreateVerticalPanel(host.transform, "Panel");
            var button = WismUiFactory.CreateButton(panel, "ProdButton", "Prod");
            var layout = button.GetComponent<LayoutElement>();

            Assert.That(button.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(layout.minWidth, Is.GreaterThanOrEqualTo(WismUiTheme.Classic.MinimumHitTarget.x));
            Assert.That(layout.minHeight, Is.GreaterThanOrEqualTo(WismUiTheme.Classic.MinimumHitTarget.y));
            Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo("Prod"));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void ScreenOverlayCanvasHost_UsesWismReferenceResolution()
    {
        var host = new GameObject("canvas-host");

        try
        {
            var canvas = ScreenOverlayCanvasHost.Ensure(host);
            var scaler = canvas.GetComponent<CanvasScaler>();

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution.x, Is.EqualTo(WismUiTheme.Classic.ReferenceResolution.x));
            Assert.That(scaler.referenceResolution.y, Is.EqualTo(WismUiTheme.Classic.ReferenceResolution.y));
            Assert.That(canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void ProductionPanelModes_KeepSingleCityAndManagementSeparate()
    {
        Assert.That(ProductionPanelMode.SingleCity, Is.Not.EqualTo(ProductionPanelMode.Management));
        Assert.That(ProductionModeNames.SingleCityEntry, Is.EqualTo("P-then-city"));
        Assert.That(ProductionModeNames.ManagementEntry, Is.EqualTo("owned-city-management"));
    }

    [Test]
    public void GameKeyboardShortcuts_LaunchesOwnedCitiesManagementWithL()
    {
        Assert.That(GameKeyboardShortcuts.ResolveLKey(shiftHeld: false), Is.EqualTo(GameKeyboardAction.OpenProductionManagement));
        Assert.That(GameKeyboardShortcuts.ResolveLKey(shiftHeld: true), Is.EqualTo(GameKeyboardAction.LoadGame));
    }

    [Test]
    public void ProductionPanelEntryPolicy_PThenCityRequiresOwnedCity()
    {
        var currentClan = CreateClan("Current");
        var enemyClan = CreateClan("Enemy");
        var ownedTile = TileWithCity(CreateCity(1, "Owned"), currentClan);
        var enemyTile = TileWithCity(CreateCity(2, "Enemy"), enemyClan);
        var neutralTile = TileWithCity(CreateCity(3, "Neutral"), null);
        var emptyTile = new Tile { X = 4, Y = 4 };

        Assert.That(ProductionPanelEntryPolicy.TryGetOwnedCity(ownedTile, currentClan, out var selectedCity), Is.True);
        Assert.That(selectedCity, Is.EqualTo(ownedTile.City));
        Assert.That(ProductionPanelEntryPolicy.TryGetOwnedCity(enemyTile, currentClan, out _), Is.False);
        Assert.That(ProductionPanelEntryPolicy.TryGetOwnedCity(neutralTile, currentClan, out _), Is.False);
        Assert.That(ProductionPanelEntryPolicy.TryGetOwnedCity(emptyTile, currentClan, out _), Is.False);
        Assert.That(ProductionPanelEntryPolicy.TryGetOwnedCity(null, currentClan, out _), Is.False);
    }

    [Test]
    public void ProductionPanelEntryPolicy_DestinationUsesOwnedCityGate()
    {
        var currentClan = CreateClan("Current");
        var enemyClan = CreateClan("Enemy");
        var ownedCity = CityWithClan(CreateCity(1, "Owned"), currentClan);
        var enemyCity = CityWithClan(CreateCity(2, "Enemy"), enemyClan);
        var neutralCity = CityWithClan(CreateCity(3, "Neutral"), null);

        Assert.That(ProductionPanelEntryPolicy.IsOwnedCity(ownedCity, currentClan), Is.True);
        Assert.That(ProductionPanelEntryPolicy.IsOwnedCity(enemyCity, currentClan), Is.False);
        Assert.That(ProductionPanelEntryPolicy.IsOwnedCity(neutralCity, currentClan), Is.False);
        Assert.That(ProductionPanelEntryPolicy.IsOwnedCity(null, currentClan), Is.False);
        Assert.That(ProductionPanelEntryPolicy.IsOwnedCity(ownedCity, null), Is.False);
    }

    [Test]
    public void ProductionViewModels_KeepPThenCityAndOwnedCityManagementDataSeparate()
    {
        var garom = CreateCity(1, "Garom");
        var ungor = CreateCity(2, "Ungor");

        var singleCity = ProductionPanelViewModelBuilder.BuildSingleCity(garom);
        var management = ProductionPanelViewModelBuilder.BuildManagement(new[] { garom, ungor }, garom);

        Assert.That(singleCity.Mode, Is.EqualTo(ProductionPanelMode.SingleCity));
        Assert.That(singleCity.Cities.Count, Is.EqualTo(1));
        Assert.That(singleCity.Cities[0].City, Is.EqualTo(garom));
        Assert.That(management.Mode, Is.EqualTo(ProductionPanelMode.Management));
        Assert.That(management.Cities.Count, Is.EqualTo(2));
        Assert.That(management.Cities.Select(city => city.City), Is.EquivalentTo(new[] { garom, ungor }));
    }

    [Test]
    public void ProductionManagementViewModel_IndexesFiveCityRoutedProductionState()
    {
        var army = new ArmyInfo { ShortName = "LightInfantry", DisplayName = "Light Infantry" };
        var garom = CreateCity(1, "Garom", 2, 10);
        var ungor = CreateCity(2, "Ungor", 7, 5);
        var idle = CreateCity(3, "Idle", 12, 4);
        var redirected = CreateCity(4, "Redirected", 3, 15);
        var deliverySource = CreateCity(5, "Delivery", 11, 12);

        garom.Barracks.ArmyInTraining = Training(garom, null, army, turnsToProduce: 2, turnsToDeliver: 0);
        ungor.Barracks.ArmyInTraining = Training(ungor, garom, army, turnsToProduce: 3, turnsToDeliver: 3);
        redirected.Barracks.ArmyInTraining = Training(redirected, ungor, army, turnsToProduce: 4, turnsToDeliver: 3);
        deliverySource.Barracks.ArmiesToDeliver = new Queue<ArmyInTraining>();
        deliverySource.Barracks.ArmiesToDeliver.Enqueue(Training(deliverySource, garom, army, turnsToProduce: 0, turnsToDeliver: 1));

        var model = ProductionPanelViewModelBuilder.BuildManagement(
            new[] { garom, ungor, idle, redirected, deliverySource },
            garom);

        Assert.That(model.Mode, Is.EqualTo(ProductionPanelMode.Management));
        Assert.That(model.Cities.Count, Is.EqualTo(5));
        Assert.That(model.SelectedCity.City, Is.EqualTo(garom));
        Assert.That(model.SelectedCity.IsIdle, Is.False);
        Assert.That(model.SelectedCity.IncomingSources.Count, Is.EqualTo(2));
        Assert.That(model.SelectedCity.IncomingSources[0].SourceCity, Is.EqualTo(ungor));
        Assert.That(model.SelectedCity.IncomingSources[1].SourceCity, Is.EqualTo(deliverySource));
        Assert.That(model.Cities[2].IsIdle, Is.True);
        Assert.That(model.MinimapMarkers, Has.Some.Matches<ProductionMinimapMarkerViewModel>(marker =>
            marker.City == garom && marker.Kind == "selected"));
        Assert.That(model.MinimapMarkers, Has.Some.Matches<ProductionMinimapMarkerViewModel>(marker =>
            marker.City == idle && marker.Kind == "idle"));
        Assert.That(model.MinimapMarkers, Has.Some.Matches<ProductionMinimapMarkerViewModel>(marker =>
            marker.City == garom && marker.Kind == "receiver"));
    }

    [Test]
    public void ProductionManagementUi_RendersFiveCityMinimapMarkers()
    {
        var host = new GameObject("production-proof-host", typeof(RectTransform));

        try
        {
            var army = new ArmyInfo { ShortName = "LightInfantry", DisplayName = "Light Infantry" };
            var garom = CreateCity(1, "Garom", 2, 10);
            var ungor = CreateCity(2, "Ungor", 7, 5);
            var idle = CreateCity(3, "Idle", 12, 4);
            var redirected = CreateCity(4, "Redirected", 3, 15);
            var deliverySource = CreateCity(5, "Delivery", 11, 12);

            garom.Barracks.ArmyInTraining = Training(garom, null, army, turnsToProduce: 2, turnsToDeliver: 0);
            ungor.Barracks.ArmyInTraining = Training(ungor, garom, army, turnsToProduce: 3, turnsToDeliver: 3);
            redirected.Barracks.ArmyInTraining = Training(redirected, ungor, army, turnsToProduce: 4, turnsToDeliver: 3);
            deliverySource.Barracks.ArmiesToDeliver = new Queue<ArmyInTraining>();
            deliverySource.Barracks.ArmiesToDeliver.Enqueue(Training(deliverySource, garom, army, turnsToProduce: 0, turnsToDeliver: 1));

            var model = ProductionPanelViewModelBuilder.BuildManagement(
                new[] { garom, ungor, idle, redirected, deliverySource },
                garom);
            var minimap = ProductionManagementUi.CreateMinimapPanel(host.transform, "ProductionMinimapOverlay");
            var markers = ProductionManagementUi.RebuildMinimapMarkers(minimap, model.MinimapMarkers);

            Assert.That(markers.Count, Is.EqualTo(model.MinimapMarkers.Count));
            Assert.That(markers.Select(marker => marker.name), Has.Some.Contains("selected_Garom"));
            Assert.That(markers.Select(marker => marker.name), Has.Some.Contains("idle_Idle"));
            Assert.That(markers.Select(marker => marker.name), Has.Some.Contains("receiver_Garom"));

            var selectedMarker = markers.Single(marker => marker.name.Contains("selected_Garom"));
            Assert.That(selectedMarker.GetComponent<Image>().color, Is.EqualTo(ProductionManagementUi.MarkerColor("selected")));
            var selectedPosition = selectedMarker.GetComponent<RectTransform>().anchoredPosition;
            var expectedPosition = ProductionManagementUi.MarkerPosition(
                model.MinimapMarkers.Single(marker => marker.Kind == "selected" && marker.City == garom),
                model.MinimapMarkers);
            Assert.That(selectedPosition.x, Is.EqualTo(expectedPosition.x).Within(0.01f));
            Assert.That(selectedPosition.y, Is.EqualTo(expectedPosition.y).Within(0.01f));

            foreach (var marker in markers)
            {
                var rect = marker.GetComponent<RectTransform>();
                Assert.That(rect.sizeDelta.x, Is.EqualTo(ProductionManagementUi.MarkerSize));
                Assert.That(rect.sizeDelta.y, Is.EqualTo(ProductionManagementUi.MarkerSize));
                Assert.That(rect.anchoredPosition.x, Is.InRange(0f, ProductionManagementUi.MinimapWidth));
                Assert.That(rect.anchoredPosition.y, Is.InRange(0f, ProductionManagementUi.MinimapHeight));
                Assert.That(marker.GetComponent<Image>().raycastTarget, Is.False);
            }
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void ProductionManagementViewModel_ExposesDestinationJumpForRedirectedCity()
    {
        var army = new ArmyInfo { ShortName = "LightInfantry", DisplayName = "Light Infantry" };
        var garom = CreateCity(1, "Garom");
        var ungor = CreateCity(2, "Ungor");
        ungor.Barracks.ArmyInTraining = Training(ungor, garom, army, turnsToProduce: 3, turnsToDeliver: 3);

        var model = ProductionPanelViewModelBuilder.BuildManagement(new[] { garom, ungor }, ungor);

        Assert.That(model.SelectedCity.City, Is.EqualTo(ungor));
        Assert.That(model.SelectedCity.CurrentDestinationCity, Is.EqualTo(garom));
        Assert.That(model.SelectedCity.DestinationCityName, Is.EqualTo("Garom"));
        Assert.That(model.MinimapMarkers, Has.Some.Matches<ProductionMinimapMarkerViewModel>(marker =>
            marker.City == garom && marker.Kind == "redirect-target"));
    }

    [Test]
    public void ProductionSingleCityViewModel_DoesNotExposeManagementCityList()
    {
        var garom = CreateCity(1, "Garom");

        var model = ProductionPanelViewModelBuilder.BuildSingleCity(garom);

        Assert.That(model.Mode, Is.EqualTo(ProductionPanelMode.SingleCity));
        Assert.That(model.Cities.Count, Is.EqualTo(1));
        Assert.That(model.SelectedCity.City, Is.EqualTo(garom));
    }

    private static City CreateCity(int id, string name)
    {
        return CreateCity(id, name, 0, 0);
    }

    private static City CreateCity(int id, string name, int x, int y)
    {
        var city = City.Create(new CityInfo
        {
            ShortName = name,
            DisplayName = name,
            Defense = 5,
            Income = 10,
            ProductionInfos = new[]
            {
                new ProductionInfo
                {
                    ArmyInfoName = "LightInfantry",
                    TurnsToProduce = 2,
                    Upkeep = 3,
                    Moves = 12,
                    Strength = 3
                }
            }
        });
        city.Id = id;
        city.DisplayName = name;
        city.Tile = new Tile { X = x, Y = y };
        return city;
    }

    private static Clan CreateClan(string shortName)
    {
        return Clan.Create(new ClanInfo
        {
            ShortName = shortName,
            DisplayName = shortName,
            StartingGold = 1000
        });
    }

    private static Tile TileWithCity(City city, Clan clan)
    {
        CityWithClan(city, clan);
        var tile = new Tile { X = city.Tile.X, Y = city.Tile.Y, City = city };
        city.Tile = tile;
        return tile;
    }

    private static City CityWithClan(City city, Clan clan)
    {
        typeof(City).GetProperty(nameof(City.Clan), BindingFlags.Instance | BindingFlags.Public)
            .GetSetMethod(true)
            .Invoke(city, new object[] { clan });
        return city;
    }
    private static ArmyInTraining Training(City source, City destination, ArmyInfo army, int turnsToProduce, int turnsToDeliver)
    {
        return new ArmyInTraining
        {
            ProductionCity = source,
            DestinationCity = destination,
            ArmyInfo = army,
            TurnsToProduce = turnsToProduce,
            TurnsToDeliver = turnsToDeliver,
            Upkeep = 3,
            DisplayName = army.DisplayName
        };
    }
}

public static class ProductionModeNames
{
    public const string SingleCityEntry = "P-then-city";
    public const string ManagementEntry = "owned-city-management";
}
