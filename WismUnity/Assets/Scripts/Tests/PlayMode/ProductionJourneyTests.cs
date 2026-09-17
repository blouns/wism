using System.Collections;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;

public sealed partial class ArmyUiInputTests
{
    [UnityTest] public IEnumerator ProductionJourney_Single1024() => ProductionJourney(false, 1024, 768);
    [UnityTest] public IEnumerator ProductionJourney_Single1280() => ProductionJourney(false, 1280, 720);
    [UnityTest] public IEnumerator ProductionJourney_Single1920() => ProductionJourney(false, 1920, 1080);
    [UnityTest] public IEnumerator ProductionJourney_Owned1024() => ProductionJourney(true, 1024, 768);
    [UnityTest] public IEnumerator ProductionJourney_Owned1280() => ProductionJourney(true, 1280, 720);
    [UnityTest] public IEnumerator ProductionJourney_Owned1920() => ProductionJourney(true, 1920, 1080);
    [UnityTest] public IEnumerator ProductionJourney_TouchControls() => ProductionJourney(true, 1024, 768, true);
    [UnityTest] public IEnumerator CampaignProduction_FirstUnit1024() => ProduceFirstUnit(1024, 768);
    [UnityTest] public IEnumerator CampaignProduction_FirstUnit1280() => ProduceFirstUnit(1280, 720);
    [UnityTest] public IEnumerator CampaignProduction_FirstUnit1920() => ProduceFirstUnit(1920, 1080);

    private IEnumerator ProduceFirstUnit(int width, int height)
    {
        int originalWidth = Screen.width, originalHeight = Screen.height;
        try
        {
            ApplyViewport(width, height);
            yield return WaitFor(() => Screen.width == width && Screen.height == height);
            AssertBatchWindowless();
            var player = Game.Current.GetCurrentPlayer();
            var city = player.Capitol;
            var before = player.GetArmies().Select(army => army.Id).ToArray();
            yield return OpenProductionWithDevice(false);
            var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
            var kinds = city.Barracks.GetProductionKinds().ToList();
            int choice = kinds.FindIndex(kind => kind.ArmyInfoName == "LightInfantry");
            Assert.That(choice, Is.GreaterThanOrEqualTo(0));
            yield return PressProductionControl(picker, "ArmyButton" + (choice + 1));
            yield return PressProductionControl(picker, "ProdButton");
            yield return WaitFor(() => input.InputMode == InputMode.Game && city.Barracks.ProducingArmy());
            for (int round = 0; round < 3 && !player.GetArmies().Any(army => !before.Contains(army.Id) && army.ShortName == "LightInfantry"); round++)
            {
                yield return PressJourneyKey(Key.E);
                yield return WaitFor(() => Game.Current.GetCurrentPlayer() != player && input.InputMode == InputMode.Game && input.CanAcceptGameplayInput,
                    () => "Waiting for opponent input readiness: " + Game.Current.GameState);
                yield return PressJourneyKey(Key.E);
                yield return WaitFor(() => Game.Current.GetCurrentPlayer() == player && input.InputMode == InputMode.Game && input.CanAcceptGameplayInput,
                    () => "Waiting for returning player input readiness: " + Game.Current.GameState);
            }
            var produced = player.GetArmies().Where(army => !before.Contains(army.Id) && army.ShortName == "LightInfantry").ToArray();
            Assert.That(produced.Length, Is.EqualTo(1), "The selected unit must actually appear, exactly once.");
            Assert.That(produced[0].Tile.City, Is.SameAs(city));
            Assert.That(produced[0].Player, Is.SameAs(player));
            Trace("campaign.production.first-unit", ScreenPoint(produced[0].Tile));
            Capture("campaign-production-first-unit");
        }
        finally { ApplyViewport(originalWidth, originalHeight); }
    }

    private IEnumerator PressJourneyKey(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    [UnityTest]
    public IEnumerator ProductionJourney_ShiftLStillLoadsRatherThanManagingCities()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift, Key.L));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return WaitFor(() => input.InputMode == InputMode.LoadGamePicker);
        Assert.That(GameObject.FindGameObjectWithTag("CityProductionPanel"), Is.Null);
        Assert.That(Game.Current.GetCurrentPlayer().Capitol.Barracks.ProducingArmy(), Is.False);
    }

    [UnityTest]
    public IEnumerator ProductionJourney_RejectedCityPickNeverSelectsOrMovesArmy()
    {
        WismUiInputAdapter.ConfigureGameEventSystem();
        WismUiInputAdapter.ConfigureGameEventSystem();
        var events = UnityEngine.EventSystems.EventSystem.current;
        Assert.That(events.GetComponents<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().Length, Is.EqualTo(1));
        Assert.That(events.GetComponents<UnityEngine.EventSystems.BaseInputModule>().Count(module => module.enabled), Is.EqualTo(1));
        var before = State();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.P));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        yield return Click(ScreenPoint(World.Current.Map[hero.X + 2, hero.Y + 2]));
        Assert.That(State(), Is.EqualTo(before));
        Assert.That(input.LastPrimaryAction, Is.EqualTo("rejected"));
        Assert.That(unity.ProductionMode, Is.EqualTo(ProductionMode.SelectCity));
    }

    private IEnumerator ProductionJourney(bool management, int width, int height, bool touchControls = false)
    {
        int originalWidth = Screen.width, originalHeight = Screen.height;
        var city = Game.Current.GetCurrentPlayer().Capitol;
        try
        {
            ApplyViewport(width, height);
            yield return WaitFor(() => Screen.width == width && Screen.height == height);
            AssertBatchWindowless();
            var before = State();
            yield return OpenProductionWithDevice(management);
            var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
            Assert.That(picker.GetPanelMode(), Is.EqualTo(management ? ProductionPanelMode.Management : ProductionPanelMode.SingleCity));
            Assert.That(State(), Is.EqualTo(before), "Opening production must not move an army or queue gameplay.");
            yield return PressProductionControl(picker, "ArmyButton1", touchControls);
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name,
                Is.EqualTo("ArmyButton1"), "Pointer input must reach the actual UI module.");
            Assert.That(city.Barracks.ProducingArmy(), Is.False, "Selection alone cannot start production.");
            yield return CaptureProductionPicker(picker, _ => { }, "-device-journey");
            yield return PressProductionControl(picker, "ProdButton", touchControls);
            yield return WaitFor(() => city.Barracks.ProducingArmy() && input.InputMode == InputMode.Game);
            Assert.That(picker.gameObject.activeSelf, Is.False, "Prod must close the panel.");
            Assert.That(city.Barracks.ArmyInTraining.ArmyInfo.ShortName,
                Is.EqualTo(city.Barracks.GetProductionKinds()[0].ArmyInfoName));
            var armyInTraining = city.Barracks.ArmyInTraining;
            yield return OpenProductionWithDevice(management);
            Assert.That(city.Barracks.ArmyInTraining, Is.SameAs(armyInTraining), "Reopening cannot reset training.");
            yield return PressProductionControl(picker, "StopButton", touchControls);
            yield return WaitFor(() => !city.Barracks.ProducingArmy());
            yield return PressProductionControl(picker, "ExitButton", touchControls);
            yield return WaitFor(() => input.InputMode == InputMode.Game && !picker.gameObject.activeSelf);
            Assert.That(unity.ProductionMode, Is.EqualTo(ProductionMode.None));
            Trace("production.journey.complete", ScreenPoint(city.Tile));
        }
        finally { ApplyViewport(originalWidth, originalHeight); }
    }

    private IEnumerator OpenProductionWithDevice(bool management)
    {
        var key = management ? Key.L : Key.P;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        if (!management)
        {
            yield return WaitFor(() => unity.ProductionMode == ProductionMode.SelectCity,
                () => "P did not enter city selection.");
            yield return Click(ScreenPoint(Game.Current.GetCurrentPlayer().Capitol.Tile));
        }
        yield return WaitFor(() => input.InputMode == InputMode.UI && GameObject.FindGameObjectWithTag("CityProductionPanel") != null,
            () => "Production picker did not open through device input.");
        Trace(management ? "production.open-owned" : "production.open-single", Vector2.zero);
    }

    private IEnumerator PressProductionControl(CityProduction picker, string name, bool touchControl = false)
    {
        Canvas.ForceUpdateCanvases();
        var button = picker.GetComponentsInChildren<Button>().Single(item => item.name == name);
        Assert.That(button.IsInteractable(), Is.True, name);
        var hit = button.GetComponent<WismHitArea>();
        Assert.That(hit, Is.Not.Null, name);
        var center = hit.GetVisualScreenBounds().center;
        if (touchControl) yield return Tap(center);
        else yield return Click(center);
        yield return null;
        Trace("production.pointer." + name, center);
    }
}
