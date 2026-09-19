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
using Wism.Client.Core;

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
    public IEnumerator BattlePresentation_GameSetupCheckboxFeedsSettingsWithoutChangingClans()
    {
        UnityModKitRuntimeSelection.Clear();
        SceneManager.LoadScene("GameSetup", LoadSceneMode.Single);
        yield return WaitForGameSetup();
        var toggle = FindToggle("ShowAiCombatToggle");
        Assert.That(toggle.interactable, Is.True);
        Assert.That(toggle.isOn, Is.True);
        Assert.That(toggle.GetComponentInChildren<Text>().text, Is.EqualTo("Show AI combat"));
        var clans = ReadGameSettings().Players.Select(player => player.ClanName).ToArray();
        var interactive = FindToggle("InteractiveToggle");
        var combatBounds = new Vector3[4];
        var interactiveBounds = new Vector3[4];
        ((RectTransform)toggle.transform).GetWorldCorners(combatBounds);
        ((RectTransform)interactive.transform).GetWorldCorners(interactiveBounds);
        Assert.That(combatBounds[1].y, Is.LessThan(interactiveBounds[0].y), "Option rows must not overlap.");
        ExecuteEvents.Execute(toggle.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        yield return null;
        Assert.That(ReadGameSettings().ShowAiCombat, Is.False);
        Assert.That(ReadGameSettings().InteractiveUI, Is.True);
        Assert.That(ReadGameSettings().Players.Select(player => player.ClanName), Is.EqualTo(clans));
        ExecuteEvents.Execute(toggle.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        yield return null;
        Assert.That(ReadGameSettings().ShowAiCombat, Is.True);
        Canvas.ForceUpdateCanvases();
        var label = toggle.GetComponentInChildren<Text>();
        Assert.That(label.cachedTextGenerator.characterCountVisible, Is.EqualTo(label.text.Length), "The entire option label must render.");
        yield return CaptureCombatOption(toggle);
    }

    private static IEnumerator CaptureCombatOption(Toggle toggle)
    {
        var canvas = toggle.GetComponentInParent<Canvas>().rootCanvas;
        var mode = canvas.renderMode;
        var camera = canvas.worldCamera;
        var distance = canvas.planeDistance;
        var captureCamera = Camera.main;
        var target = captureCamera.targetTexture;
        var active = RenderTexture.active;
        var render = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
        var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        var positions = canvas.GetComponentsInChildren<RectTransform>(true)
            .Where(rect => rect != canvas.transform).Select(rect => (rect, position: rect.localPosition)).ToArray();
        try
        {
            // Batch mode has no Game View texture; capture the shipped canvas through its camera.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = captureCamera;
            canvas.planeDistance = captureCamera.nearClipPlane + 1;
            foreach (var item in positions)
                item.rect.localPosition = new Vector3(item.position.x, item.position.y, 0);
            Canvas.ForceUpdateCanvases();
            yield return null;
            captureCamera.targetTexture = render;
            captureCamera.Render();
            RenderTexture.active = render;
            texture.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0);
            texture.Apply();
            var root = System.IO.Path.Combine(Application.dataPath, "../Library/WismUiCaptures");
            System.IO.Directory.CreateDirectory(root);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(root, "show-ai-combat-setup-camera-projection.png"), texture.EncodeToPNG());
        }
        finally
        {
            captureCamera.targetTexture = target;
            RenderTexture.active = active;
            foreach (var item in positions) item.rect.localPosition = item.position;
            canvas.renderMode = mode;
            canvas.worldCamera = camera;
            canvas.planeDistance = distance;
            RenderTexture.ReleaseTemporary(render);
            Object.Destroy(texture);
            Canvas.ForceUpdateCanvases();
        }
    }

    [UnityTest]
    public IEnumerator GameSetup_UnimplementedOptionsAreDisabledAndCannotChangeSettings()
    {
        UnityModKitRuntimeSelection.Clear();
        SceneManager.LoadScene("GameSetup", LoadSceneMode.Single);
        yield return WaitForGameSetup();

        var randomStart = FindToggle("RandomStartToggle");
        var interactive = FindToggle("InteractiveToggle");
        Assert.That(randomStart.interactable, Is.False);
        Assert.That(interactive.interactable, Is.False);

        randomStart.isOn = true;
        interactive.isOn = false;
        yield return null;

        var settings = ReadGameSettings();
        Assert.That(settings.RandomStartLocations, Is.False);
        Assert.That(settings.InteractiveUI, Is.True);
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
        var expectedDifficulties = new AiDifficultyTier?[]
        {
            AiDifficultyTier.Knight,
            AiDifficultyTier.Baron,
            AiDifficultyTier.Lord,
            AiDifficultyTier.Warlord,
            null
        };
        var roleIndex = 0;
        foreach (var expectedRole in expectedRoles)
        {
            Click(FindRoleText("Player1"));
            yield return null;

            Assert.That(FindRoleText("Player1").text.Trim(), Is.EqualTo(expectedRole));
            var selectedPlayer = ReadGameSettings().Players.First(player => player.ClanName == "Sirians");
            Assert.That(selectedPlayer.IsHuman, Is.EqualTo(expectedRole == "Human"));
            Assert.That(selectedPlayer.AiDifficulty, Is.EqualTo(expectedDifficulties[roleIndex++]));
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

