using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.UnityGame.ModKit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

[TestFixture]
public sealed class ModSettingsUiTests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        UnityModKitRuntimeSelection.Clear();
        SceneManager.LoadScene("ModSettings", LoadSceneMode.Single);
        yield return WaitForModSettings();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityModKitRuntimeSelection.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator ModSettings_LoadsAndDiscoversPacks()
    {
        yield return WaitForModSettings();

        Assert.That(FindDropdown("ProfileDropdown").options.Select(option => option.text), Does.Contain("classic-warlords"));
        Assert.That(FindDropdown("WorldDropdown").options.Select(option => option.text), Does.Contain("TestWorld"));
        Assert.That(GameObject.Find("PackToggle:pack-illurian-legends-flavor"), Is.Not.Null);
        Assert.That(GameObject.Find("PackToggle:pack-dusklands-visual"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator ModSettings_HasCameraAndCompactLayout()
    {
        yield return WaitForModSettings();

        Assert.That(Camera.main, Is.Not.Null);
        Assert.That(RectHeight("ActionsRow"), Is.LessThanOrEqualTo(56f));
        Assert.That(RectHeight("BackButton"), Is.LessThanOrEqualTo(48f));
        Assert.That(RectHeight("RefreshButton"), Is.LessThanOrEqualTo(48f));
        Assert.That(RectHeight("ContinueButton"), Is.LessThanOrEqualTo(48f));
        Assert.That(RectHeight("PackScroll"), Is.InRange(96f, 160f));
    }

    [UnityTest]
    public IEnumerator ValidStack_EnablesContinue()
    {
        SetDropdown("ProfileDropdown", "classic-warlords");
        SetDropdown("WorldDropdown", "TestWorld");
        SetToggle("pack-illurian-legends-flavor", true);
        yield return null;

        Assert.That(FindText("StatusText").text, Does.StartWith("Green"));
        Assert.That(FindButton("ContinueButton").interactable, Is.True);
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection, Is.Not.Null);
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.PackIds, Is.EqualTo(new[] { "pack-illurian-legends-flavor" }));
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.ContentFingerprint, Is.Not.Empty);
    }

    [UnityTest]
    public IEnumerator Continue_LocksSelectionAndLoadsGameSetup()
    {
        SetDropdown("ProfileDropdown", "classic-warlords");
        SetDropdown("WorldDropdown", "TestWorld");
        SetToggle("pack-illurian-legends-flavor", true);
        yield return null;

        Click(FindButton("ContinueButton"));
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "GameSetup");

        Assert.That(UnityModKitRuntimeSelection.CurrentSelection, Is.Not.Null);
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.ProfileId, Is.EqualTo("classic-warlords"));
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.World, Is.EqualTo("TestWorld"));
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.PackIds, Is.EqualTo(new[] { "pack-illurian-legends-flavor" }));
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.ContentFingerprint, Is.Not.Empty);
    }

    [UnityTest]
    public IEnumerator MissingScene_DisablesContinue()
    {
        SetDropdown("ProfileDropdown", "classic-warlords");
        SetDropdown("WorldDropdown", "UnitTestWorld");
        yield return null;

        Assert.That(FindText("StatusText").text, Does.StartWith("Red"));
        Assert.That(FindText("DetailText").text, Does.Contain("Scene: Missing"));
        Assert.That(FindButton("ContinueButton").interactable, Is.False);
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection, Is.Null);
    }

    [UnityTest]
    public IEnumerator Refresh_ReevaluatesSelectionWithoutStalePack()
    {
        SetDropdown("ProfileDropdown", "classic-warlords");
        SetDropdown("WorldDropdown", "TestWorld");
        SetToggle("pack-illurian-legends-flavor", true);
        yield return null;
        Assert.That(FindToggle("pack-illurian-legends-flavor").isOn, Is.True);

        Click(FindButton("RefreshButton"));
        yield return null;

        Assert.That(FindToggle("pack-illurian-legends-flavor").isOn, Is.False);
        Assert.That(FindButton("ContinueButton").interactable, Is.True);
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection, Is.Not.Null);
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.PackIds, Is.Empty);
    }

    static IEnumerator WaitForModSettings()
    {
        yield return new WaitUntil(() =>
            SceneManager.GetActiveScene().name == "ModSettings" &&
            GameObject.Find("ContinueButton") != null &&
            GameObject.Find("PackToggle:pack-illurian-legends-flavor") != null);
    }

    static Dropdown FindDropdown(string name)
    {
        return RequireComponent<Dropdown>(name);
    }

    static Button FindButton(string name)
    {
        return RequireComponent<Button>(name);
    }

    static Text FindText(string name)
    {
        return RequireComponent<Text>(name);
    }

    static Toggle FindToggle(string packId)
    {
        return RequireComponent<Toggle>("PackToggle:" + packId);
    }

    static T RequireComponent<T>(string name) where T : Component
    {
        var gameObject = GameObject.Find(name);
        Assert.That(gameObject, Is.Not.Null, "Could not find UI object: " + name);
        var component = gameObject.GetComponent<T>();
        Assert.That(component, Is.Not.Null, "Could not find component " + typeof(T).Name + " on " + name);
        return component;
    }

    static float RectHeight(string name)
    {
        return RequireComponent<RectTransform>(name).rect.height;
    }

    static void SetDropdown(string name, string value)
    {
        var dropdown = FindDropdown(name);
        var index = dropdown.options.FindIndex(option => string.Equals(option.text, value, StringComparison.OrdinalIgnoreCase));
        Assert.That(index, Is.GreaterThanOrEqualTo(0), $"Dropdown {name} did not contain option {value}.");

        Click(dropdown);
        dropdown.value = index;
        dropdown.onValueChanged.Invoke(index);
        dropdown.RefreshShownValue();
    }

    static void SetToggle(string packId, bool value)
    {
        var toggle = FindToggle(packId);
        if (toggle.isOn != value)
        {
            Click(toggle);
        }

        Assert.That(toggle.isOn, Is.EqualTo(value));
    }

    static void Click(Component component)
    {
        var eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
        var pointer = new PointerEventData(eventSystem);
        ExecuteEvents.Execute(component.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
}
