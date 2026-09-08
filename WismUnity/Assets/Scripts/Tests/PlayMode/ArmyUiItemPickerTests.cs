using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

public sealed partial class ArmyUiInputTests
{
    private Artifact GroundItem(string name)
    {
        var item = Artifact.Create(ModFactory.FindArtifactInfo(name));
        hero.Tile.AddItem(item);
        return item;
    }

    private IEnumerator ItemKey(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private IEnumerator SelectItemHero()
    {
        manager.SelectArmies(new List<Army> { hero });
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        unity.InteractiveUI = true;
    }

    [UnityTest]
    public IEnumerator ItemPicker_SingleTakeKey_TakesOnceWithoutDialogAndNamesItem()
    {
        var item = GroundItem("StaffOfMight");
        yield return SelectItemHero();
        int command = manager.ControllerProvider.CommandController.GetLastCommand().Id;
        yield return ItemKey(Key.T);
        yield return WaitFor(() => hero.HasItems() && hero.Items.Contains(item));
        Assert.That(unity.ItemPicker.gameObject.activeSelf, Is.False);
        Assert.That(hero.Tile.HasItems(), Is.False);
        Assert.That(hero.Items.Count(value => value == item), Is.EqualTo(1));
        Assert.That(manager.ControllerProvider.CommandController.GetLastCommand().Id, Is.EqualTo(command + 1));
        Assert.That(GameObject.FindGameObjectWithTag("NotificationBox").GetComponent<Text>().text,
            Is.EqualTo("Taken: Staff of Might"));
        yield return new WaitForSecondsRealtime(0.15f);
        Assert.That(manager.ControllerProvider.CommandController.GetLastCommand().Id, Is.EqualTo(command + 1));
    }

    [UnityTest]
    public IEnumerator ItemPicker_MultipleTake_ChoosesOnlySelectedItemAndCancelIsInert()
    {
        var first = GroundItem("StaffOfMight");
        var second = GroundItem("CrownOfLoriel");
        yield return SelectItemHero();
        var before = State();
        yield return ItemKey(Key.T);
        yield return WaitFor(() => unity.ItemPicker.IsInitialized());
        Assert.That(hero.Tile.Items, Is.EquivalentTo(new[] { first, second }));
        Assert.That(State(), Is.EqualTo(before));
        Assert.That(((RectTransform)unity.ItemPicker.transform.Find("ClassicItemPicker")).rect.height, Is.LessThanOrEqualTo(300),
            "Two items should not occupy a full-height empty list.");
        yield return CaptureClassicPanel(unity.ItemPicker, _ => { }, "item-picker-take");
        yield return ItemKey(Key.Escape);
        yield return WaitFor(() => input.InputMode == InputMode.Game);
        Assert.That(hero.Tile.Items, Is.EquivalentTo(new[] { first, second }));
        yield return ItemKey(Key.T);
        yield return WaitFor(() => unity.ItemPicker.IsInitialized());
        yield return ItemKey(Key.DownArrow);
        Assert.That(unity.ItemPicker.GetSelectedItem(), Is.SameAs(second));
        yield return ItemKey(Key.Enter);
        yield return WaitFor(() => hero.HasItems() && hero.Items.Contains(second));
        Assert.That(hero.Tile.Items, Is.EquivalentTo(new[] { first }));
        Assert.That(hero.Items, Has.No.Member(first));
        Assert.That(unity.ItemPicker.gameObject.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator ItemPicker_DropIsDeliberateAndSecondHeroCanTakeTransfer()
    {
        var item = GroundItem("Firesword");
        hero.Take(item);
        yield return SelectItemHero();
        yield return ItemKey(Key.O);
        yield return WaitFor(() => unity.ItemPicker.IsInitialized());
        Assert.That(hero.Items, Does.Contain(item));
        Assert.That(unity.ItemPicker.GetComponentsInChildren<Button>().Single(button => button.name == "ConfirmItem")
            .GetComponentInChildren<Text>().text, Is.EqualTo("Drop"));
        yield return ItemKey(Key.Escape);
        yield return WaitFor(() => input.InputMode == InputMode.Game);
        Assert.That(hero.Items, Does.Contain(item));
        yield return ItemKey(Key.O);
        yield return WaitFor(() => unity.ItemPicker.IsInitialized());
        var confirm = unity.ItemPicker.GetComponentsInChildren<Button>().Single(button => button.name == "ConfirmItem");
        ExecuteEvents.Execute(confirm.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        yield return WaitFor(() => hero.Tile.Items.Contains(item));
        Assert.That(hero.Items, Has.No.Member(item));
        var firstHero = hero;
        firstHero.Player.HireHero(firstHero.Tile);
        hero = firstHero.Player.GetArmies().OfType<Hero>().First(value => value != firstHero);
        manager.DeselectArmies();
        yield return WaitFor(() => !Game.Current.ArmiesSelected());
        yield return SelectItemHero();
        yield return ItemKey(Key.T);
        yield return WaitFor(() => hero.HasItems() && hero.Items.Contains(item));
        Assert.That(firstHero.Items, Has.No.Member(item));
        Assert.That(hero.Tile.HasItems(), Is.False);
    }

    [UnityTest]
    public IEnumerator ItemPicker_LocationNavigationStillWorksWithoutMovingHero()
    {
        unity.InteractiveUI = true;
        var origin = hero.Tile;
        input.SetInputMode(InputMode.LocationPicker);
        yield return WaitFor(() => unity.ItemPicker.IsInitialized());
        var destination = (Location)unity.ItemPicker.GetSelectedItem();
        Assert.That(destination, Is.Not.Null);
        yield return ItemKey(Key.Enter);
        yield return WaitFor(() => input.InputMode == InputMode.Game);
        Assert.That(hero.Tile, Is.SameAs(origin));
        Assert.That(unity.ItemPicker.gameObject.activeSelf, Is.False);
        Assert.That(GameObject.FindGameObjectWithTag("NotificationBox").GetComponent<Text>().text,
            Is.EqualTo("Going to " + destination.DisplayName));
    }

    [UnityTest]
    public IEnumerator ItemPicker_EmptyTakeAndDrop_ReturnToGameWithoutModal()
    {
        yield return SelectItemHero();
        foreach (var key in new[] { Key.T, Key.O })
        {
            yield return ItemKey(key);
            yield return WaitFor(() => input.InputMode == InputMode.Game);
            Assert.That(unity.ItemPicker.gameObject.activeSelf, Is.False);
        }
    }

    [UnityTest]
    public IEnumerator ItemPicker_ClassicLayoutAndDenseScrolling_DoNotMutateItemsOrOtherContent()
    {
        var choices = new List<MapObject>();
        for (int i = 0; i < 16; i++) choices.Add(GroundItem(i % 2 == 0 ? "StaffOfMight" : "SceptreOfLoriel"));
        choices[15].DisplayName = "The Sceptre of the Ancient Kings of Loriel";
        var otherContent = new GameObject("Content");
        var unrelated = new GameObject("Button1");
        unrelated.transform.SetParent(otherContent.transform);
        yield return SelectItemHero();
        try
        {
            yield return ItemKey(Key.T);
            yield return WaitFor(() => unity.ItemPicker.IsInitialized());
            var picker = unity.ItemPicker;
            var reference = UnityUtilities.GameObjectHardFind("CityProductionPanel")
                .GetComponentsInChildren<Text>(true).First(text => text.font != null).font;
            Assert.That(picker.GetComponentsInChildren<Text>().All(text => text.font == reference), Is.True);
            var rows = picker.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("ItemRow")).ToArray();
            Assert.That(rows.Length, Is.EqualTo(16));
            Assert.That(rows.All(button => ((RectTransform)button.transform).rect.height >= 44), Is.True);
            var before = ((RectTransform)rows[0].transform).rect;
            ExecuteEvents.Execute(rows[1].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            Assert.That(picker.GetSelectedItem(), Is.SameAs(choices[1]));
            Assert.That(((RectTransform)rows[0].transform).rect, Is.EqualTo(before));
            for (int i = 0; i < 14; i++) yield return ItemKey(Key.DownArrow);
            Assert.That(picker.GetSelectedItem(), Is.SameAs(choices[15]));
            Assert.That(picker.GetComponentInChildren<ScrollRect>().content.anchoredPosition.y, Is.GreaterThan(0));
            Assert.That(picker.GetComponentInChildren<ScrollRect>().verticalScrollbar.gameObject.activeSelf, Is.True);
            Canvas.ForceUpdateCanvases();
            var longLabel = rows[15].GetComponentInChildren<Text>();
            Assert.That(longLabel.cachedTextGenerator.characterCountVisible, Is.EqualTo(longLabel.text.Length));
            string path = null;
            yield return CaptureClassicPanel(picker, value => path = value, "item-picker-dense");
            Assert.That(path, Is.Not.Null);
            picker.Cancel();
            yield return WaitFor(() => input.InputMode == InputMode.Game);
            Assert.That(hero.Tile.Items.Cast<MapObject>(), Is.EquivalentTo(choices));
            Assert.That(unrelated, Is.Not.Null);
            Assert.That(otherContent.transform.childCount, Is.EqualTo(1));
            picker.Initialize(unity, choices, "Take an item", "Take", hero.DisplayName);
            picker.Clear();
            picker.Clear();
            Assert.That(choices.Count, Is.EqualTo(16), "The picker must own a copy, never clear the caller's collection.");
        }
        finally { Object.Destroy(otherContent); }
    }
}
