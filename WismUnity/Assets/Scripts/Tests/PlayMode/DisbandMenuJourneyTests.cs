using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using Assets.Tests.PlayMode;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Commands.Armies;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;
using Newtonsoft.Json;

public sealed partial class ArmyUiInputTests
{
    [UnityTest] public IEnumerator DisbandMenu_1024() => DisbandJourney(1024, 768);
    [UnityTest] public IEnumerator DisbandMenu_1280() => DisbandJourney(1280, 720);
    [UnityTest] public IEnumerator DisbandMenu_1920() => DisbandJourney(1920, 1080);
    [UnityTest] public IEnumerator DisbandMenu_Ultrawide() => DisbandJourney(2560, 1080);

    private IEnumerator DisbandJourney(int width, int height)
    {
        int oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            AssertBatchWindowless();
            ApplyViewport(width, height);
            yield return WaitFor(() => Screen.width == width && Screen.height == height);
            yield return SelectItemHero();
            var ordinary = hero.Player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), hero.Tile);
            var item = GroundItem("Firesword");
            hero.Take(item);
            manager.SelectArmies(new List<Army> { hero, ordinary });
            yield return new WaitForLastCommand(manager.ControllerProvider);
            var before = TerminalState();
            int last = unity.LastCommandId;
            Assert.That(MenuButton("OpenReports").gameObject.activeInHierarchy, Is.False);
            yield return Click(MenuPoint("OpenGameMenu"));
            Assert.That(MenuButton("OpenOrdersMenu").gameObject.activeInHierarchy, Is.True);
            yield return Click(MenuPoint("OpenOrdersMenu"));
            yield return Click(MenuPoint("Disband"));
            Assert.That(unity.GameMenu.IsConfirmingDisband, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("CancelDisband"));
            var text = DisbandText("DisbandDetails");
            Assert.That(text.text, Does.Contain(ordinary.DisplayName).And.Contain(hero.DisplayName));
            Assert.That(DisbandText("DisbandTitle").text, Does.Contain("1"));
            Assert.That(TerminalState(), Is.EqualTo(before));
            foreach (var name in new[] { "CancelDisband", "ConfirmDisband" })
            {
                var corners = new Vector3[4];
                ((RectTransform)MenuButton(name).transform).GetWorldCorners(corners);
                Assert.That(corners.All(p => p.x >= 0 && p.y >= 0 && p.x <= Screen.width && p.y <= Screen.height), Is.True, name);
            }
            string capture = null;
            yield return CaptureClassicPanel(DisbandText("DisbandTitle"), path => capture = path, "disband-" + width);
            AssertProductionTextRendered(text, capture);
            yield return Click(MenuPoint("CancelDisband"));
            Assert.That(unity.GameMenu.IsOpen, Is.False);
            Assert.That(TerminalState(), Is.EqualTo(before));
            Assert.That(unity.LastCommandId, Is.EqualTo(last));
            yield return ItemKey(Key.Delete);
            Assert.That(unity.GameMenu.IsConfirmingDisband, Is.True);
            yield return ItemKey(Key.Escape);
            Assert.That(TerminalState(), Is.EqualTo(before));
            yield return ItemKey(Key.Delete);
            var upkeep = hero.Player.GetUpkeep();
            yield return Click(MenuPoint("ConfirmDisband"));
            unity.GameMenu.ConfirmDisband();
            yield return new WaitForLastCommand(manager.ControllerProvider);
            yield return new WaitForFixedUpdate();
            Assert.That(ordinary.IsDead, Is.True);
            Assert.That(hero.IsDead, Is.False);
            Assert.That(hero.Items, Does.Contain(item));
            Assert.That(hero.Player.GetUpkeep(), Is.EqualTo(upkeep - ordinary.Upkeep));
            Assert.That(Game.Current.GetSelectedArmies(), Is.EquivalentTo(new[] { hero }));
            Assert.That(manager.ControllerProvider.CommandController.GetCommands().OfType<DisbandArmiesCommand>().Count(), Is.EqualTo(1));
            var armies = unity.GetComponentInChildren<ArmyManager>();
            Assert.That(armies.ArmyDictionary.ContainsKey(ordinary.Id), Is.False);
            var snapshot = JsonConvert.DeserializeObject<GameEntity>(JsonConvert.SerializeObject(Game.Current.Snapshot()));
            GameFactory.Load(snapshot);
            Assert.That(Game.Current.GetCurrentPlayer().GetArmies().Any(a => a.Id == ordinary.Id), Is.False);
            Assert.That(Game.Current.GetCurrentPlayer().GetArmies().OfType<Hero>().Single(h => h.Id == hero.Id).Items.Any(a => a.ShortName == item.ShortName), Is.True);
        }
        finally { ApplyViewport(oldWidth, oldHeight); }
    }

    [UnityTest]
    public IEnumerator DisbandMenu_KeyboardMenuAndBackdropCloseDoNotMutate()
    {
        yield return SelectItemHero();
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var before = TerminalState();
        yield return ItemKey(Key.F10);
        Assert.That(unity.GameMenu.IsOpen, Is.True);
        Assert.That(MenuButton("OpenReports").gameObject.activeInHierarchy, Is.True);
        yield return Click(new Vector2(Screen.width * .7f, Screen.height * .3f));
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Assert.That(MenuButton("OpenViewMenu").gameObject.activeInHierarchy, Is.False);
        yield return new WaitForSecondsRealtime(.15f);
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator DisbandMenu_HeroOnlyPendingAiDialogAndTerminalAreProtected()
    {
        yield return SelectItemHero();
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.GameMenu.RequestDisband();
        Assert.That(unity.GameMenu.IsOpen, Is.False, "Heroes alone cannot be dismissed.");
        var ordinary = hero.Player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), hero.Tile);
        manager.SelectArmies(new List<Army> { hero, ordinary });
        yield return new WaitForLastCommand(manager.ControllerProvider);
        input.TryBeginEndTurn();
        unity.GameMenu.RequestDisband();
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        input.CompleteEndTurn();
        input.SetInputMode(InputMode.Game);
        hero.Player.IsHuman = false;
        unity.GameMenu.RequestDisband();
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        hero.Player.IsHuman = true;
        unity.GameMenu.RequestExit();
        unity.GameMenu.RequestDisband();
        Assert.That(unity.GameMenu.IsConfirmingExit, Is.True);
        unity.GameMenu.Cancel();
        unity.GameMenu.Close();
        unity.GameMenu.RequestDisband();
        Assert.That(unity.GameMenu.IsConfirmingDisband, Is.True);
        unity.GameMenu.OpenHeroes(true);
        unity.GameMenu.OpenReports();
        unity.GameMenu.RequestExit();
        Assert.That(unity.GameMenu.IsConfirmingDisband, Is.True);
        unity.GameMenu.Close();
        Game.Current.Transition(GameState.GameOver);
        unity.GameMenu.RequestDisband();
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Assert.That(ordinary.IsDead, Is.False);
    }

    [UnityTest]
    public IEnumerator DisbandMenu_StaleSelectionCannotRemoveThePreviousStack()
    {
        yield return SelectItemHero();
        var ordinary = hero.Player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), hero.Tile);
        manager.SelectArmies(new List<Army> { hero, ordinary });
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.GameMenu.RequestDisband();
        Assert.That(unity.GameMenu.IsConfirmingDisband, Is.True);
        Game.Current.DeselectArmies();
        var before = TerminalState();
        unity.GameMenu.ConfirmDisband();
        yield return new WaitForFixedUpdate();
        Assert.That(TerminalState(), Is.EqualTo(before));
        Assert.That(ordinary.IsDead, Is.False);
    }

    private Text DisbandText(string name) => unity.GameMenu.GetComponentsInChildren<Text>(true).Single(t => t.name == name);

    [UnityTest]
    public IEnumerator DisbandMenu_DenseLongNamesScrollAndCancelWithoutMutation()
    {
        yield return SelectItemHero();
        var selected = new List<Army> { hero };
        for (int i = 0; i < 7; i++)
        {
            var army = hero.Player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), hero.Tile);
            army.DisplayName = "Northern heavy infantry regiment of the distant marches " + i;
            selected.Add(army);
        }
        manager.SelectArmies(selected);
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var before = TerminalState();
        yield return ItemKey(Key.Delete);
        Assert.That(DisbandText("DisbandTitle").text, Does.Contain("7 armies"));
        var scroll = unity.GameMenu.GetComponentsInChildren<ScrollRect>().Single(s => s.name == "DisbandList");
        Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
        scroll.verticalNormalizedPosition = 0;
        yield return null;
        Assert.That(scroll.content.anchoredPosition.y, Is.GreaterThan(0));
        Assert.That(DisbandText("DisbandDetails").text, Does.Contain(selected.Last().DisplayName));
        yield return Click(MenuPoint("CancelDisband"));
        Assert.That(TerminalState(), Is.EqualTo(before));
    }
}
