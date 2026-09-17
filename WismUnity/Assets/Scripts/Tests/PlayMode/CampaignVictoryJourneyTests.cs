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
            yield return WaitFor(() => !World.Current.GetCities().Contains(city), () =>
                $"Confirmed city must be removed. input={input.InputMode}; yes={confirmation.Answer}; active={confirmation.IsActive()}; interactive={unity.InteractiveUI}; stage={RazeJourneyStage()}");
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
