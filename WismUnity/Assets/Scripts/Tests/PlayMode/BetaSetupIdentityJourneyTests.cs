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

            playerTwo.isOn = true;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(start.interactable, Is.True, "A valid roster must re-enable Start at " + viewport);
            Assert.That(validation.text, Is.Empty, "Returning to a valid setup clears the error.");
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
