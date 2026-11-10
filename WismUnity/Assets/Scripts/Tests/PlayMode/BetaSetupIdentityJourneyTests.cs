using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts.UnityGame.ModKit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

[TestFixture]
[UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.LinuxEditor, RuntimePlatform.OSXEditor)]
public sealed class BetaSetupIdentityJourneyTests
{
    private Vector2Int originalViewport;

    private static readonly Vector2Int[] Viewports =
    {
        new Vector2Int(1024, 768),
        new Vector2Int(1280, 720),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1080)
    };

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        originalViewport = new Vector2Int(Screen.width, Screen.height);
        UnityModKitRuntimeSelection.Clear();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityModKitRuntimeSelection.Clear();
        if (originalViewport.x > 0 && originalViewport.y > 0)
        {
            yield return SetViewport(originalViewport.x, originalViewport.y);
        }
        yield return null;
    }

    [Test]
    public void ClassicClanPaletteCopies_MatchSpriteEvidence()
    {
        var source = Path.GetFullPath(Path.Combine(
            Application.dataPath, "..", "..", "WismClient", "Wism.Client.Core", "mod", "Clan.json"));
        var copies = new[]
        {
            source,
            Path.Combine(Application.dataPath, "Mod", "Clan.json"),
            Path.Combine(UnityModKitSelection.PluginModRoot, "Clan.json")
        };
        var palettes = copies.Select(path =>
        {
            Assert.That(File.Exists(path), Is.True, "Missing shared clan data: " + path);
            return JArray.Parse(File.ReadAllText(path));
        }).ToArray();

        foreach (var clans in palettes)
        {
            AssertPalette(clans, "HorseLords", "(0, 134, 199)", "(0, 0, 0)");
            AssertPalette(clans, "Selentines", "(199, 199, 0)", "(0, 85, 199)");
        }
    }

    [UnityTest]
    public IEnumerator SetupJourney_HidesValidStatusAndKeepsErrorsAnchoredAtSupportedViewports()
    {
        UnityModKitRuntimeSelection.Clear();
        SceneManager.LoadScene("GameSetup", LoadSceneMode.Single);
        yield return new WaitUntil(() => GameObject.Find("GameSetupValidationText") != null);

        var start = GameObject.Find("StartButton").GetComponent<Button>();
        var validation = GameObject.Find("GameSetupValidationText").GetComponent<Text>();
        var playerOne = GameObject.Find("Player1").GetComponent<Toggle>();
        var playerTwo = GameObject.Find("Player2").GetComponent<Toggle>();
        AssertSetupPalette(
            "Player6", "The Selentines",
            new Color32(199, 199, 0, 255), new Color32(0, 85, 199, 255));
        AssertSetupPalette(
            "Player7", "Horse Lords",
            new Color32(0, 134, 199, 255), new Color32(0, 0, 0, 255));

        foreach (var viewport in Viewports)
        {
            yield return SetViewport(viewport.x, viewport.y);
            Canvas.ForceUpdateCanvases();
            Assert.That(start.interactable, Is.True, "The valid setup must allow Start at " + viewport);
            Assert.That(validation.text, Is.Empty, "A valid setup must not show a Ready status.");

            foreach (var toggle in Enumerable.Range(1, 8)
                .Select(index => GameObject.Find("Player" + index).GetComponent<Toggle>()))
            {
                toggle.isOn = false;
            }
            playerOne.isOn = true;
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(start.interactable, Is.False, "One selected player must disable Start at " + viewport);
            Assert.That(validation.text, Is.EqualTo("Select at least two players."));
            Assert.That(validation.color.r, Is.GreaterThan(validation.color.g), "Validation errors remain red.");
            Assert.That(validation.cachedTextGenerator.characterCountVisible, Is.EqualTo(validation.text.Length),
                "The full validation error must remain readable.");
            var startRect = start.GetComponent<RectTransform>();
            var validationRect = validation.GetComponent<RectTransform>();
            Assert.That(validationRect.transform.parent, Is.EqualTo(startRect.transform.parent));
            Assert.That(validationRect.anchorMin, Is.EqualTo(startRect.anchorMin));
            Assert.That(validationRect.anchorMax, Is.EqualTo(startRect.anchorMax));
            AssertRectOnScreen(validationRect, viewport);
            var validationBounds = TextInkBounds(validation);
            var mods = GameObject.Find("AdvancedModsButton");
            Assert.That(mods, Is.Not.Null);
            foreach (var button in new[] { start, mods.GetComponent<Button>() })
                Assert.That(validationBounds.Overlaps(ScreenBounds((RectTransform)button.transform)), Is.False,
                    "Validation feedback must not cover " + button.name);

            playerTwo.isOn = true;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(start.interactable, Is.True, "A valid roster must re-enable Start at " + viewport);
            Assert.That(validation.text, Is.Empty, "Returning to a valid setup clears the error.");
        }
    }

    [UnityTest]
    public IEnumerator SetupJourney_LongErrorsScrollWithoutCoveringControlsAndRecover()
    {
        SceneManager.LoadScene("GameSetup", LoadSceneMode.Single);
        yield return new WaitUntil(() => GameObject.Find("GameSetupValidationText") != null);
        var setup = UnityEngine.Object.FindFirstObjectByType<GameSetup>();
        var errorField = typeof(GameSetup).GetField("rosterError", BindingFlags.Instance | BindingFlags.NonPublic);
        var refresh = typeof(GameSetup).GetMethod("UpdateStartValidation", BindingFlags.Instance | BindingFlags.NonPublic);
        var start = GameObject.Find("StartButton").GetComponent<Button>();
        var validation = GameObject.Find("GameSetupValidationText").GetComponent<Text>();
        var error = "Could not load clans: " + string.Join("\n", Enumerable.Range(1, 24)
            .Select(i => "Missing starting city for clan " + i + " <unknown> with a long display name."));

        foreach (var viewport in Viewports)
        {
            yield return SetViewport(viewport.x, viewport.y);
            errorField.SetValue(setup, error);
            refresh.Invoke(setup, null);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(start.interactable, Is.False);
            Assert.That(validation.text, Is.EqualTo(error), "Do not truncate diagnostic details.");
            Assert.That(validation.supportRichText, Is.False, "Mod text is literal, not markup.");
            var scroll = validation.GetComponentInParent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null, "Long diagnostics need a bounded scrollable region.");
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(scroll.verticalScrollbar, Is.Not.Null, "Overflow must have a discoverable control.");
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            AssertRectOnScreen(scroll.viewport, viewport);
            var region = ScreenBounds((RectTransform)scroll.transform);
            foreach (var control in new[] { "StartButton", "LoadButton", "AdvancedModsButton", "ShowAiCombatToggle" })
                Assert.That(region.Overlaps(ScreenBounds(GameObject.Find(control).GetComponent<RectTransform>())),
                    Is.False, "Long feedback must not cover " + control);
            scroll.verticalNormalizedPosition = 0f;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(ScreenBounds(scroll.content).yMin,
                Is.EqualTo(ScreenBounds(scroll.viewport).yMin).Within(2f), "The last error line must be reachable.");
            yield return GameSetupModSettingsFlowTests.CaptureSetupCanvas(start.gameObject,
                $"setup-long-error-{viewport.x}x{viewport.y}-camera-projection.png");

            errorField.SetValue(setup, null);
            refresh.Invoke(setup, null);
            yield return null;
            Assert.That(start.interactable, Is.True);
            Assert.That(validation.text, Is.Empty);
            Assert.That(scroll.gameObject.activeSelf, Is.False, "Recovered feedback must not intercept input.");
        }
    }

    private static void AssertPalette(JArray clans, string shortName, string primary, string secondary)
    {
        var clan = clans.OfType<JObject>().Single(item => (string)item["ShortName"] == shortName);
        Assert.That((string)clan["PrimaryColor"], Is.EqualTo(primary), shortName + " primary");
        Assert.That((string)clan["SecondaryColor"], Is.EqualTo(secondary), shortName + " secondary");
    }

    private static void AssertSetupPalette(string rowName, string displayName, Color32 primary, Color32 secondary)
    {
        var labels = GameObject.Find(rowName).GetComponentsInChildren<Text>(true)
            .Where(label => label.text == displayName).ToArray();
        Assert.That(labels.Any(label => ((Color32)label.color).Equals(primary)), Is.True,
            displayName + " setup label must use the shared primary clan color.");
        Assert.That(labels.Any(label => ((Color32)label.color).Equals(secondary)), Is.True,
            displayName + " setup shadow must use the shared secondary clan color.");
    }

    private static void AssertRectOnScreen(RectTransform rect, Vector2Int viewport)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        foreach (var corner in corners)
        {
            Assert.That(corner.x, Is.InRange(0f, viewport.x), "Validation text must stay horizontally on screen.");
            Assert.That(corner.y, Is.InRange(0f, viewport.y), "Validation text must stay vertically on screen.");
        }
    }

    private static Rect ScreenBounds(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners.Min(p => p.x), corners.Min(p => p.y), corners.Max(p => p.x), corners.Max(p => p.y));
    }

    private static Rect TextInkBounds(Text text)
    {
        // The feedback's non-raycast rectangle is wider than its glyphs. Compare
        // visible text, not transparent layout space, against neighboring controls.
        var vertices = text.cachedTextGenerator.verts;
        var points = vertices.Take(Math.Max(0, vertices.Count - 4))
            .Select(vertex => text.rectTransform.TransformPoint(vertex.position / text.pixelsPerUnit)).ToArray();
        Assert.That(points, Is.Not.Empty);
        return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
    }

    private static IEnumerator SetViewport(int width, int height)
    {
        var editorType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("WismUnity.Playground.UnityPlaygroundCli"))
            .FirstOrDefault(type => type != null);
        Assert.That(editorType, Is.Not.Null, "The editor Game View sizing helper is required.");
        var method = editorType.GetMethod("TryApplyEditorGameViewSize", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments = { width, height, "Beta setup identity journey", null };
        Assert.That(method.Invoke(null, arguments), Is.True, arguments[3]?.ToString());
        var deadline = Time.realtimeSinceStartup + 20f;
        while ((Screen.width != width || Screen.height != height) && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
        Assert.That(Screen.width, Is.EqualTo(width));
        Assert.That(Screen.height, Is.EqualTo(height));
    }
}
