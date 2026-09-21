using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using Assets.Tests.PlayMode;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Commands.Armies;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.MapObjects;

public sealed partial class ArmyUiInputTests
{
    [UnityTest]
    public IEnumerator ObserveNavigation_OffStillPresentsAiCombatWhenCombatOptionIsOn()
    {
        unity.ObserveAiMovement = false;
        yield return ExerciseBattlePresentation(false, false, true, true, true);
    }

    [UnityTest]
    public IEnumerator ObserveNavigation_OffDoesNotSuppressHumanMovementFollowing()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.ObserveAiMovement = false;
        unity.InteractiveUI = true;
        manager.SelectArmies(new List<Army> { hero });
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.enabled = false;
        var renderer = unity.GetComponent<ArmyManager>();
        var camera = Camera.main.GetComponent<CameraFollow>();
        camera.target = null;
        var move = new MoveOnceCommand(manager.ControllerProvider.ArmyController, new List<Army> { hero }, hero.X + 3, hero.Y);
        Assert.That(move.Execute(), Is.EqualTo(ActionState.InProgress));
        renderer.DrawArmyGameObjects();
        Assert.That(camera.target, Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator ObserveNavigation_MenuPreferencesAreIndependentAndDoNotIssueOrders()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        yield return Click(ScreenPoint(hero.Tile));
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var before = TerminalState();
        int command = unity.LastCommandId;
        yield return Click(MenuPoint("OpenGameMenu"));
        var observe = PreferenceToggle("ObserveMovement");
        var combat = PreferenceToggle("ShowAiCombat");
        Assert.That(observe.isOn && combat.isOn, Is.True);
        yield return Click(ControlPoint(observe.transform));
        Assert.That(unity.ObserveAiMovement, Is.False);
        Assert.That(unity.ShowAiCombat, Is.True);
        yield return Tap(ControlPoint(combat.transform));
        Assert.That(unity.ShowAiCombat, Is.False);
        EventSystem.current.SetSelectedGameObject(observe.gameObject);
        yield return PressJourneyKey(Key.Enter);
        Assert.That(unity.ObserveAiMovement, Is.True, "Keyboard submit toggles the same preference.");
        Assert.That(unity.ShowAiCombat, Is.False);
        yield return PressJourneyKey(Key.E);
        yield return PressJourneyKey(Key.C);
        yield return PressJourneyKey(Key.Escape);
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Assert.That(TerminalState(), Is.EqualTo(before));
        Assert.That(unity.LastCommandId, Is.EqualTo(command));
        unity.GameMenu.Open();
        Assert.That(observe.isOn, Is.True);
        Assert.That(combat.isOn, Is.False);
    }

    [UnityTest]
    public IEnumerator ObserveNavigation_PreferenceRoundTripAndLegacyDefault()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var originalSnapshot = PersistanceManager.GetLastSnapshot();
        string filename = "observe-proof-" + System.Guid.NewGuid().ToString("N") + ".json";
        string path = Path.Combine(PersistanceManager.SaveDirectory, filename);
        try
        {
            Assert.That(JsonConvert.DeserializeObject<Assets.Scripts.Persistance.Entities.UnityGameEntity>("{}").ObserveAiMovement, Is.True);
            Assert.That(new Assets.Scripts.UnityGame.Persistance.Entities.UnityNewGameEntity().ObserveAiMovement, Is.True);
            foreach (bool observe in new[] { true, false })
            {
                unity.ObserveAiMovement = observe;
                unity.ShowAiCombat = !observe;
                PersistanceManager.Save(filename, "Observe preference", unity);
                PersistanceManager.LoadEntities(path, unity);
                unity.ObserveAiMovement = !observe;
                unity.ShowAiCombat = observe;
                typeof(PersistanceManager).GetMethod("LoadLastSnapshot", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null, new object[] { unity });
                Assert.That(unity.ObserveAiMovement, Is.EqualTo(observe));
                Assert.That(unity.ShowAiCombat, Is.EqualTo(!observe));
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            typeof(PersistanceManager).GetMethod("SetLastSnapshot", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { originalSnapshot });
        }
    }

    [UnityTest]
    public IEnumerator ObserveNavigation_OrderedAiMovementHasIdenticalStateWithFollowingOnOrOff()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.enabled = false;
        input.enabled = false;
        unity.InteractiveUI = true;
        hero.Player.IsHuman = false;
        input.SetInputMode(InputMode.AITurn);
        var original = hero.Tile;
        int id = hero.Id;
        var snapshot = JsonConvert.SerializeObject(Game.Current.Snapshot());
        var renderer = unity.GetComponent<ArmyManager>();
        renderer.enabled = false;
        var camera = Camera.main.GetComponent<CameraFollow>();
        var expected = new List<string>();
        foreach (bool observe in new[] { true, false })
        {
            renderer.Reset();
            manager.ControllerProvider.GameController.LoadSnapshot(JsonConvert.DeserializeObject<GameEntity>(snapshot));
            hero = Game.Current.GetCurrentPlayer().GetArmies().OfType<Hero>().Single(army => army.Id == id);
            unity.ObserveAiMovement = observe;
            renderer.DrawArmyGameObjects();
            camera.target = null;
            var armies = new List<Army> { hero };
            Assert.That(new SelectArmyCommand(manager.ControllerProvider.ArmyController, armies).Execute(), Is.EqualTo(ActionState.Succeeded));
            var move = new MoveOnceCommand(manager.ControllerProvider.ArmyController, armies, original.X + 3, original.Y);
            int step = 0;
            int followed = 0;
            ActionState result;
            do
            {
                result = move.Execute();
                Assert.That(result, Is.Not.EqualTo(ActionState.Failed));
                renderer.DrawArmyGameObjects();
                if (camera.target != null) followed++;
                if (!observe) Assert.That(camera.target, Is.Null, "AI movement must not take the camera when Observe is off.");
                string state = result + ":" + TerminalState();
                if (observe) expected.Add(state);
                else Assert.That(state, Is.EqualTo(expected[step]));
                Assert.That(++step, Is.LessThan(64));
            } while (result == ActionState.InProgress);
            Assert.That(hero.X, Is.EqualTo(original.X + 3));
            Assert.That(hero.Y, Is.EqualTo(original.Y));
            Assert.That(step, Is.EqualTo(expected.Count));
            if (observe) Assert.That(followed, Is.GreaterThan(0));
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator ObserveNavigation_ToggleDuringAiTurnClearsTargetButNotCombatPreference()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.enabled = false;
        unity.InteractiveUI = true;
        hero.Player.IsHuman = false;
        input.SetInputMode(InputMode.AITurn);
        var camera = Camera.main.GetComponent<CameraFollow>();
        camera.target = hero == null ? null : unity.transform;
        yield return Click(MenuPoint("OpenGameMenu"));
        var before = TerminalState();
        yield return Click(ControlPoint(PreferenceToggle("ObserveMovement").transform));
        Assert.That(unity.ObserveAiMovement, Is.False);
        Assert.That(camera.target, Is.Null);
        Assert.That(unity.ShowAiCombat, Is.True);
        yield return PressJourneyKey(Key.Escape);
        Assert.That(input.InputMode, Is.EqualTo(InputMode.AITurn));
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator ObserveNavigation_CapitalMenuAndKeyboardPreserveSelectionAndOrders()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.InteractiveUI = true;
        yield return Click(ScreenPoint(hero.Tile));
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.ObserveAiMovement = false;
        var before = TerminalState();
        int command = unity.LastCommandId;
        var camera = Camera.main.GetComponent<CameraFollow>();
        camera.target = null;
        yield return Click(MenuPoint("OpenViewMenu"));
        yield return Click(MenuPoint("Capital"));
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        var position = camera.transform.position;
        camera.SetCameraTarget(new Vector3(1, 1, 0));
        yield return PressJourneyKey(Key.C);
        Assert.That(camera.transform.position, Is.EqualTo(position));
        Assert.That(TerminalState(), Is.EqualTo(before));
        Assert.That(unity.LastCommandId, Is.EqualTo(command));
        Game.Current.Transition(GameState.GameOver);
        unity.enabled = false;
        before = TerminalState();
        camera.SetCameraTarget(new Vector3(1, 1, 0));
        yield return PressJourneyKey(Key.C);
        Assert.That(camera.transform.position, Is.EqualTo(position));
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator ObserveNavigation_RazedCapitalIsDisabledWithoutOrders()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.InteractiveUI = true;
        hero.Player.Capitol.Raze();
        Assert.That(unity.CanNavigateToCapital, Is.False);
        var before = TerminalState();
        yield return Click(MenuPoint("OpenViewMenu"));
        Assert.That(MenuButton("Capital").interactable, Is.False);
        Assert.That(MenuButton("Capital").GetComponentInChildren<Text>().text, Is.EqualTo("No owned capital"));
        yield return Click(MenuPoint("Capital"));
        yield return PressJourneyKey(Key.C);
        Assert.That(TerminalState(), Is.EqualTo(before));
        yield return PressJourneyKey(Key.Escape);
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
    }

    [UnityTest] public IEnumerator ObserveNavigation_Viewport1024() => ObserveViewport(1024, 768);
    [UnityTest] public IEnumerator ObserveNavigation_Viewport1280() => ObserveViewport(1280, 720);
    [UnityTest] public IEnumerator ObserveNavigation_Viewport1920() => ObserveViewport(1920, 1080);
    [UnityTest] public IEnumerator ObserveNavigation_ViewportUltrawide() => ObserveViewport(2560, 1080);

    private IEnumerator ObserveViewport(int width, int height)
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        AssertBatchWindowless();
        int oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            Screen.SetResolution(width, height, false);
            yield return null;
            yield return null;
            yield return Tap(MenuPoint("OpenGameMenu"));
            Canvas.ForceUpdateCanvases();
            foreach (var item in unity.GameMenu.GetComponentsInChildren<Selectable>())
            {
                var rect = (RectTransform)item.transform;
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Assert.That(corners.All(point => point.x >= 0 && point.y >= 0 && point.x <= Screen.width && point.y <= Screen.height), Is.True);
                Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(44));
            }
            CaptureUiCanvas(unity.GameMenu.GetComponentInChildren<Canvas>(), "observe-menu", width, height);
            yield return null;
            yield return Tap(ControlPoint(PreferenceToggle("ObserveMovement").transform));
            Assert.That(unity.ObserveAiMovement, Is.False);
            unity.GameMenu.Close();
            yield return Tap(MenuPoint("OpenViewMenu"));
            Assert.That(unity.GameMenu.IsOpen, Is.True);
            unity.GameMenu.Close();
        }
        finally { Screen.SetResolution(oldWidth, oldHeight, false); }
    }

    private Toggle PreferenceToggle(string name) => unity.GameMenu.GetComponentsInChildren<Toggle>(true).Single(toggle => toggle.name == name);
    private static Vector2 ControlPoint(Transform control)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)control;
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }
}
