using System.Collections;
using System.Reflection;
using Assets.Scripts.UI.Panels;
using Assets.Scripts.UnityGame.ModKit;
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
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection, Is.Null);
        Assert.That(FindButton("AdvancedModsButton").GetComponentInChildren<Text>().text, Is.EqualTo("Mods..."));
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

    static Button FindButton(string name)
    {
        var go = GameObject.Find(name);
        Assert.That(go, Is.Not.Null, "Could not find button object: " + name);
        var button = go.GetComponent<Button>();
        Assert.That(button, Is.Not.Null, "Could not find Button on: " + name);
        return button;
    }

    static void Click(Component component)
    {
        var eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
        var pointer = new PointerEventData(eventSystem);
        ExecuteEvents.Execute(component.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
}
