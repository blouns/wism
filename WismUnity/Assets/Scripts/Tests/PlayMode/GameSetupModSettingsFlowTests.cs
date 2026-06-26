using System.Collections;
using System.Linq;
using System.Reflection;
using Assets.Scripts.UI.Panels;
using Assets.Scripts.UnityGame.ModKit;
using Assets.Scripts.UnityGame.Persistance.Entities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;

[TestFixture]
public sealed class GameSetupModSettingsFlowTests
{
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityModKitRuntimeSelection.Clear();
        yield return null;
    }

    [Test]
    public void SplashScreen_DefaultsToStandardGameSetup()
    {
        var go = new GameObject("SplashScreen Test");
        try
        {
            var splash = go.AddComponent<SplashScreen>();
            var field = typeof(SplashScreen).GetField("nextSceneName", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(field, Is.Not.Null);
            Assert.That(field.GetValue(splash), Is.EqualTo("GameSetup"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [UnityTest]
    public IEnumerator GameSetup_ExposesOptionalModSettingsButton()
    {
        UnityModKitRuntimeSelection.Clear();
        SceneManager.LoadScene("GameSetup", LoadSceneMode.Single);
        yield return WaitForGameSetup();

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameSetup"));
        Assert.That(FindButton("AdvancedModsButton").GetComponentInChildren<Text>().text, Does.Contain("M</color>ods..."));
        AssertLabelStackAligned("AdvancedModsButton");
        Assert.That(FindButton("StartButton").GetComponentInChildren<Text>().text, Does.Contain("S</color>tart"));
        Assert.That(GameObject.Find("GameSetupValidationText"), Is.Not.Null);
        Assert.That(FindDropdown("WorldDropdown").options.Select(option => option.text), Does.Contain("Illuria"));
        Assert.That(FindDropdown("WorldDropdown").options[FindDropdown("WorldDropdown").value].text, Is.EqualTo("Illuria"));
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection, Is.Not.Null);
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.ProfileId, Is.EqualTo("classic-warlords"));
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.World, Is.EqualTo("Illuria"));
    }


    [UnityTest]
    public IEnumerator GameSetup_OptionTogglesAreInteractiveAndFeedSettings()
    {
        UnityModKitRuntimeSelection.Clear();
        SceneManager.LoadScene("GameSetup", LoadSceneMode.Single);
        yield return WaitForGameSetup();

        var randomStart = FindToggle("RandomStartToggle");
        var interactive = FindToggle("InteractiveToggle");
        Assert.That(randomStart.interactable, Is.True);
        Assert.That(interactive.interactable, Is.True);

        randomStart.isOn = true;
        interactive.isOn = false;
        yield return null;

        var settings = ReadGameSettings();
        Assert.That(settings.RandomStartLocations, Is.True);
        Assert.That(settings.InteractiveUI, Is.False);
    }

    [UnityTest]
    public IEnumerator GameSetup_DeselectingSiriansDoesNotResetAllClanRows()
    {
        UnityModKitRuntimeSelection.Clear();
        SceneManager.LoadScene("GameSetup", LoadSceneMode.Single);
        yield return WaitForGameSetup();

        var sirians = FindToggle("Player1");
        Assert.That(sirians.isOn, Is.True);

        sirians.isOn = false;
        yield return null;

        Assert.That(sirians.isOn, Is.False);
        Assert.That(ReadGameSettings().Players.Select(player => player.ClanName), Does.Not.Contain("Sirians"));
    }

    [UnityTest]
    public IEnumerator GameSetup_PlayerRoleTextCyclesHumanThroughAiRoles()
    {
        UnityModKitRuntimeSelection.Clear();
        SceneManager.LoadScene("GameSetup", LoadSceneMode.Single);
        yield return WaitForGameSetup();

        var expectedRoles = new[] { "Knight", "Baron", "Lord", "Warlord", "Human" };
        foreach (var expectedRole in expectedRoles)
        {
            Click(FindRoleText("Player1"));
            yield return null;

            Assert.That(FindRoleText("Player1").text.Trim(), Is.EqualTo(expectedRole));
        }

        Click(FindRoleText("Player1"));
        yield return null;

        Assert.That(FindRoleText("Player1").text.Trim(), Is.EqualTo("Knight"));
        Assert.That(ReadGameSettings().Players.First(player => player.ClanName == "Sirians").IsHuman, Is.False);
    }

    [UnityTest]
    public IEnumerator GameSetup_ModSettingsButtonOpensAdvancedSelector()
    {
        UnityModKitRuntimeSelection.Clear();
        SceneManager.LoadScene("GameSetup", LoadSceneMode.Single);
        yield return WaitForGameSetup();

        Click(FindButton("AdvancedModsButton"));
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "ModSettings");

        Assert.That(GameObject.Find("ProfileDropdown"), Is.Not.Null);
        Assert.That(GameObject.Find("ContinueButton"), Is.Not.Null);
    }

    static IEnumerator WaitForGameSetup()
    {
        yield return new WaitUntil(() =>
            SceneManager.GetActiveScene().name == "GameSetup" &&
            GameObject.Find("WorldDropdown") != null &&
            GameObject.Find("StartButton") != null &&
            GameObject.Find("LoadButton") != null &&
            GameObject.Find("AdvancedModsButton") != null);
    }


    static Toggle FindToggle(string name)
    {
        var go = GameObject.Find(name);
        Assert.That(go, Is.Not.Null, "Could not find toggle object: " + name);
        var toggle = go.GetComponent<Toggle>();
        Assert.That(toggle, Is.Not.Null, "Could not find Toggle on: " + name);
        return toggle;
    }

    static Text FindRoleText(string playerRowName)
    {
        var row = GameObject.Find(playerRowName);
        Assert.That(row, Is.Not.Null, "Could not find player row: " + playerRowName);
        var roleText = row.GetComponentsInChildren<Text>(true)
            .FirstOrDefault(text => IsRoleText(text.text));
        Assert.That(roleText, Is.Not.Null, "Could not find role text under: " + playerRowName);
        return roleText;
    }

    static bool IsRoleText(string text)
    {
        var normalized = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        return normalized == "Human" ||
            normalized == "Knight" ||
            normalized == "Baron" ||
            normalized == "Lord" ||
            normalized == "Warlord";
    }

    static UnityNewGameEntity ReadGameSettings()
    {
        var setup = UnityEngine.Object.FindObjectOfType<GameSetup>();
        Assert.That(setup, Is.Not.Null);
        var method = typeof(GameSetup).GetMethod("GetGameSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return (UnityNewGameEntity)method.Invoke(setup, null);
    }

    static void AssertLabelStackAligned(string buttonName)
    {
        var labels = GameObject.Find(buttonName).GetComponentsInChildren<Text>(true);
        Assert.That(labels.Length, Is.GreaterThanOrEqualTo(1));
        if (labels.Length < 2)
        {
            return;
        }

        var reference = labels[0].rectTransform;
        foreach (var label in labels.Skip(1))
        {
            Assert.That(label.rectTransform.anchoredPosition, Is.EqualTo(reference.anchoredPosition));
            Assert.That(label.rectTransform.sizeDelta, Is.EqualTo(reference.sizeDelta));
        }
    }
    static Button FindButton(string name)
    {
        var go = GameObject.Find(name);
        Assert.That(go, Is.Not.Null, "Could not find button object: " + name);
        var button = go.GetComponent<Button>();
        Assert.That(button, Is.Not.Null, "Could not find Button on: " + name);
        return button;
    }

    static Dropdown FindDropdown(string name)
    {
        var go = GameObject.Find(name);
        Assert.That(go, Is.Not.Null, "Could not find dropdown object: " + name);
        var dropdown = go.GetComponent<Dropdown>();
        Assert.That(dropdown, Is.Not.Null, "Could not find Dropdown on: " + name);
        return dropdown;
    }

    static void Click(Component component)
    {
        var eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
        var pointer = new PointerEventData(eventSystem);
        ExecuteEvents.Execute(component.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
}

