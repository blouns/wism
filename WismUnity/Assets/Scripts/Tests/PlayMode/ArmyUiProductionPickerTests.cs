using System.Collections;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;

public sealed partial class ArmyUiInputTests
{
    [UnityTest]
    public IEnumerator ProductionLoc_MinimapMouseAndTouchRouteWithoutMovingArmy()
    {
        var player = Game.Current.GetCurrentPlayer();
        var source = player.Capitol;
        var destination = Game.Current.Players.First(other => other != player).Capitol;
        player.ClaimCity(destination);
        int width = Screen.width, height = Screen.height;
        var armyTile = hero.Tile;
        var armyMoves = hero.MovesRemaining;
        try
        {
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(3440, 1440) })
            {
                ApplyViewport(size.x, size.y);
                yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y);
                OpenProductionPicker(true);
                var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
                ClickProductionButton(picker, "ArmyButton1");
                yield return null;
                Canvas.ForceUpdateCanvases();
                var loc = (RectTransform)picker.GetComponentsInChildren<Button>().Single(b => b.name == "LocButton").transform;
                yield return Click(RectTransformUtility.WorldToScreenPoint(null, loc.TransformPoint(loc.rect.center)));
                Assert.That(picker.IsSelectingDestination(), Is.True, "Loc must open after layout settles at " + size);
                var overlay = GameObject.Find("Minimap").GetComponentInChildren<MinimapCityOverlay>();
                Assert.That(overlay.ColorOverride(source), Is.EqualTo((Color32)Color.yellow));
                Assert.That(overlay.ColorOverride(destination), Is.EqualTo((Color32)Color.white));
                var point = ProductionMinimapPoint(destination);
                Assert.That(overlay.HitTestCity(point, ProductionMinimapEventCamera()), Is.SameAs(destination));
                yield return CaptureProductionPicker(picker, _ => { }, "-loc");
                if (size.x <= 1280) yield return Click(point); else yield return Tap(point);
                yield return WaitFor(() => source.Barracks.ArmyInTraining?.DestinationCity == destination,
                    () => $"routing: mode={input.InputMode}, picking={picker.IsSelectingDestination()}, point={point}");
                Assert.That(picker.IsSelectingDestination(), Is.False);
                Assert.That(input.InputMode, Is.EqualTo(InputMode.UI));
                Assert.That(overlay.ColorOverride, Is.Null);
                yield return null;
                Assert.That(picker.transform.Find("ClassicProductionPicker/CurrentArmyKind").gameObject.activeSelf, Is.True);
                Assert.That(picker.transform.Find("ClassicProductionPicker/TurnsRemainingText").GetComponent<Text>().text,
                    Is.EqualTo(source.Barracks.ArmyInTraining.TurnsToProduce + "t"));
                Assert.That(hero.Tile, Is.SameAs(armyTile));
                Assert.That(hero.MovesRemaining, Is.EqualTo(armyMoves));
                picker.OnExitClick();
                source.Barracks.StopProduction();
            }
        }
        finally { ApplyViewport(width, height); }
    }

    [UnityTest]
    public IEnumerator ProductionLoc_CancelAndRejectedTargetDoNotQueueOrders()
    {
        var player = Game.Current.GetCurrentPlayer();
        var source = player.Capitol;
        var owned = Game.Current.Players.First(other => other != player).Capitol;
        var enemy = World.Current.GetCities().First(city => city != source && city != owned);
        player.ClaimCity(owned);
        OpenProductionPicker(true);
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        ClickProductionButton(picker, "ArmyButton1");
        ClickProductionButton(picker, "LocButton");
        var before = TerminalState();
        var count = manager.ControllerProvider.CommandController.GetCommands().Count();
        yield return Click(ProductionMinimapPoint(enemy));
        yield return PressJourneyKey(UnityEngine.InputSystem.Key.E);
        yield return PressJourneyKey(UnityEngine.InputSystem.Key.N);
        yield return PressJourneyKey(UnityEngine.InputSystem.Key.Q);
        Assert.That(picker.IsSelectingDestination(), Is.True);
        Assert.That(picker.DestinationColor(enemy), Is.EqualTo(new Color32(128, 128, 128, 255)));
        yield return PressJourneyKey(UnityEngine.InputSystem.Key.Escape);
        Assert.That(picker.IsSelectingDestination(), Is.False);
        Assert.That(input.InputMode, Is.EqualTo(InputMode.UI));
        Assert.That(GameObject.Find("Minimap").GetComponentInChildren<MinimapCityOverlay>().ColorOverride, Is.Null);
        Assert.That(TerminalState(), Is.EqualTo(before));
        Assert.That(manager.ControllerProvider.CommandController.GetCommands().Count(), Is.EqualTo(count));
        ClickProductionButton(picker, "LocButton");
        ClickProductionButton(picker, "LocButton");
        Assert.That(picker.IsSelectingDestination(), Is.False);
        picker.OnExitClick();
    }

    [UnityTest]
    public IEnumerator ProductionLoc_RevalidatesCapturedDestinationAndSourceSelectionMeansLocal()
    {
        var player = Game.Current.GetCurrentPlayer();
        var source = player.Capitol;
        var enemy = Game.Current.Players.First(other => other != player);
        var destination = enemy.Capitol;
        player.ClaimCity(destination);
        OpenProductionPicker(true);
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        ClickProductionButton(picker, "ArmyButton1");
        ClickProductionButton(picker, "LocButton");
        enemy.ClaimCity(destination);
        var gold = player.Gold;
        yield return Click(ProductionMinimapPoint(destination));
        Assert.That(picker.IsSelectingDestination(), Is.True);
        Assert.That(source.Barracks.ProducingArmy(), Is.False);
        Assert.That(player.Gold, Is.EqualTo(gold));
        yield return Tap(ProductionMinimapPoint(source));
        yield return WaitFor(() => source.Barracks.ProducingArmy());
        Assert.That(source.Barracks.ArmyInTraining.DestinationCity, Is.Null);
        picker.OnExitClick();
    }

    [UnityTest]
    public IEnumerator ProductionLoc_CapacityRefreshesAndRejectsFifthSource()
    {
        var player = Game.Current.GetCurrentPlayer();
        var source = player.Capitol;
        var destination = Game.Current.Players.First(other => other != player).Capitol;
        player.ClaimCity(destination);
        var sources = Enumerable.Range(0, 4).Select(index => CreateUiRoutingCity("Route" + index, "LightInfantry")).ToArray();
        foreach (var city in sources)
            city.Barracks.ArmyInTraining = new Wism.Client.Core.Armies.ArmyInTraining
            {
                ProductionCity = city, DestinationCity = destination,
                ArmyInfo = Wism.Client.Modules.ModFactory.FindArmyInfo("LightInfantry"), TurnsToProduce = 2
            };
        OpenProductionPicker(false);
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        ClickProductionButton(picker, "ArmyButton1");
        ClickProductionButton(picker, "LocButton");
        Assert.That(picker.DestinationColor(destination), Is.EqualTo((Color32)Color.red));
        var count = manager.ControllerProvider.CommandController.GetCommands().Count();
        yield return Click(ProductionMinimapPoint(destination));
        Assert.That(picker.IsSelectingDestination(), Is.True);
        Assert.That(manager.ControllerProvider.CommandController.GetCommands().Count(), Is.EqualTo(count));
        sources[0].Barracks.StopProduction();
        Assert.That(picker.DestinationColor(destination), Is.EqualTo((Color32)Color.white));
        yield return Tap(ProductionMinimapPoint(destination));
        yield return WaitFor(() => source.Barracks.ProducingArmy());
        Assert.That(source.Barracks.ArmyInTraining.DestinationCity, Is.SameAs(destination));
        picker.OnExitClick();
    }

    [UnityTest]
    public IEnumerator ProductionLoc_NavyDestinationControlIsDisabled()
    {
        var city = CreateUiRoutingCity("Port", "Navy");
        typeof(UnityManager).GetMethod("ShowProductionPanel", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).Invoke(unity, new object[] { city });
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        ClickProductionButton(picker, "ArmyButton1");
        Assert.That(picker.GetComponentsInChildren<Button>().Single(button => button.name == "LocButton").interactable, Is.False);
        var count = manager.ControllerProvider.CommandController.GetCommands().Count();
        picker.OnLocClick();
        yield return null;
        Assert.That(picker.IsSelectingDestination(), Is.False);
        Assert.That(input.InputMode, Is.EqualTo(InputMode.UI));
        Assert.That(manager.ControllerProvider.CommandController.GetCommands().Count(), Is.EqualTo(count));
        picker.OnExitClick();
    }

    private static Wism.Client.MapObjects.City CreateUiRoutingCity(string name, string army)
    {
        var map = World.Current.Map;
        var tile = map.Cast<Tile>().First(candidate => candidate.X > 0 && candidate.Y > 0 &&
            candidate.X < map.GetLength(0) - 1 &&
            new[] { candidate, map[candidate.X + 1, candidate.Y], map[candidate.X, candidate.Y - 1],
                map[candidate.X + 1, candidate.Y - 1] }.All(cell => !cell.HasCity() && !cell.HasArmies()));
        var city = Wism.Client.MapObjects.City.Create(new Wism.Client.Modules.Infos.CityInfo
        {
            ShortName = name, DisplayName = name, Defense = 4, Income = 20,
            ProductionInfos = new[] { new Wism.Client.Modules.Infos.ProductionInfo
                { ArmyInfoName = army, TurnsToProduce = 1, Upkeep = 4, Moves = 10, Strength = 3 } }
        });
        World.Current.AddCity(city, tile);
        Game.Current.GetCurrentPlayer().ClaimCity(city);
        return city;
    }

    private static Vector2 ProductionMinimapPoint(Wism.Client.MapObjects.City city)
    {
        var rect = GameObject.Find("Minimap").GetComponent<RectTransform>();
        var tilemap = Object.FindObjectOfType<Assets.Scripts.Tilemaps.WorldTilemap>();
        var camera = GameObject.FindGameObjectWithTag("MinimapCamera").GetComponent<Camera>();
        var center = (tilemap.ConvertGameToUnityVector(city.X, city.Y) +
            tilemap.ConvertGameToUnityVector(city.X + 1, city.Y - 1)) * .5f;
        var viewport = camera.WorldToViewportPoint(center);
        return RectTransformUtility.WorldToScreenPoint(ProductionMinimapEventCamera(), rect.TransformPoint(rect.rect.min +
            Vector2.Scale(rect.rect.size, new Vector2(viewport.x, viewport.y))));
    }

    private static Camera ProductionMinimapEventCamera()
    {
        var canvas = GameObject.Find("Minimap").GetComponentInParent<Canvas>().rootCanvas;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    [UnityTest]
    public IEnumerator LiveRegression_DefendAdvancesOnceAndEmptySelectionDoesNothing()
    {
        var army = Game.Current.GetCurrentPlayer().ConscriptArmy(
            Wism.Client.Modules.Infos.ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[2, 2]);
        yield return Tap(ScreenPoint(hero.Tile));
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
            new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.D));
        yield return null;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        yield return WaitFor(() => hero.IsDefending && Game.Current.GetSelectedArmies() != null && Game.Current.GetSelectedArmies().Contains(army));
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(Game.Current.GetSelectedArmies().Single(), Is.SameAs(army));
        manager.DeselectArmies();
        yield return WaitFor(() => !Game.Current.ArmiesSelected());
        var before = State();
        manager.DefendSelectedArmies();
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(State(), Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator LiveRegression_EndTurnAcceptsPlainE()
    {
        yield return EndTurnWithKeys(UnityEngine.InputSystem.Key.E);
    }

    [UnityTest]
    public IEnumerator LiveRegression_EndTurnAcceptsAltE()
    {
        yield return EndTurnWithKeys(UnityEngine.InputSystem.Key.LeftAlt, UnityEngine.InputSystem.Key.E);
    }

    private IEnumerator EndTurnWithKeys(params UnityEngine.InputSystem.Key[] keys)
    {
        var player = Game.Current.GetCurrentPlayer();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
            new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));
        yield return null;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        yield return WaitFor(() => Game.Current.GetCurrentPlayer() != player);
    }

    [UnityTest]
    public IEnumerator LiveRegression_EndTurnImmediatelyLocksMapAndRejectsDuplicateRequests()
    {
        yield return Tap(ScreenPoint(hero.Tile));
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        var commands = manager.ControllerProvider.CommandController;
        var count = commands.GetCommands().Count();
        var player = Game.Current.GetCurrentPlayer();
        unity.enabled = false;
        try
        {
            manager.EndTurn();
            manager.EndTurn();
            Assert.That(input.EndTurnPending, Is.True);
            Assert.That(input.CanAcceptGameplayInput, Is.False);
            Assert.That(input.InputMode, Is.EqualTo(InputMode.AITurn));
            Assert.That(commands.GetCommands().Count(), Is.EqualTo(count + 1));
            var before = State();
            yield return Click(ScreenPoint(World.Current.Map[hero.X + 1, hero.Y]));
            yield return Tap(ScreenPoint(hero.Tile));
            Assert.That(commands.GetCommands().Count(), Is.EqualTo(count + 1), "No mouse or touch action may queue after End Turn.");
            Assert.That(State(), Is.EqualTo(before), "Pending handoff cannot mutate gameplay through pointer input.");
        }
        finally { unity.enabled = true; }
        yield return WaitFor(() => Game.Current.GetCurrentPlayer() != player,
            () => $"handoff: mode={input.InputMode}, pending={input.EndTurnPending}, state={Game.Current.GameState}");
        Assert.That(input.EndTurnPending, Is.False);
        yield return Assets.Scripts.Tests.PlayMode.Common.WismTestAction.WaitForNewHeroOffer();
        yield return Assets.Scripts.Tests.PlayMode.Common.WismTestAction.AcceptNewHeroOffer();
        yield return WaitFor(() => input.CanAcceptGameplayInput && input.InputMode == InputMode.Game,
            () => $"restore: mode={input.InputMode}, pending={input.EndTurnPending}, state={Game.Current.GameState}");
        var nextPlayer = Game.Current.GetCurrentPlayer();
        yield return WaitFor(() => nextPlayer.GetArmies().Count > 0,
            () => "The accepted next-player hero offer must execute before pointer selection.");
        var nextArmy = nextPlayer.GetArmies().First();
        yield return Click(ScreenPoint(nextArmy.Tile));
        yield return WaitFor(() => Game.Current.ArmiesSelected(),
            () => $"next selection: action={input.LastPrimaryAction}, mode={input.InputMode}, armies={nextPlayer.GetArmies().Count}, point={ScreenPoint(nextArmy.Tile)}");
        Assert.That(Game.Current.GetSelectedArmies().All(army => army.Player == nextPlayer), Is.True);
    }

    [UnityTest]
    public IEnumerator LiveRegression_RenewProductionSpansWindowAndKeepsAnswerSemantics()
    {
        var asset = (GameObject)EditorType("UnityEditor.AssetDatabase")
            .GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(System.Type) })
            .Invoke(null, new object[] { "Assets/Prefab/UI/Panels/RenewProductionPanel.prefab", typeof(GameObject) });
        var existing = UnityUtilities.GameObjectHardFind("RenewProductionPanel");
        var instance = Object.Instantiate(asset, existing.transform.parent, false);
        var box = instance.GetComponent<YesNoBox>();
        int width = Screen.width, height = Screen.height;
        bool interactive = unity.InteractiveUI;
        unity.InteractiveUI = true;
        yield return null;
        var before = State();
        try
        {
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(3440, 1440) })
            {
                ApplyViewport(size.x, size.y);
                yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y);
                box.AskFullWidth("Marthos 1st Light Infantry - Produced!");
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4];
                ((RectTransform)instance.transform).GetWorldCorners(corners);
                var canvas = instance.GetComponentInParent<Canvas>();
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Assert.That(RectTransformUtility.WorldToScreenPoint(camera, corners[0]).x, Is.EqualTo(0).Within(1));
                Assert.That(RectTransformUtility.WorldToScreenPoint(camera, corners[2]).x, Is.EqualTo(Screen.width).Within(1));
                Assert.That(State(), Is.EqualTo(before), "Opening a report cannot mutate gameplay.");
                box.Yes(); Assert.That(box.Answer, Is.True);
                box.Clear(); box.No(); Assert.That(box.Answer, Is.False);
                box.Clear(); box.Cancel(); Assert.That(box.Cancelled, Is.True);
                box.Clear(); Assert.That(box.Answer, Is.Null); Assert.That(box.Cancelled, Is.False);
            }
        }
        finally
        {
            unity.InteractiveUI = interactive;
            Object.Destroy(instance);
            ApplyViewport(width, height);
        }
    }

    [UnityTest]
    public IEnumerator ProductionPicker_ClassicControlsRemainVisibleAcrossViewports()
    {
        int width = Screen.width, height = Screen.height;
        var city = Game.Current.GetCurrentPlayer().Capitol;
        var before = State();
        var training = city.Barracks.ArmyInTraining;
        var panelField = typeof(UnityManager).GetField("productionPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var originalPanel = (GameObject)panelField.GetValue(unity);
        var asset = (GameObject)EditorType("UnityEditor.AssetDatabase")
            .GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(System.Type) })
            .Invoke(null, new object[] { "Assets/Prefab/UI/Panels/CityProductionPanel.prefab", typeof(GameObject) });
        Assert.That(asset, Is.Not.Null, "Exercise the shipped picker, not only the test-scene copy.");
        var shippedPanel = Object.Instantiate(asset, originalPanel.transform.parent, false);
        panelField.SetValue(unity, shippedPanel);
        OpenProductionPicker(false);
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        try
        {
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(3440, 1440) })
            {
                ApplyViewport(size.x, size.y);
                yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y);
                yield return null;
                Canvas.ForceUpdateCanvases();
                string capturePath = null;
                yield return CaptureProductionPicker(picker, path => capturePath = path);
                Assert.That(picker.GetPanelMode(), Is.EqualTo(ProductionPanelMode.SingleCity));
                var summary = picker.transform.Find("WismProductionPanelSummary");
                Assert.That(summary == null || !summary.gameObject.activeInHierarchy, Is.True,
                    "P-then-city must not display the management summary over its army choices.");
                foreach (var button in picker.GetComponentsInChildren<Button>())
                {
                    var hit = button.GetComponent<WismHitArea>();
                    Assert.That(hit, Is.Not.Null, button.name);
                    var bounds = hit.GetVisualScreenBounds();
                    Assert.That(bounds.width, Is.GreaterThan(0), button.name);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(-1), button.name);
                    Assert.That(bounds.xMax, Is.LessThanOrEqualTo(Screen.width + 1), button.name);
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(-1), button.name);
                    Assert.That(bounds.yMax, Is.LessThanOrEqualTo(Screen.height + 1), button.name);
                    var hits = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = bounds.center }, hits);
                    Assert.That(hits.Count, Is.GreaterThan(0), button.name);
                    Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button),
                        "A visible picker control must not be covered by another panel: " + button.name);
                }
                var slots = picker.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("ArmyButton")).ToArray();
                Assert.That(slots.Length, Is.EqualTo(city.Barracks.GetProductionKinds().Count));
                foreach (var slot in slots)
                {
                    Assert.That(slot.transform.Find("ArmyKind").GetComponent<Image>().sprite, Is.Not.Null);
                    var text = slot.transform.Find("ArmyInfo").GetComponent<Text>();
                    Assert.That(text.font, Is.Not.Null);
                    Assert.That(text.text, Does.Contain("t /"));
                    AssertProductionTextRendered(text, capturePath);
                }
            }
            Assert.That(State(), Is.EqualTo(before), "Opening and resizing cannot mutate gameplay.");
            Assert.That(city.Barracks.ArmyInTraining, Is.SameAs(training));
            picker.OnArmy1Click();
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("ArmyButton1"));
            Assert.That(city.Barracks.ArmyInTraining, Is.SameAs(training), "Choosing an option is not starting production.");
        }
        finally
        {
            picker.OnExitClick();
            panelField.SetValue(unity, originalPanel);
            Object.Destroy(shippedPanel);
            ApplyViewport(width, height);
        }
    }

    [UnityTest]
    public IEnumerator ProductionPicker_ManagementThenSingleCityNeverLeaksManagementChrome()
    {
        var player = Game.Current.GetCurrentPlayer();
        OpenProductionPicker(true);
        yield return null;
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        var summary = picker.transform.Find("WismProductionPanelSummary");
        Assert.That(summary, Is.Not.Null);
        Assert.That(summary.gameObject.activeInHierarchy, Is.True);
        var bounds = ((RectTransform)summary).anchorMin;
        Assert.That(bounds.y, Is.EqualTo(1), "Management details must sit above, not cover, the picker.");
        Assert.That(((RectTransform)summary).rect.height, Is.EqualTo(44f).Within(0.1f));
        Assert.That(summary.GetComponent<Image>().sprite, Is.SameAs(picker.GetComponent<Image>().sprite));
        Assert.That(summary.GetComponent<Image>().color, Is.EqualTo(Color.white));
        Assert.That(summary.Find("ProductionMinimapOverlay"), Is.Null, "Do not create a second minimap.");
        Assert.That(summary.Find("ProductionJumpRow").gameObject.activeSelf, Is.False, "No blank route buttons for an idle city.");
        var classicFont = picker.GetComponentsInChildren<Button>().Single(button => button.name == "ExitButton")
            .GetComponentInChildren<Text>().font;
        foreach (var text in summary.GetComponentsInChildren<Text>())
            Assert.That(text.font, Is.SameAs(classicFont), text.name);
        ClickProductionButton(picker, "NextProductionCityButton");
        Assert.That(picker.gameObject.activeSelf, Is.True, "A cloned navigation button must not inherit Exit.");
        ClickProductionButton(picker, "PreviousProductionCityButton");
        Assert.That(picker.gameObject.activeSelf, Is.True);
        int width = Screen.width, height = Screen.height;
        try
        {
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(3440, 1440) })
            {
                ApplyViewport(size.x, size.y);
                yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y);
                yield return null;
                string capturePath = null;
                yield return CaptureProductionPicker(picker, path => capturePath = path);
                foreach (var text in summary.GetComponentsInChildren<Text>())
                    AssertProductionTextRendered(text, capturePath);
                var children = summary.Find("ProductionNavigationRow").Cast<Transform>().ToArray();
                for (int i = 1; i < children.Length; i++)
                {
                    var left = new Vector3[4];
                    var right = new Vector3[4];
                    ((RectTransform)children[i - 1]).GetWorldCorners(left);
                    ((RectTransform)children[i]).GetWorldCorners(right);
                    Assert.That(left[2].x, Is.LessThanOrEqualTo(right[0].x), "Navigation controls must not overlap.");
                }
            }
        }
        finally { ApplyViewport(width, height); }
        picker.OnExitClick();
        OpenProductionPicker(false);
        yield return null;
        Assert.That(summary.gameObject.activeInHierarchy, Is.False);
        Assert.That(picker.GetPanelMode(), Is.EqualTo(ProductionPanelMode.SingleCity));
        picker.OnExitClick();
        OpenProductionPicker(true);
        yield return null;
        Assert.That(summary.gameObject.activeInHierarchy, Is.True);
        Assert.That(picker.GetComponentsInChildren<RectTransform>(true).Count(rect => rect.name == summary.name), Is.EqualTo(1));
        picker.OnExitClick();
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
    }

    private void OpenProductionPicker(bool management)
    {
        var player = Game.Current.GetCurrentPlayer();
        typeof(UnityManager).GetMethod(management ? "ShowProductionManagementPanel" : "ShowProductionPanel",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(unity, new object[] { management ? (object)player : player.Capitol });
    }

    [UnityTest]
    public IEnumerator ProductionPicker_CompactRoutesNavigateWithoutClosingOrChangingProduction()
    {
        var player = Game.Current.GetCurrentPlayer();
        var source = player.Capitol;
        var destination = Game.Current.Players.First(other => other != player).Capitol;
        player.ClaimCity(destination);
        OpenProductionPicker(true);
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        ClickProductionButton(picker, "ArmyButton1");
        ClickProductionButton(picker, "LocButton");
        picker.SelectDestination(destination);
        yield return WaitFor(() => source.Barracks.ProducingArmy());
        picker.OnExitClick();
        OpenProductionPicker(true);
        yield return null;
        var training = source.Barracks.ArmyInTraining;
        var before = State();
        var summary = (RectTransform)picker.transform.Find("WismProductionPanelSummary");
        Assert.That(summary.rect.height, Is.EqualTo(84f).Within(0.1f));
        var to = summary.GetComponentsInChildren<Button>().Single(button => button.name == "DestinationProductionCityButton");
        Assert.That(to.GetComponentInChildren<Text>().text, Is.EqualTo("To " + destination.DisplayName));
        ClickProductionButton(picker, to.name);
        Assert.That(picker.gameObject.activeSelf, Is.True);
        Assert.That(picker.GetViewModel().SelectedCity.City, Is.SameAs(destination));
        yield return CaptureProductionPicker(picker, _ => { }, "-routed");
        ClickProductionButton(picker, "SourceProductionCityButton1");
        Assert.That(picker.gameObject.activeSelf, Is.True);
        Assert.That(picker.GetViewModel().SelectedCity.City, Is.SameAs(source));
        ClickProductionButton(picker, "NextProductionCityButton");
        Assert.That(picker.GetViewModel().SelectedCity.City, Is.SameAs(destination));
        ClickProductionButton(picker, "PreviousProductionCityButton");
        Assert.That(picker.GetViewModel().SelectedCity.City, Is.SameAs(source));
        Assert.That(source.Barracks.ArmyInTraining, Is.SameAs(training));
        Assert.That(State(), Is.EqualTo(before), "City navigation must not issue gameplay commands.");
        picker.OnExitClick();
    }

    [UnityTest]
    public IEnumerator ProductionPicker_StartStopAndReopenPreserveCurrentProductionDisplay()
    {
        yield return ExerciseProductionStartStop(false);
    }

    [UnityTest]
    public IEnumerator ProductionPicker_ManagementProdDismissesAndRestoresGameInput()
    {
        yield return ExerciseProductionStartStop(true);
    }

    private IEnumerator ExerciseProductionStartStop(bool management)
    {
        var city = Game.Current.GetCurrentPlayer().Capitol;
        OpenProductionPicker(management);
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        picker.OnProdClick();
        Assert.That(picker.gameObject.activeSelf, Is.True, "Prod without a selected army must do nothing.");
        Assert.That(city.Barracks.ProducingArmy(), Is.False);
        foreach (var name in new[] { "ArmyButton1", "ArmyButton2", "ArmyButton3", "ArmyButton4" })
        {
            ClickProductionButton(picker, name);
            yield return null;
            Assert.That(city.Barracks.ProducingArmy(), Is.False, "Selecting an option must not start production.");
        }
        ClickProductionButton(picker, "ProdButton");
        Assert.That(picker.gameObject.activeSelf, Is.False, "Prod closes both production flows like Exit.");
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
        Assert.That(unity.ProductionMode, Is.EqualTo(ProductionMode.None));
        yield return WaitFor(() => city.Barracks.ProducingArmy());
        Assert.That(city.Barracks.ArmyInTraining.ArmyInfo.ShortName,
            Is.EqualTo(city.Barracks.GetProductionKinds()[3].ArmyInfoName));
        OpenProductionPicker(management);
        yield return null;
        var current = picker.transform.Find("ClassicProductionPicker/CurrentArmyKind");
        Assert.That(current.gameObject.activeInHierarchy, Is.True);
        Assert.That(current.GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(picker.transform.Find("ClassicProductionPicker/TurnsRemainingText").GetComponent<Text>().text,
            Is.EqualTo(city.Barracks.ArmyInTraining.TurnsToProduce + "t"));
        ClickProductionButton(picker, "StopButton");
        yield return WaitFor(() => !city.Barracks.ProducingArmy());
        ClickProductionButton(picker, "ExitButton");
        OpenProductionPicker(management);
        yield return null;
        Assert.That(current.gameObject.activeInHierarchy, Is.False);
        Assert.That(picker.transform.Find("ClassicProductionPicker/TurnsRemainingText").GetComponent<Text>().text, Is.EqualTo("None"));
        picker.OnExitClick();
    }

    private static void ClickProductionButton(CityProduction picker, string name)
    {
        var button = picker.GetComponentsInChildren<Button>().Single(item => item.name == name);
        Assert.That(button.IsInteractable(), Is.True, name);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }

    private static IEnumerator CaptureProductionPicker(CityProduction picker, System.Action<string> captured, string suffix = "")
    {
        return CaptureClassicPanel(picker, captured, "production-picker-" + picker.GetPanelMode() + suffix);
    }

    private static IEnumerator CaptureClassicPanel(Component picker, System.Action<string> captured, string name)
    {
        // Batch Game View has no screen texture. Render the real overlay through the
        // camera temporarily, then restore it; interaction assertions use the original canvas.
        var canvas = picker.GetComponentInParent<Canvas>().rootCanvas;
        var mode = canvas.renderMode;
        var camera = canvas.worldCamera;
        var distance = canvas.planeDistance;
        int order = canvas.sortingOrder;
        var depths = canvas.GetComponentsInChildren<RectTransform>(true)
            .Where(rect => rect != canvas.transform).Select(rect => (rect, position: rect.localPosition)).ToArray();
        try
        {
            if (mode == RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = Camera.main.nearClipPlane + 1;
                // Screen overlays composite after camera canvases, irrespective of their sorting orders.
                int cameraOrder = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                    .Where(other => other != canvas && other.renderMode != RenderMode.ScreenSpaceOverlay)
                    .Select(other => other.sortingOrder).DefaultIfEmpty(0).Max();
                canvas.sortingOrder = Mathf.Min(short.MaxValue, cameraOrder + 1);
                // Overlay order ignores local Z; a camera render must flatten it to match that contract.
                foreach (var item in depths)
                    item.rect.localPosition = new Vector3(item.position.x, item.position.y, 0);
                Canvas.ForceUpdateCanvases();
            }
            // Canvas render-mode/sorting changes need a rendered frame, not only a layout rebuild.
            yield return null;
            Canvas.ForceUpdateCanvases();
            captured(Capture(name + "-camera-projection"));
        }
        finally
        {
            foreach (var item in depths) item.rect.localPosition = item.position;
            canvas.renderMode = mode;
            canvas.worldCamera = camera;
            canvas.planeDistance = distance;
            canvas.sortingOrder = order;
            Canvas.ForceUpdateCanvases();
        }
        yield return null;
    }

    private static void AssertProductionTextRendered(Text text, string path)
    {
        var corners = new Vector3[4];
        text.rectTransform.GetWorldCorners(corners);
        var canvas = text.GetComponentInParent<Canvas>().rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 lower = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 upper = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        var texture = new Texture2D(2, 2);
        try
        {
            Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes(path)), Is.True);
            int ink = 0;
            for (int y = Mathf.Max(0, Mathf.CeilToInt(lower.y)); y < Mathf.Min(texture.height, upper.y); y++)
            for (int x = Mathf.Max(0, Mathf.CeilToInt(lower.x)); x < Mathf.Min(texture.width, upper.x); x++)
            {
                Color pixel = texture.GetPixel(x, y);
                if (pixel.r < 0.25f && pixel.g < 0.25f && pixel.b < 0.25f) ink++;
            }
            Assert.That(ink, Is.GreaterThan(15), "Production values must actually render in both rows: " + text.transform.parent.name);
        }
        finally { Object.Destroy(texture); }
    }
}
