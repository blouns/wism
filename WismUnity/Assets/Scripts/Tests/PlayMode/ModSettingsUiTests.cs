using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
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
    string createdInvalidPackPath;

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
        if (!string.IsNullOrWhiteSpace(createdInvalidPackPath) && Directory.Exists(createdInvalidPackPath))
        {
            Directory.Delete(createdInvalidPackPath, true);
        }

        createdInvalidPackPath = null;
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
        Assert.That(EventSystem.current, Is.Not.Null);
        Assert.That(RectHeight("ActionsRow"), Is.LessThanOrEqualTo(56f));
        Assert.That(RectHeight("BackButton"), Is.LessThanOrEqualTo(48f));
        Assert.That(RectHeight("RefreshButton"), Is.LessThanOrEqualTo(48f));
        Assert.That(RectHeight("ContinueButton"), Is.LessThanOrEqualTo(48f));
        Assert.That(RectHeight("PackScroll"), Is.InRange(96f, 160f));
    }

    [UnityTest]
    public IEnumerator CursorHotspots_DoNotOffsetPointerCursorTargets()
    {
        yield return WaitForModSettings();

        var pointTexture = new Texture2D(9, 22);
        var targetTexture = new Texture2D(32, 32);
        var oversizedPointer = new Texture2D(64, 128);
        try
        {
            Assert.That(CursorManager.CalculateHotspot(pointTexture, CursorManager.HotspotAnchor.UpperLeft), Is.EqualTo(Vector2.zero));
            Assert.That(CursorManager.CalculateHotspot(targetTexture, CursorManager.HotspotAnchor.Center), Is.EqualTo(new Vector2(16f, 16f)));
            Assert.That(CursorManager.CalculatePivot(pointTexture, CursorManager.HotspotAnchor.UpperLeft), Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(CursorManager.CalculatePivot(targetTexture, CursorManager.HotspotAnchor.Center), Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(CursorManager.CalculateViewportScale(360, 0.45f, 1f), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(CursorManager.CalculateViewportScale(1440, 0.45f, 1f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(CursorManager.CalculateOverlaySize(oversizedPointer, CursorManager.HotspotAnchor.UpperLeft, 1f).y, Is.LessThanOrEqualTo(28f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(pointTexture);
            UnityEngine.Object.DestroyImmediate(targetTexture);
            UnityEngine.Object.DestroyImmediate(oversizedPointer);
        }
    }

    [UnityTest]
    public IEnumerator PointerCursorOverlay_UsesTipPivotAndViewportScaledSize()
    {
        yield return WaitForModSettings();

        var cursorObject = new GameObject("CursorManager Test");
        var pointTexture = new Texture2D(9, 22);
        try
        {
            var manager = cursorObject.AddComponent<CursorManager>();
            SetPrivateField(manager, "point", pointTexture);

            manager.PointCursor();
            yield return null;

            var rect = GetPrivateField<RectTransform>(manager, "cursorTransform");
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.pivot, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rect.sizeDelta.y, Is.EqualTo(22f * CursorManager.CalculateViewportScale(Screen.height, 0.45f, 1f)).Within(0.01f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cursorObject);
            UnityEngine.Object.DestroyImmediate(pointTexture);
        }
    }

    [UnityTest]
    public IEnumerator PackRows_UseVisibleRowsAsClickTargets()
    {
        yield return WaitForModSettings();

        var rowImage = GameObject.Find("PackToggle:pack-illurian-legends-flavor").GetComponent<Image>();
        Assert.That(rowImage.raycastTarget, Is.True);

        var viewportImage = GameObject.Find("PackScroll").transform.Find("Viewport").GetComponent<Image>();
        Assert.That(viewportImage.raycastTarget, Is.False);

        var contentFitter = GameObject.Find("PackList").GetComponent<ContentSizeFitter>();
        Assert.That(contentFitter, Is.Not.Null);
        Assert.That(contentFitter.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
    }

    [UnityTest]
    public IEnumerator PackRows_ShowSelectionStateAndColorCodedValidity()
    {
        yield return WaitForModSettings();

        Assert.That(FindPackChildText("pack-illurian-legends-flavor", "PackCheck").text, Is.EqualTo("[x]"));
        Assert.That(FindPackChildText("pack-illurian-legends-flavor", "PackName").text, Does.Contain("Display-name overlay"));
        Assert.That(FindPackChildText("pack-illurian-legends-flavor", "PackState").text, Is.EqualTo("Verified"));
        Assert.That(FindPackChildText("pack-illurian-legends-flavor", "PackState").color.g, Is.GreaterThan(0.30f));

        SetToggle("pack-illurian-legends-flavor", false);
        yield return null;

        Assert.That(FindPackChildText("pack-illurian-legends-flavor", "PackCheck").text, Is.EqualTo("[ ]"));
        Assert.That(FindText("PackHintText").text, Does.Contain("not selected"));

        SetToggle("pack-illurian-legends-flavor", true);
        yield return null;

        Assert.That(FindPackChildText("pack-illurian-legends-flavor", "PackCheck").text, Is.EqualTo("[x]"));
        Assert.That(FindText("PackHintText").text, Does.Contain("selected"));
    }

    [UnityTest]
    public IEnumerator InvalidPack_ShowsReasonInPackStatusBar()
    {
        yield return WaitForModSettings();

        CreateInvalidPack("pack-ui-invalid-test");
        Click(FindButton("RefreshButton"));
        yield return WaitForPackRow("pack-ui-invalid-test");

        Assert.That(FindPackChildText("pack-ui-invalid-test", "PackState").text, Is.EqualTo("Invalid"));
        Assert.That(FindPackChildText("pack-ui-invalid-test", "PackState").color.r, Is.GreaterThan(0.70f));

        SetToggle("pack-ui-invalid-test", true);
        yield return null;

        Assert.That(FindText("PackHintText").text, Does.Contain("Missing schemaVersion and version metadata"));
        Assert.That(FindText("StatusText").color.r, Is.GreaterThan(0.70f));
        Assert.That(FindButton("ContinueButton").interactable, Is.False);
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
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.PackIds, Is.EquivalentTo(new[] { "pack-dusklands-visual", "pack-illurian-legends-flavor" }));
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
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.PackIds, Is.EquivalentTo(new[] { "pack-dusklands-visual", "pack-illurian-legends-flavor" }));
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.ContentFingerprint, Is.Not.Empty);
    }

    [UnityTest]
    public IEnumerator Back_CancelsSelectionAndLoadsGameSetup()
    {
        SetDropdown("ProfileDropdown", "classic-warlords");
        SetDropdown("WorldDropdown", "TestWorld");
        SetToggle("pack-illurian-legends-flavor", true);
        yield return null;
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection, Is.Not.Null);

        Click(FindButton("BackButton"));
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "GameSetup");

        Assert.That(UnityModKitRuntimeSelection.CurrentSelection, Is.Null);
        Assert.That(GameObject.Find("AdvancedModsButton"), Is.Not.Null);
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
    public IEnumerator Refresh_PreservesCurrentSelection()
    {
        SetDropdown("ProfileDropdown", "classic-warlords");
        SetDropdown("WorldDropdown", "TestWorld");
        SetToggle("pack-quick-clash-mode", true);
        yield return null;
        Assert.That(FindToggle("pack-quick-clash-mode").isOn, Is.True);

        Click(FindButton("RefreshButton"));
        yield return null;

        Assert.That(FindToggle("pack-quick-clash-mode").isOn, Is.True);
        Assert.That(FindButton("ContinueButton").interactable, Is.True);
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection, Is.Not.Null);
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.PackIds, Does.Contain("pack-quick-clash-mode"));
    }

    [UnityTest]
    public IEnumerator WorldDetails_ShowPreviewAndMetadata()
    {
        yield return WaitForModSettings();

        Assert.That(GameObject.Find("WorldPreview").GetComponent<RawImage>().texture, Is.Not.Null);
        Assert.That(FindText("WorldDetailText").text, Does.Contain("Cities:"));
    }

    static IEnumerator WaitForModSettings()
    {
        yield return new WaitUntil(() =>
            SceneManager.GetActiveScene().name == "ModSettings" &&
            GameObject.Find("ContinueButton") != null &&
            GameObject.Find("PackToggle:pack-illurian-legends-flavor") != null);
    }

    static IEnumerator WaitForPackRow(string packId)
    {
        yield return new WaitUntil(() => GameObject.Find("PackToggle:" + packId) != null);
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

    static Text FindPackChildText(string packId, string childName)
    {
        var row = GameObject.Find("PackToggle:" + packId);
        Assert.That(row, Is.Not.Null, "Could not find pack row: " + packId);
        var child = row.transform.Find(childName);
        Assert.That(child, Is.Not.Null, $"Could not find {childName} under {packId}.");
        var text = child.GetComponent<Text>();
        Assert.That(text, Is.Not.Null, $"Could not find Text on {childName} under {packId}.");
        return text;
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

    void CreateInvalidPack(string packId)
    {
        createdInvalidPackPath = Path.Combine(UnityModKitSelection.PluginModRoot, "FeaturePacks", packId);
        Directory.CreateDirectory(createdInvalidPackPath);
        File.WriteAllText(
            Path.Combine(createdInvalidPackPath, "pack.json"),
            "{ \"id\": \"" + packId + "\", \"displayName\": \"UI Invalid Test Pack\", \"kind\": \"Flavor\" }");
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
            toggle.isOn = value;
        }

        Assert.That(toggle.isOn, Is.EqualTo(value));
    }

    static void Click(Component component)
    {
        var eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
        var pointer = new PointerEventData(eventSystem);
        ExecuteEvents.Execute(component.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "Could not find field: " + fieldName);
        field.SetValue(target, value);
    }

    static T GetPrivateField<T>(object target, string fieldName) where T : class
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "Could not find field: " + fieldName);
        return field.GetValue(target) as T;
    }
}
