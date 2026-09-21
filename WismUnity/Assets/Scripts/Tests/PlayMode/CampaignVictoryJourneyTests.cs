using System.Collections;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.Tiles;
using Assets.Scripts.UI;
using Assets.Tests.PlayMode;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;

public sealed partial class ArmyUiInputTests
{
    [UnityTest] public IEnumerator CampaignVictory_AiOnlyRemainsTerminal() => TerminalCampaign(false);
    [UnityTest] public IEnumerator CampaignVictory_HumanRemainsTerminal() => TerminalCampaign(true);

    private IEnumerator TerminalCampaign(bool human)
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        AssertBatchWindowless();
        var winner = Game.Current.GetCurrentPlayer();
        var playerSnapshot = Game.Current.Snapshot();
        foreach (var city in Game.Current.Players.Where(player => player != winner).SelectMany(player => player.GetCities()).ToArray())
            winner.ClaimCity(city);
        Game.Current.EndTurn();
        Game.Current.StartTurn(); // The opponent is eliminated through the normal city-loss rule.
        Game.Current.EndTurn();
        winner.IsHuman = human;
        Game.Current.EndTurn();
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
        var commands = manager.ControllerProvider.CommandController;
        for (int i = 0; i < 4; i++)
            commands.AddCommand(new Wism.Client.Commands.Players.EndTurnCommand(
                manager.ControllerProvider.GameController, winner));
        var lastQueued = commands.GetLastCommand().Id;
        var before = TerminalState();
        for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
        Assert.That(TerminalState(), Is.EqualTo(before), "No turns, production, ownership or army mutations after victory.");
        Assert.That(commands.GetLastCommand().Id, Is.EqualTo(lastQueued), "No AI commands may be generated.");
        Assert.That(unity.LastCommandId, Is.EqualTo(lastQueued), "Stale orders must drain without running processors.");
        Assert.That(input.CanAcceptGameplayInput, Is.False);
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
        Assert.That(unity.CampaignResultText, Does.Contain(winner.Clan.DisplayName));
        var text = GameObject.FindGameObjectWithTag("NotificationBox").GetComponent<Text>();
        Assert.That(text.text, Is.EqualTo(unity.CampaignResultText));
        yield return new WaitForSecondsRealtime(3.2f);
        Assert.That(text.text, Is.EqualTo(unity.CampaignResultText), "Victory result must not expire as an ordinary notification.");
        yield return PressJourneyKey(Key.E);
        yield return PressJourneyKey(Key.N);
        yield return Click(ScreenPoint(hero.Tile));
        Assert.That(TerminalState(), Is.EqualTo(before));
        Assert.That(commands.GetLastCommand().Id, Is.EqualTo(lastQueued));

        // Load commands remain admissible even while stale gameplay is rejected.
        var load = new Wism.Client.Commands.Games.LoadGameCommand(manager.ControllerProvider.GameController, playerSnapshot);
        Assert.That(load.CanExecuteInCurrentState, Is.True);
        Assert.That(load.Execute(), Is.EqualTo(Wism.Client.Controllers.ActionState.Succeeded));
        unity.Reset();
        yield return null;
        Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
        Assert.That(input.CanAcceptGameplayInput, Is.True);
        Assert.That(unity.CampaignResultText, Is.Null);
    }

    [UnityTest]
    public IEnumerator CampaignVictory_InspectionSaveAndLoadKeyboardRemainAvailable()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        Game.Current.Transition(GameState.GameOver);
        yield return new WaitForFixedUpdate();
        var before = TerminalState();
        yield return PressJourneyKey(Key.S);
        yield return WaitFor(() => input.InputMode == InputMode.SaveGamePicker && unity.SaveLoadPicker.IsInitialized());
        Assert.That(TerminalState(), Is.EqualTo(before));
        unity.SaveLoadPicker.gameObject.SetActive(false);
        input.SetInputMode(InputMode.Game);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
            new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.LeftShift, Key.L));
        yield return null;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        yield return WaitFor(() => input.InputMode == InputMode.LoadGamePicker);
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    private static string TerminalState()
    {
        var snapshot = Game.Current.Snapshot();
        snapshot.Timestamp = System.DateTime.MinValue;
        return Newtonsoft.Json.JsonConvert.SerializeObject(snapshot);
    }

    [UnityTest] public IEnumerator CampaignVictory_CaptureRaze1024() => CaptureRazeVictory(1024, 768);
    [UnityTest] public IEnumerator CampaignVictory_CaptureRaze1280() => CaptureRazeVictory(1280, 720);
    [UnityTest] public IEnumerator CampaignVictory_CaptureRaze1920() => CaptureRazeVictory(1920, 1080);

    private IEnumerator CaptureRazeVictory(int width, int height)
    {
        int originalWidth = Screen.width, originalHeight = Screen.height;
        bool interactive = unity.InteractiveUI;
        try
        {
            ApplyViewport(width, height);
            yield return WaitFor(() => Screen.width == width && Screen.height == height);
            AssertBatchWindowless();
            var player = hero.Player;
            var enemy = Game.Current.Players[1];
            var city = enemy.Capitol;
            enemy.HireHero(city.Tile);
            enemy.GetArmies().OfType<Hero>().Single().Strength = 1;
            hero.Strength = 9;
            city.Defense = 0;
            var footprint = city.GetTiles().ToArray();
            yield return AttackCapitalWithDevice();
            yield return new WaitForLastCommand(manager.ControllerProvider);
            yield return WaitFor(() => input.InputMode == InputMode.Game);
            Assert.That(city.Player, Is.SameAs(player));
            Assert.That(enemy.GetCities(), Is.Empty);

            if (!Game.Current.ArmiesSelected())
            {
                yield return Click(ScreenPoint(hero.Tile));
                yield return WaitFor(() => Game.Current.ArmiesSelected(), () => "Reselect victorious army.");
            }
            if (hero.Tile.City != city)
            {
                yield return Click(ScreenPoint(city.Tile));
                yield return WaitFor(() => hero.Tile.City == city, () => "Enter captured city before razing.");
                yield return new WaitForLastCommand(manager.ControllerProvider);
            }

            unity.InteractiveUI = true;
            yield return PressJourneyKey(Key.R);
            var confirmation = GameObject.FindGameObjectWithTag("YesNoBox").GetComponent<YesNoBox>();
            yield return WaitFor(() => confirmation.IsActive() && input.InputMode == InputMode.UI,
                () => $"Raze confirmation: mode={input.InputMode}; selected={Game.Current.ArmiesSelected()}; city={hero.Tile.City?.ShortName}");
            Assert.That(World.Current.GetCities(), Does.Contain(city), "Opening confirmation cannot raze the city.");
            var yesButton = confirmation.GetComponentsInChildren<Button>()
                .Single(button => button.GetComponentInChildren<Text>()?.text == "Yes");
            bool clickedYes = false;
            yesButton.onClick.AddListener(() => clickedYes = true);
            yield return PressJourneyButton(yesButton);
            Assert.That(clickedYes, Is.True, "The real Yes button must receive the click.");
            unity.InteractiveUI = false;
            yield return WaitFor(() => !player.GetCities().Contains(city) && footprint.All(tile => tile.City == null), () =>
                $"Confirmed city must leave holdings and all four tiles. input={input.InputMode}; yes={confirmation.Answer}; active={confirmation.IsActive()}; interactive={unity.InteractiveUI}; stage={RazeJourneyStage()}");
            yield return new WaitForLastCommand(manager.ControllerProvider);
            yield return null;
            var map = unity.WorldTilemap.GetComponent<Tilemap>();
            Assert.That(footprint.Length, Is.EqualTo(4));
            foreach (var tile in footprint)
            {
                var cell = map.WorldToCell(unity.WorldTilemap.ConvertGameToUnityVector(tile.X, tile.Y));
                Assert.That(map.GetTile(cell), Is.TypeOf<RuinsTile>(), $"Razed quadrant {tile.X},{tile.Y}");
                Assert.That(tile.City, Is.Null);
            }
            Capture("campaign-four-ruins");
            yield return WaitFor(() => input.InputMode == InputMode.Game && input.CanAcceptGameplayInput);
            for (int turn = 0; turn < 3 && Game.Current.GameState != GameState.GameOver; turn++)
            {
                yield return PressJourneyKey(Key.E);
                yield return WaitFor(() => Game.Current.GameState == GameState.GameOver ||
                    (input.InputMode == InputMode.Game && input.CanAcceptGameplayInput && enemy.IsDead));
            }
            Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
            Assert.That(enemy.IsDead, Is.True);
            Assert.That(player.IsDead, Is.False);
            Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(player));
            var finalState = TerminalState();
            for (int frame = 0; frame < 20; frame++) yield return new WaitForFixedUpdate();
            Assert.That(TerminalState(), Is.EqualTo(finalState));
            Assert.That(unity.CampaignResultText, Does.Contain(player.Clan.DisplayName));
            Trace("campaign.capture-raze-victory", ScreenPoint(hero.Tile));
            Capture("campaign-victory");
        }
        finally
        {
            unity.InteractiveUI = interactive;
            ApplyViewport(originalWidth, originalHeight);
        }
    }

    private int RazeJourneyStage()
    {
        var processors = (System.Collections.IEnumerable)typeof(Assets.Scripts.Managers.UnityManager)
            .GetField("commandProcessors", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(unity);
        var processor = processors.Cast<object>().Single(item => item is Assets.Scripts.CommandProcessors.RazeCityDefensesProcessor);
        var stager = (Assets.Scripts.CommandProcessors.CutsceneStager)processor.GetType()
            .GetField("stager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(processor);
        return stager?.SceneIndex ?? -1;
    }
}
