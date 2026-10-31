using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UnityGame.Mapping;
using Assets.Tests.PlayMode;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Commands.Games;
using Wism.Client.Commands.Locations;
using Wism.Client.Core;

public sealed partial class ArmyUiInputTests
{
    [UnityTest] public IEnumerator TowerJourney_1024() => TowerBuildAndRaze(1024, 768);
    [UnityTest] public IEnumerator TowerJourney_1280() => TowerBuildAndRaze(1280, 720);
    [UnityTest] public IEnumerator TowerJourney_1920() => TowerBuildAndRaze(1920, 1080);
    [UnityTest] public IEnumerator TowerJourney_Ultrawide() => TowerBuildAndRaze(2560, 1080);

    private IEnumerator PrepareTowerBuilder()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var site = World.Current.Map.Cast<Tile>().Where(tile => tile.Terrain.ShortName == "Grass" &&
            !tile.HasAnyArmies() && tile.City == null && tile.Location == null)
            .OrderBy(tile => Mathf.Abs(tile.X - hero.X) + Mathf.Abs(tile.Y - hero.Y)).First();
        hero.Tile.RemoveArmies(new List<Wism.Client.MapObjects.Army> { hero });
        site.AddArmy(hero);
        hero.Player.Gold = 500;
        manager.SelectArmies(new List<Wism.Client.MapObjects.Army> { hero });
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.GetComponent<ArmyManager>().DrawArmyGameObjects();
        unity.InteractiveUI = true;
        yield return null;
    }

    private IEnumerator TowerBuildAndRaze(int width, int height)
    {
        int oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            ApplyViewport(width, height);
            yield return WaitFor(() => Screen.width == width && Screen.height == height);
            AssertBatchWindowless();
            yield return PrepareTowerBuilder();
            var site = hero.Tile;
            var player = hero.Player;
            var prompt = GameObject.FindGameObjectWithTag("YesNoBox").GetComponent<YesNoBox>();
            yield return Click(MenuPoint("OpenOrdersMenu"));
            Assert.That(MenuButton("Build").interactable, Is.True);
            yield return Click(MenuPoint("Build"));
            yield return WaitFor(() => prompt.IsActive());
            Assert.That(prompt.transform.Find("Message").GetComponent<Text>().text, Does.Contain("100 gp"));
            Assert.That(player.Gold, Is.EqualTo(500));
            Assert.That(site.IsTower(), Is.False);
            yield return PressJourneyKey(Key.Escape);
            yield return new WaitForLastCommand(manager.ControllerProvider);
            Assert.That(site.IsTower(), Is.False);
            Assert.That(player.Gold, Is.EqualTo(500));

            yield return PressJourneyKey(Key.B);
            yield return WaitFor(() => prompt.IsActive());
            yield return TowerAnswer(prompt, true);
            yield return new WaitForLastCommand(manager.ControllerProvider);
            Assert.That(site.IsTower(), Is.True);
            Assert.That(player.Gold, Is.EqualTo(400));
            var visual = TowerAt(site);
            Assert.That(visual.OwnerClanShortName, Is.EqualTo(player.Clan.ShortName));
            Assert.That(visual.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
            Assert.That(visual.IsRazed, Is.False);
            Capture("tower-built");

            yield return PressJourneyKey(Key.R);
            yield return WaitFor(() => prompt.IsActive());
            Assert.That(site.IsTower(), Is.True, "Raze must wait for confirmation.");
            yield return TowerAnswer(prompt, false);
            yield return new WaitForLastCommand(manager.ControllerProvider);
            Assert.That(site.IsTower(), Is.True);
            yield return Click(MenuPoint("OpenOrdersMenu"));
            yield return Click(MenuPoint("Raze"));
            yield return WaitFor(() => prompt.IsActive());
            yield return TowerAnswer(prompt, true);
            yield return new WaitForLastCommand(manager.ControllerProvider);
            Assert.That(site.IsTowerRazed, Is.True);
            Assert.That(site.Terrain.ShortName, Is.EqualTo("Ruins"));
            Assert.That(site.TowerOwnerClanShortName, Is.Null);
            Assert.That(TowerAt(site).IsRazed, Is.True);
            Capture("tower-razed");

            yield return PressJourneyKey(Key.B);
            yield return new WaitForLastCommand(manager.ControllerProvider);
            Assert.That(site.IsTowerRazed, Is.True);
            Assert.That(site.IsTower(), Is.False);
            Assert.That(player.Gold, Is.EqualTo(400));
            Assert.That(prompt.IsActive(), Is.False, "A ruined site is rejected before confirmation.");
            Trace("tower.build-cancel-confirm-raze-cancel-confirm-no-rebuild", ScreenPoint(site));
        }
        finally { Screen.SetResolution(oldWidth, oldHeight, false); }
    }

    [UnityTest]
    public IEnumerator TowerJourney_LoadRecreatesBuiltAndRazedVisuals()
    {
        yield return PrepareTowerBuilder();
        var site = hero.Tile;
        int x = site.X, y = site.Y;
        var build = new BuildTowerCommand(Game.Current.GetSelectedArmies(), site);
        Assert.That(build.Execute(), Is.EqualTo(Wism.Client.Controllers.ActionState.Succeeded));
        var saved = Game.Current.Snapshot();
        var enemy = Game.Current.Players[1];
        site.Claim(enemy);
        unity.GetComponent<ArmyManager>().RefreshTowerOwnershipVisuals(true);
        Assert.That(TowerAt(site).OwnerClanShortName, Is.EqualTo(enemy.Clan.ShortName));
        var load = new LoadGameCommand(manager.ControllerProvider.GameController, saved);
        Assert.That(load.Execute(), Is.EqualTo(Wism.Client.Controllers.ActionState.Succeeded));
        unity.Reset();
        yield return new WaitForFixedUpdate();
        site = World.Current.Map[x, y];
        Assert.That(site.IsTower(), Is.True);
        Assert.That(TowerAt(site).OwnerClanShortName, Is.EqualTo(Game.Current.GetCurrentPlayer().Clan.ShortName));
        Assert.That(TowerAt(site).IsRazed, Is.False);
        Assert.That(UnityEngine.Object.FindObjectsOfType<TowerOwnershipVisual>().Count(visual =>
            unity.WorldTilemap.ConvertUnityToGameVector(visual.transform.position) == new Vector2Int(x, y)), Is.EqualTo(1));
        Assert.That(new RazeTowerCommand(site.Armies, site).Execute(), Is.EqualTo(Wism.Client.Controllers.ActionState.Succeeded));
        saved = Game.Current.Snapshot();
        Assert.That(new LoadGameCommand(manager.ControllerProvider.GameController, saved).Execute(),
            Is.EqualTo(Wism.Client.Controllers.ActionState.Succeeded));
        unity.Reset();
        yield return new WaitForFixedUpdate();
        site = World.Current.Map[x, y];
        Assert.That(site.IsTowerRazed, Is.True);
        Assert.That(TowerAt(site).IsRazed, Is.True);
        Assert.That(new BuildTowerCommand(site.Armies, site).Execute(), Is.EqualTo(Wism.Client.Controllers.ActionState.Failed));
    }

    [UnityTest]
    public IEnumerator TowerJourney_OrdersUnavailableWithoutArmyAndAfterEnding()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.GameMenu.OpenOrders();
        Assert.That(MenuButton("Build").interactable, Is.False);
        Assert.That(MenuButton("Raze").interactable, Is.False);
        unity.GameMenu.Close();
        Game.Current.Transition(GameState.GameOver);
        yield return new WaitForFixedUpdate();
        unity.GameMenu.OpenOrders();
        Assert.That(MenuButton("Build").interactable, Is.False);
        Assert.That(MenuButton("Raze").interactable, Is.False);
    }

    [UnityTest]
    public IEnumerator TowerJourney_ExtraClanUsesDeclaredColorAndNeutralShape()
    {
        yield return PrepareTowerBuilder();
        var site = hero.Tile;
        Assert.That(new BuildTowerCommand(Game.Current.GetSelectedArmies(), site).Execute(),
            Is.EqualTo(Wism.Client.Controllers.ActionState.Succeeded));
        unity.GetComponent<ArmyManager>().RefreshTowerOwnershipVisuals(true);
        var extras = new List<Player>();
        try
        {
            for (int i = 2; i < 10; i++)
            {
                var clan = Clan.Create(new Wism.Client.Modules.Infos.ClanInfo {
                    ShortName = "TowerModClan" + i, DisplayName = "Tower Mod Clan " + i,
                    PrimaryColor = "(37, 149, 211)", SecondaryColor = "(0, 0, 0)"
                });
                var player = Player.Create(clan);
                extras.Add(player);
                Game.Current.Players.Add(player);
            }
            site.Claim(extras.Last());
            unity.GetComponent<ArmyManager>().RefreshTowerOwnershipVisuals();
            var visual = TowerAt(site);
            Assert.That(visual.OwnerClanShortName, Is.EqualTo("TowerModClan9"));
            Assert.That(visual.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
            Assert.That(visual.GetComponent<SpriteRenderer>().color, Is.EqualTo((Color)new Color32(37, 149, 211, 255)));
        }
        finally { foreach (var player in extras) Game.Current.Players.Remove(player); }
    }

    private TowerOwnershipVisual TowerAt(Tile site) => UnityEngine.Object.FindObjectsOfType<TowerOwnershipVisual>()
        .Single(visual => {
            var point = unity.WorldTilemap.ConvertUnityToGameVector(visual.transform.position);
            return point.x == site.X && point.y == site.Y;
        });

    private IEnumerator TowerAnswer(YesNoBox prompt, bool yes)
    {
        var button = prompt.GetComponentsInChildren<Button>().Single(candidate =>
            candidate.GetComponentInChildren<Text>()?.text == (yes ? "Yes" : "No"));
        yield return PressJourneyButton(button);
    }
}
