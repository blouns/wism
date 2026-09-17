using System;
using System.Collections;
using System.IO;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using Assets.Tests.PlayMode;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;

public sealed partial class ArmyUiInputTests
{
    [UnityTest] public IEnumerator CampaignSave_ResumeRedirectBattle1024() => SaveResumeRedirectBattle(1024, 768);
    [UnityTest] public IEnumerator CampaignSave_ResumeRedirectBattle1280() => SaveResumeRedirectBattle(1280, 720);
    [UnityTest] public IEnumerator CampaignSave_ResumeRedirectBattle1920() => SaveResumeRedirectBattle(1920, 1080);

    private IEnumerator SaveResumeRedirectBattle(int width, int height)
    {
        var directory = Path.Combine(Application.temporaryCachePath, "SaveJourney-" + Guid.NewGuid().ToString("N"));
        var previousDirectory = PersistanceManager.SaveDirectory;
        var previousSnapshot = PersistanceManager.GetLastSnapshot();
        int originalWidth = Screen.width, originalHeight = Screen.height;
        try
        {
            PersistanceManager.SaveDirectory = directory;
            ApplyViewport(width, height);
            yield return WaitFor(() => Screen.width == width && Screen.height == height);
            AssertBatchWindowless();
            // Seed a two-city realm and a bounded battle; all journey actions use devices.
            var seededCity = World.Current.GetCities().Single(city => city.ShortName == "Deserton");
            foreach (var defender in seededCity.MusterArmies().ToArray()) defender.Kill();
            seededCity.Claim(hero.Player);
            var enemy = Game.Current.Players[1];
            enemy.HireHero(enemy.Capitol.Tile);
            enemy.GetArmies().OfType<Hero>().Single().Strength = 1;
            enemy.Capitol.Defense = 0;
            hero.Strength = 9;
            yield return OpenProductionWithDevice(false);
            var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
            yield return PressProductionControl(picker, "ArmyButton1");
            yield return PressProductionControl(picker, "ProdButton");
            yield return WaitFor(() => input.CanAcceptGameplayInput && input.InputMode == InputMode.Game &&
                Game.Current.GetCurrentPlayer().Capitol.Barracks.ProducingArmy());
            var training = Game.Current.GetCurrentPlayer().Capitol.Barracks.ArmyInTraining.ArmyInfo.ShortName;

            yield return PressJourneyKey(Key.S);
            yield return WaitFor(() => input.InputMode == InputMode.SaveGamePicker && unity.SaveLoadPicker.IsInitialized());
            var savePicker = unity.SaveLoadPicker;
            var rows = savePicker.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("Button", StringComparison.Ordinal)).ToArray();
            Assert.That(rows.Length, Is.EqualTo(8));
            Assert.That(rows.All(button => button.IsInteractable()), Is.True, "Every empty slot must allow the first save.");
            yield return PressJourneyButton(rows.Single(button => button.name == "Button3"));
            yield return PressJourneyButton(savePicker.GetComponentsInChildren<Button>().Single(button => button.GetComponentInChildren<Text>()?.text == "Save"));
            var savePath = Path.Combine(directory, "WISM3.SAV");
            yield return WaitFor(() => File.Exists(savePath) && input.InputMode == InputMode.Game);
            Assert.That(Directory.GetFiles(directory).Select(Path.GetFileName), Is.EqualTo(new[] { "WISM3.SAV" }));

            yield return OpenProductionWithDevice(false);
            yield return PressProductionControl(picker, "StopButton");
            yield return WaitFor(() => !Game.Current.GetCurrentPlayer().Capitol.Barracks.ProducingArmy());
            yield return PressProductionControl(picker, "ExitButton");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift, Key.L));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return WaitFor(() => input.InputMode == InputMode.LoadGamePicker && savePicker.IsInitialized());
            rows = savePicker.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("Button", StringComparison.Ordinal)).ToArray();
            Assert.That(rows.Where(button => button.IsInteractable()).Select(button => button.name), Is.EqualTo(new[] { "Button3" }));
            yield return PressJourneyButton(rows.Single(button => button.name == "Button3"));
            yield return PressJourneyButton(savePicker.GetComponentsInChildren<Button>().Single(button => button.GetComponentInChildren<Text>()?.text == "Load"));
            yield return WaitFor(() => Game.IsInitialized() && Game.Current.GetCurrentPlayer().Capitol.Barracks.ProducingArmy() && input.CanAcceptGameplayInput);
            Assert.That(Game.Current.GetCurrentPlayer().Capitol.Barracks.ArmyInTraining.ArmyInfo.ShortName, Is.EqualTo(training));
            hero = Game.Current.GetCurrentPlayer().GetArmies().OfType<Hero>().Single();

            yield return OpenProductionWithDevice(false);
            var destination = World.Current.GetCities().Single(city => city.ShortName == "Deserton");
            yield return PressProductionControl(picker, "ArmyButton1");
            yield return PressProductionControl(picker, "LocButton");
            yield return Click(ScreenPoint(destination.Tile));
            yield return WaitFor(() => input.InputMode == InputMode.UI);
            Assert.That(input.LastPrimaryAction, Is.EqualTo("production.select-destination"));
            yield return WaitFor(() => Game.Current.GetCurrentPlayer().Capitol.Barracks.ArmyInTraining?.DestinationCity == destination);
            yield return PressProductionControl(picker, "ExitButton");
            yield return AttackCapitalWithDevice();
            Trace("campaign.save-resume-redirect-battle", ScreenPoint(hero.Tile));
            Capture("campaign-save-resume-redirect-battle");
        }
        finally
        {
            PersistanceManager.SaveDirectory = previousDirectory;
            typeof(PersistanceManager).GetMethod("SetLastSnapshot", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { previousSnapshot });
            ApplyViewport(originalWidth, originalHeight);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private IEnumerator PressJourneyButton(Button button)
    {
        Assert.That(button.IsInteractable(), Is.True, button.name);
        Canvas.ForceUpdateCanvases();
        var canvas = button.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var rect = button.GetComponent<RectTransform>();
        yield return Click(RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)));
        yield return null;
    }

    private IEnumerator AttackCapitalWithDevice()
    {
        // Let LateUpdate apply the resized viewport before computing device coordinates.
        yield return null;
        var city = Game.Current.Players[1].Capitol;
        if (!Game.Current.ArmiesSelected())
        {
            yield return Click(ScreenPoint(hero.Tile));
            yield return WaitFor(() => Game.Current.ArmiesSelected(),
                () => $"Select hero after viewport layout: {input.LastPrimaryAction}; mode={input.InputMode}");
        }
        var adjacent = World.Current.Map[city.X - 1, city.Y];
        yield return Click(ScreenPoint(adjacent));
        yield return WaitFor(() => hero.Tile == adjacent);
        yield return new WaitForLastCommand(manager.ControllerProvider);
        yield return null;
        yield return Click(ScreenPoint(city.Tile));
        Assert.That(input.LastPrimaryAction, Is.EqualTo("army.attack"),
            $"mode={input.InputMode}; moves={hero.MovesRemaining}; hero={hero.X},{hero.Y}; city={city.X},{city.Y}; selected={Game.Current.ArmiesSelected()}");
        yield return WaitFor(() => city.Player == hero.Player);
        Assert.That(hero.IsDead, Is.False);
        yield return new WaitForLastCommand(manager.ControllerProvider);
        yield return WaitFor(() => input.InputMode == InputMode.UI &&
            GameObject.FindGameObjectWithTag("CityProductionPanel") != null,
            () => "Captured city must offer its production picker.");
        var capturedPicker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        yield return PressProductionControl(capturedPicker, "ExitButton");
        yield return WaitFor(() => input.InputMode == InputMode.Game, () => "Captured-city picker Exit must return map control.");
    }
}
