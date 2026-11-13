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
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Assets.Scripts.UnityGame.Persistance.Entities;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

public sealed partial class ArmyUiInputTests
{
    [UnityTest] public IEnumerator HeroRecruitment_FirstHeroNamingUsesVisibleSubmit() => FirstHeroNaming(false);
    [UnityTest] public IEnumerator HeroRecruitment_BlankNameCannotSubmitAndRecovers() => FirstHeroNaming(true);

    private IEnumerator FirstHeroNaming(bool exerciseBlank)
    {
        unity.enabled = false;
        input.enabled = false;
        Game.Unload();
        UnityManager.SetNewGameSettings(new UnityNewGameEntity
        {
            InteractiveUI = true, IsNewGame = true, RandomSeed = 1990, WorldName = "TestWorld",
            Players = new[] {
                new UnityPlayerEntity { ClanName = "Sirians", IsHuman = true },
                new UnityPlayerEntity { ClanName = "LordBane", IsHuman = true }
            }
        });
        SceneManager.LoadScene("Scenes/Test/TestWorld");
        yield return WaitFor(() => Game.IsInitialized());
        unity = GameObject.FindGameObjectWithTag("UnityManager").GetComponent<UnityManager>();
        manager = unity.GetComponent<GameManager>();
        input = unity.GetComponent<InputManager>();
        manager.StandardTime = 0f;
        manager.WarTime = 0f;
        var naming = UnityUtilities.GameObjectHardFind("SolicitInputPanel").GetComponent<SolicitInput>();
        yield return WaitFor(() => naming.IsInitialized());
        ApplyViewport(1280, 720);
        yield return null;
        var player = Game.Current.GetCurrentPlayer();
        var gold = player.Gold;
        Assert.That(player.GetArmies().OfType<Hero>(), Is.Empty);
        Assert.That(input.InputMode, Is.EqualTo(InputMode.UI));
        var field = naming.GetComponentInChildren<InputField>(true);
        var submit = naming.GetComponentsInChildren<Button>(true).Single(button => button.name == "EnterButton");
        if (exerciseBlank)
        {
            field.text = "   ";
            yield return null;
            Assert.That(submit.interactable, Is.False);
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, submit.transform.position));
            Assert.That(player.GetArmies().OfType<Hero>(), Is.Empty);
            Assert.That(input.InputMode, Is.EqualTo(InputMode.UI));
        }
        field.text = "Ariadne";
        yield return null;
        Assert.That(submit.interactable, Is.True);
        var group = naming.GetComponent<CanvasGroup>();
        Assert.That(group.alpha, Is.EqualTo(1));
        Assert.That(group.interactable && group.blocksRaycasts, Is.True);
        yield return Click(RectTransformUtility.WorldToScreenPoint(null, submit.transform.position));
        yield return WaitFor(() => player.GetArmies().OfType<Hero>().Count() == 1);
        hero = player.GetArmies().OfType<Hero>().Single();
        Assert.That(hero.DisplayName, Is.EqualTo("Ariadne"));
        Assert.That(hero.Tile, Is.SameAs(player.Capitol.Tile));
        Assert.That(player.Gold, Is.EqualTo(gold), "The initial hero is free.");
        Assert.That(group.alpha, Is.EqualTo(0));
        Assert.That(group.blocksRaycasts, Is.False);
        yield return null;
        Assert.That(player.GetArmies().OfType<Hero>().Count(), Is.EqualTo(1));
    }

    [UnityTest] public IEnumerator HeroControls_1024() => HeroControlJourney(1024, 768);
    [UnityTest] public IEnumerator HeroControls_1280() => HeroControlJourney(1280, 720);
    [UnityTest] public IEnumerator HeroControls_1920() => HeroControlJourney(1920, 1080);
    [UnityTest] public IEnumerator HeroControls_Ultrawide() => HeroControlJourney(2560, 1080);

    private IEnumerator HeroControlJourney(int width, int height)
    {
        int oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            ApplyViewport(width, height);
            yield return WaitFor(() => Screen.width == width && Screen.height == height);
            yield return SelectItemHero();
            yield return new WaitForLastCommand(manager.ControllerProvider);
            var item = GroundItem("Firesword");
            hero.Take(item);
            var before = TerminalState();
            int last = unity.LastCommandId;
            var focus = EventSystem.current.currentSelectedGameObject;
            yield return ItemKey(Key.I);
            var panel = unity.GameMenu.HeroControls;
            Assert.That(panel.gameObject.activeInHierarchy, Is.True);
            Assert.That(panel.SelectedHero, Is.SameAs(hero));
            Assert.That(HeroText("InventoryItem0").text, Does.Contain(item.DisplayName));
            Assert.That(HeroText("InventoryItem0").text, Does.Contain("Combat +" + item.CombatBonus));
            Assert.That(input.InputMode, Is.EqualTo(InputMode.UI));
            Canvas.ForceUpdateCanvases();
            foreach (var button in panel.GetComponentsInChildren<Button>().Where(button => button.name != "GoToHero"))
            {
                var corners = new Vector3[4];
                ((RectTransform)button.transform).GetWorldCorners(corners);
                Assert.That(corners.All(point => point.x >= 0 && point.y >= 0 && point.x <= Screen.width && point.y <= Screen.height), Is.True, button.name);
            }
            string path = null;
            yield return CaptureClassicPanel(panel, value => path = value, "hero-inventory-" + width);
            AssertProductionTextRendered(HeroText("HeroHeading"), path);
            AssertProductionTextRendered(HeroText("InventoryItem0"), path);
            yield return ItemKey(Key.Escape);
            Assert.That(unity.GameMenu.IsOpen, Is.False);
            Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(focus));
            yield return Click(MenuPoint("OpenViewMenu"));
            yield return Click(MenuPoint("FindHeroes"));
            Assert.That(panel.gameObject.activeInHierarchy, Is.True);
            Assert.That(panel.Heroes, Is.EquivalentTo(new[] { hero }));
            yield return Click(MenuPoint("GoToHero"));
            Assert.That(unity.GameMenu.IsOpen, Is.False);
            Assert.That(Game.Current.GetSelectedArmies(), Is.EquivalentTo(new[] { hero }));
            Assert.That(TerminalState(), Is.EqualTo(before));
            Assert.That(unity.LastCommandId, Is.EqualTo(last));
            Assert.That(hero.Items, Is.EquivalentTo(new[] { item }));
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(TerminalState(), Is.EqualTo(before), "Closing the panel must not leak a delayed map click.");
        }
        finally { ApplyViewport(oldWidth, oldHeight); }
    }

    [UnityTest]
    public IEnumerator HeroControls_FindRemoteHeroMovesCameraOnlyAndKeepsSelection()
    {
        yield return SelectItemHero();
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var remote = hero.Player.HireHero(World.Current.Map[6, 3]);
        var before = TerminalState();
        int last = unity.LastCommandId;
        yield return ItemKey(Key.K);
        var panel = unity.GameMenu.HeroControls;
        int index = panel.Heroes.ToList().IndexOf(remote);
        yield return Click(MenuPoint("HeroRow" + index));
        var camera = Camera.main.GetComponent<CameraFollow>();
        camera.target = null;
        camera.transform.position = new Vector3(-50, -50, -10);
        var destination = unity.WorldTilemap.ConvertGameToUnityVector(remote.Tile.X, remote.Tile.Y);
        var expected = new Vector3(Mathf.Clamp(destination.x, camera.xMinClamp, camera.xMaxClamp),
            Mathf.Clamp(destination.y, camera.yMinClamp, camera.yMaxClamp), -10);
        yield return Click(MenuPoint("GoToHero"));
        Assert.That(Vector3.Distance(camera.transform.position, expected), Is.LessThan(.01f));
        Assert.That(Game.Current.GetSelectedArmies(), Is.EquivalentTo(new[] { hero }));
        Assert.That(TerminalState(), Is.EqualTo(before));
        Assert.That(unity.LastCommandId, Is.EqualTo(last));
    }

    [UnityTest]
    public IEnumerator HeroControls_EmptyAndMixedInventoryUsesOnlySelectedOwnedHeroes()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.InteractiveUI = true;
        yield return ItemKey(Key.I);
        Assert.That(unity.GameMenu.HeroControls.Heroes, Is.Empty);
        Assert.That(HeroText("NoHeroes").text, Is.EqualTo("No selected heroes"));
        yield return ItemKey(Key.Escape);
        var ordinary = hero.Player.ConscriptArmy(Wism.Client.Modules.Infos.ArmyInfo.GetArmyInfo("LightInfantry"), hero.Tile);
        var other = hero.Player.HireHero(hero.Tile);
        var enemy = Game.Current.Players[1].HireHero(World.Current.Map[7, 4]);
        Assert.That(other, Is.Not.SameAs(hero));
        manager.SelectArmies(new List<Army> { hero, other, ordinary });
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var before = TerminalState();
        yield return Click(MenuPoint("OpenViewMenu"));
        yield return Click(MenuPoint("Inventory"));
        var panel = unity.GameMenu.HeroControls;
        Assert.That(panel.Heroes, Is.EquivalentTo(new[] { hero, other }));
        Assert.That(panel.Heroes, Has.No.Member(enemy));
        yield return Click(MenuPoint("HeroRow1"));
        Assert.That(panel.SelectedHero, Is.SameAs(panel.Heroes[1]));
        Assert.That(HeroText("InventoryItem0").text, Is.EqualTo("No artifacts"));
        yield return Click(MenuPoint("CloseHeroes"));
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator HeroControls_LongRosterAndInventoryScrollWithoutMutation()
    {
        yield return SelectItemHero();
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var player = hero.Player;
        for (int i = 0; i < 9; i++)
        {
            var extra = player.HireHero(hero.Tile);
            extra.DisplayName = "Hero " + i.ToString("D2") + " of the Northern Marches";
        }
        for (int i = 0; i < 18; i++)
        {
            var item = Artifact.Create(new Wism.Client.Modules.Infos.ArtifactInfo {
                ShortName = "ProofArtifact" + i, DisplayName = "Relic " + i + " of the Northern Marches", CombatBonus = i
            });
            hero.Tile.AddItem(item);
            hero.Take(item);
        }
        var before = TerminalState();
        yield return ItemKey(Key.I);
        var panel = unity.GameMenu.HeroControls;
        var items = panel.GetComponentsInChildren<ScrollRect>().Single(scroll => scroll.name == "HeroInventory");
        Assert.That(items.content.rect.height, Is.GreaterThan(items.viewport.rect.height));
        items.verticalNormalizedPosition = 0;
        yield return null;
        Assert.That(items.content.anchoredPosition.y, Is.GreaterThan(0));
        yield return ItemKey(Key.Escape);
        yield return ItemKey(Key.K);
        var roster = panel.GetComponentsInChildren<ScrollRect>().Single(scroll => scroll.name == "HeroRoster");
        Assert.That(panel.Heroes, Has.Count.EqualTo(10));
        roster.verticalNormalizedPosition = 0;
        yield return null;
        yield return Click(MenuPoint("HeroRow9"));
        Assert.That(panel.SelectedHero, Is.SameAs(panel.Heroes[9]));
        yield return ItemKey(Key.Escape);
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator HeroControls_FindHasEmptyStateAndExcludesDeadAndForeignHeroes()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.InteractiveUI = true;
        var enemy = Game.Current.Players[1].HireHero(World.Current.Map[7, 4]);
        hero.Kill();
        yield return ItemKey(Key.K);
        Assert.That(unity.GameMenu.HeroControls.Heroes, Is.Empty);
        Assert.That(HeroText("NoHeroes").text, Is.EqualTo("No living heroes"));
        Assert.That(MenuButton("GoToHero").interactable, Is.False);
        yield return ItemKey(Key.Escape);
        Assert.That(enemy.IsDead, Is.False);
    }

    [UnityTest]
    public IEnumerator HeroControls_UnavailableDuringAiAndPendingTurnsButReadableAfterGameOver()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.InteractiveUI = true;
        Game.Current.GetCurrentPlayer().IsHuman = false;
        unity.GameMenu.OpenHeroes(true);
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Game.Current.GetCurrentPlayer().IsHuman = true;
        input.TryBeginEndTurn();
        unity.GameMenu.OpenHeroes(true);
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        input.CompleteEndTurn();
        input.SetInputMode(InputMode.Game);
        Game.Current.Transition(GameState.GameOver);
        unity.GameMenu.OpenHeroes(true);
        Assert.That(unity.GameMenu.HeroControls.gameObject.activeInHierarchy, Is.True);
        unity.GameMenu.Close();
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
    }

    [UnityTest]
    public IEnumerator HeroControls_ActiveDialogIsNotReplacedAndResetClosesPanel()
    {
        yield return SelectItemHero();
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.GameMenu.RequestExit();
        unity.GameMenu.OpenHeroes(true);
        Assert.That(unity.GameMenu.IsConfirmingExit, Is.True);
        Assert.That(unity.GameMenu.HeroControls.gameObject.activeInHierarchy, Is.False);
        unity.GameMenu.Cancel();
        unity.GameMenu.Close();
        yield return ItemKey(Key.K);
        unity.GameMenu.ResetForGame();
        Assert.That(unity.GameMenu.HeroControls.gameObject.activeInHierarchy, Is.False);
    }

    private Text HeroText(string name) => unity.GameMenu.HeroControls.GetComponentsInChildren<Text>(true)
        .Single(text => text.name == name);
}
