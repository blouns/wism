using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using Assets.Scripts.UnityGame.ModKit;
using Assets.Scripts.UnityGame.Persistance.Entities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.Modules;
using Assets.Scripts.Tests.PlayMode.Common;
using Newtonsoft.Json.Linq;

[Category("SettingsE2E")]
public sealed class GameSetupPointerMatrixTests
{
    private Mouse mouse;
    private Keyboard keyboard;
    private Touchscreen touch;
    private InputSettings originalSettings;
    private InputSettings testSettings;
    private readonly string[] roles = { "Human", "Knight", "Baron", "Lord", "Warlord" };
    private readonly AiDifficultyTier?[] difficulties = { null, AiDifficultyTier.Knight, AiDifficultyTier.Baron, AiDifficultyTier.Lord, AiDifficultyTier.Warlord };

    [UnitySetUp]
    public IEnumerator OpenSettings()
    {
        AssertWindowless();
        Game.Unload();
        UnityManager.SetNewGameSettings(null);
        UnityModKitRuntimeSelection.Clear();
        ModFactory.ModPath = GameManager.DefaultModPath;
        ModFactory.ResetCache();
        originalSettings = InputSystem.settings;
        testSettings = UnityEngine.Object.Instantiate(originalSettings);
        testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = testSettings;
        mouse = InputSystem.AddDevice<Mouse>("SettingsProofMouse");
        keyboard = InputSystem.AddDevice<Keyboard>("SettingsProofKeyboard");
        touch = InputSystem.AddDevice<Touchscreen>("SettingsProofTouch");
        SceneManager.LoadScene("GameSetup");
        yield return WaitFor(() => GameObject.Find("ShowAiCombatToggle") != null, "Settings startup");
        Canvas.ForceUpdateCanvases();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        AssertWindowless();
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
        if (originalSettings != null) InputSystem.settings = originalSettings;
        if (testSettings != null) UnityEngine.Object.Destroy(testSettings);
        UnityManager.SetNewGameSettings(null);
        UnityModKitRuntimeSelection.Clear();
        var previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("SettingsProofCleanup"));
        if (previous.IsValid() && previous.isLoaded) yield return SceneManager.UnloadSceneAsync(previous);
        Game.Unload();
        yield return null;
    }

    [UnityTest]
    public IEnumerator AuthoredGeometry_RoutesEveryControlToItsOwnHandler()
    {
        foreach (var name in new[] { "StartButton", "LoadButton", "AdvancedModsButton", "WorldDropdown", "ShowAiCombatToggle" })
            AssertHandler(GameObject.Find(name).GetComponent<RectTransform>(), GameObject.Find(name));
        for (int i = 1; i <= 8; i++)
        {
            var row = GameObject.Find("Player" + i);
            AssertHandler(row.transform.Find("Background").GetComponent<RectTransform>(), row);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator CombatCheckbox_ChangesOnlyCombat_AndKeyboardCanRestoreIt()
    {
        var before = Settings();
        var toggle = Find<Toggle>("ShowAiCombatToggle");
        yield return Click(toggle.transform.Find("Background").GetComponent<RectTransform>());
        Assert.That(toggle.isOn, Is.False, "A real pointer press must toggle the combat option.");
        Assert.That(Settings().Players.Select(Identity), Is.EqualTo(before.Players.Select(Identity)), "Combat clicks must never cycle an adjacent player role.");
        Assert.That(Settings().ShowAiCombat, Is.False);
        yield return KeyPress(Key.Enter);
        Assert.That(toggle.isOn, Is.True, "Pointer selection must support keyboard submission.");
    }

    private static string Identity(UnityPlayerEntity player) => player.ClanName + ":" + player.IsHuman + ":" + player.AiDifficulty;

    [UnityTest] public IEnumerator Qualification_ScaleAndTouch_1024x768() => QualifyScaledControls(new Vector2Int(1024, 768));
    [UnityTest] public IEnumerator Qualification_ScaleAndTouch_1280x720() => QualifyScaledControls(new Vector2Int(1280, 720));
    [UnityTest] public IEnumerator Qualification_ScaleAndTouch_1920x1080() => QualifyScaledControls(new Vector2Int(1920, 1080));
    [UnityTest] public IEnumerator Qualification_ScaleAndTouch_2560x1080() => QualifyScaledControls(new Vector2Int(2560, 1080));

    private IEnumerator QualifyScaledControls(Vector2Int viewport)
    {
        var scaler = RoleIcon(1).GetComponentInParent<Canvas>().rootCanvas.GetComponent<CanvasScaler>();
        var reference = scaler.referenceResolution;
        try
        {
            yield return SetViewport(viewport.x, viewport.y);
            foreach (float scale in new[] { 1f, 1.25f, 1.5f })
            {
                // Exercise the real layout with an increased logical UI scale, not a synthetic canvas.
                scaler.referenceResolution = reference / scale;
                yield return null;
                Canvas.ForceUpdateCanvases();
                var before = Settings().Players.Select(Identity).ToArray();
                for (int row = 1; row <= 8; row++)
                {
                    AssertClanLabelFits(row);
                    var checkbox = Checkbox("Player" + row);
                    AssertOnScreen(checkbox, viewport, scale);
                    AssertOnScreen(RoleIcon(row), viewport, scale);
                    yield return TouchPoint(Point(checkbox));
                    Assert.That(Find<Toggle>("Player" + row).isOn, Is.False, $"Touch checkbox {row}, {viewport}, {scale}");
                    yield return Click(checkbox);
                    yield return TouchPoint(Point(RoleIcon(row)));
                    Assert.That(RoleText(row).text, Is.EqualTo("Knight"));
                    for (int role = 0; role < 4; role++) yield return Click(RoleIcon(row));
                }
                foreach (var name in new[] { "StartButton", "LoadButton", "AdvancedModsButton", "WorldDropdown", "ShowAiCombatToggle" })
                {
                    var rect = Find<RectTransform>(name);
                    AssertOnScreen(rect, viewport, scale);
                    AssertHandler(rect, rect.gameObject);
                }
                Assert.That(Settings().Players.Select(Identity), Is.EqualTo(before));
                Assert.That(Game.IsInitialized(), Is.False, "Settings gestures must not initialize gameplay.");
                if (scale == 1.5f)
                    yield return GameSetupModSettingsFlowTests.CaptureSetupCanvas(RoleIcon(1).gameObject,
                        $"settings-{viewport.x}x{viewport.y}-scale150-camera-projection.png");
            }
        }
        finally { scaler.referenceResolution = reference; }
    }

    [UnityTest]
    public IEnumerator Qualification_TouchPagedRosterAndDisabledActions()
    {
        using (var fixture = new ClanRosterFixture(12))
        {
            yield return ModsRoundTrip();
            var before = Settings().Players.Select(Identity).ToArray();
            yield return TouchPoint(Point(Find<RectTransform>("PreviousClans")));
            Assert.That(Find<Text>("ClanPageLabel").text, Is.EqualTo("1-7 / 12"));
            yield return TouchPoint(Point(Find<RectTransform>("NextClans")));
            Assert.That(Find<Text>("ClanPageLabel").text, Is.EqualTo("8-12 / 12"));
            yield return TouchPoint(Point(Find<RectTransform>("NextClans")));
            Assert.That(Settings().Players.Select(Identity), Is.EqualTo(before), "Paging and rejected gestures cannot alter choices.");
            yield return TouchPoint(Point(RoleIcon(5)));
            Assert.That(Settings().Players[11].AiDifficulty, Is.EqualTo(AiDifficultyTier.Knight));
            Assert.That(Settings().Players.Take(11).Select(Identity), Is.EqualTo(before.Take(11)));
        }
    }

    [UnityTest]
    public IEnumerator Qualification_KeyboardSubmit_ReturnAndNumpadEnter()
    {
        foreach (var key in new[] { Key.Enter, Key.NumpadEnter })
        {
            yield return Click(RoleIcon(1));
            var selected = EventSystem.current.currentSelectedGameObject;
            Assert.That(selected, Is.EqualTo(RoleIcon(1).gameObject));
            var before = Settings().Players.Select(Identity).ToArray();
            var label = RoleText(1).text;
            yield return KeyPress(key);
            Assert.That(RoleText(1).text, Is.EqualTo(roles[(Array.IndexOf(roles, label) + 1) % roles.Length]), key.ToString());
            Assert.That(Settings().Players.Skip(1).Select(Identity), Is.EqualTo(before.Skip(1)));
            Assert.That(Game.IsInitialized(), Is.False);
        }
    }

    [UnityTest]
    public IEnumerator Qualification_OneHundredInteractions_ImmediateFeedbackAndLatency()
    {
        var samples = new List<double>();
        var toggle = Find<Toggle>("ShowAiCombatToggle");
        var before = Settings().Players.Select(Identity).ToArray();
        var point = Point(Checkbox("ShowAiCombatToggle"));
        for (int i = -10; i < 100; i++)
        {
            bool expected = !toggle.isOn;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            Assert.That(toggle.isOn, Is.Not.EqualTo(expected), "Press alone must not commit a click.");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int frame = Time.frameCount;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            watch.Stop();
            Assert.That(toggle.isOn, Is.EqualTo(expected), "Feedback must be visible in the next frame.");
            Assert.That(Time.frameCount - frame, Is.LessThanOrEqualTo(1));
            if (i >= 0) samples.Add(watch.Elapsed.TotalMilliseconds);
        }
        Assert.That(Settings().Players.Select(Identity), Is.EqualTo(before));
        Assert.That(Game.IsInitialized(), Is.False);
        var ordered = samples.OrderBy(value => value).ToArray();
        var p95 = ordered[(int)Math.Ceiling(ordered.Length * .95) - 1];
        var folder = Path.Combine(Application.dataPath, "../Library/WismUiCaptures");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "settings-interaction-latency.json"), new JObject
        {
            ["kind"] = "synthetic-input-to-next-frame", ["samples"] = samples.Count,
            ["warmup"] = 10, ["p95Milliseconds"] = p95,
            ["viewport"] = $"{Screen.width}x{Screen.height}", ["milliseconds"] = new JArray(samples)
        }.ToString());
        Assert.That(p95, Is.LessThanOrEqualTo(180), "Post-warmup input feedback p95 budget.");
    }

    private static void AssertOnScreen(RectTransform rect, Vector2Int viewport, float scale)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
        foreach (var corner in corners)
        {
            var point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, corner);
            Assert.That(point.x, Is.InRange(-1f, viewport.x + 1f), $"{rect.name} horizontal clipping at {viewport}, scale {scale}");
            Assert.That(point.y, Is.InRange(-1f, viewport.y + 1f), $"{rect.name} vertical clipping at {viewport}, scale {scale}");
        }
    }

    private IEnumerator TouchPoint(Vector2 point)
    {
        InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = point });
        yield return null;
        InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = point });
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Mods_TwelveClans_PageTargetsPreserveIdentityAndChoices()
    {
        using (var fixture = new ClanRosterFixture(12))
        {
            yield return ModsRoundTrip();
            Assert.That(Settings().Players.Length, Is.EqualTo(12));
            Assert.That(GameObject.Find("Player8"), Is.Null, "Last authored row is reserved for paging.");
            Assert.That(Find<Button>("PreviousClans").interactable, Is.False);
            Assert.That(Find<Button>("PreviousClans").GetComponentInChildren<Text>().color, Is.EqualTo(Color.gray));
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 800), new Vector2Int(1920, 1080) })
            {
                yield return SetViewport(size.x, size.y);
                var before = Settings().Players.Select(Identity).ToArray();
                var next = Find<Button>("NextClans").GetComponent<RectTransform>();
                AssertHandler(next, next.gameObject);
                yield return Click(next);
                var previous = Find<Button>("PreviousClans").GetComponent<RectTransform>();
                AssertHandler(previous, previous.gameObject);
                yield return Click(previous);
                Assert.That(Settings().Players.Select(Identity), Is.EqualTo(before));
            }
            var first = Settings().Players[0].ClanName;
            yield return Click(Checkbox("Player1"));
            yield return Click(Find<Button>("NextClans").GetComponent<RectTransform>());
            Assert.That(Find<Text>("ClanPageLabel").text, Is.EqualTo("8-12 / 12"));
            Assert.That(Find<Button>("NextClans").interactable, Is.False);
            yield return GameSetupModSettingsFlowTests.CaptureSetupCanvas(Find<Button>("NextClans").gameObject, "mod-roster-twelve-page-two.png");
            Assert.That(GameObject.Find("Player6"), Is.Null, "Unused rows must not retain invisible targets.");
            yield return Click(RoleIcon(2));
            Assert.That(Settings().Players.Single(p => p.ClanName == "ExtraClan1").AiDifficulty, Is.EqualTo(AiDifficultyTier.Knight));
            Assert.That(Settings().Players.Any(p => p.ClanName == first), Is.False);
            yield return Click(Find<Button>("PreviousClans").GetComponent<RectTransform>());
            Assert.That(Find<Toggle>("Player1").isOn, Is.False);
            yield return ModsRoundTrip();
            Assert.That(Settings().Players.Length, Is.EqualTo(11));
            Assert.That(Settings().Players.Single(p => p.ClanName == "ExtraClan1").AiDifficulty, Is.EqualTo(AiDifficultyTier.Knight));
            Assert.That(Settings().Players.Any(p => p.ClanName == first), Is.False);
        }
    }

    [UnityTest]
    public IEnumerator Mods_TwelveClans_StartsAllPlayersWithCapitalsAndArtwork()
    {
        using (var fixture = new ClanRosterFixture(12))
        {
            yield return ModsRoundTrip();
            var expected = Settings().Players.Select(p => p.ClanName).ToArray();
            Assert.That(expected.Length, Is.EqualTo(12));
            Assert.That(Find<Button>("StartButton").interactable, Is.True);
            yield return Click(Find<Button>("StartButton").GetComponent<RectTransform>());
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Illuria" && Game.IsInitialized(), "Twelve-clan start");
            Assert.That(Game.Current.Players.Select(p => p.Clan.ShortName), Is.EqualTo(expected));
            var armies = UnityEngine.Object.FindAnyObjectByType<ArmyManager>();
            var flags = UnityEngine.Object.FindAnyObjectByType<FlagManager>();
            foreach (var player in Game.Current.Players)
            {
                Assert.That(player.Capitol, Is.Not.Null, player.Clan.ShortName);
                Assert.That(player.Capitol.Clan.ShortName, Is.EqualTo(player.Clan.ShortName));
                Assert.That(armies.FindGameObjectKind(player.Clan, ModFactory.FindArmyInfo("LightInfantry")), Is.Not.Null);
                Assert.That(flags.FindGameObjectKind(player.Clan, 1), Is.Not.Null);
            }
        }
    }

    [UnityTest]
    public IEnumerator Mods_ReorderedRosterKeepsRolesAndUsesLiteralNamesAndDataColors()
    {
        var originalName = ClanTexts(1).First().text;
        yield return Click(RoleIcon(1));
        yield return Click(Checkbox("Player2"));
        using (var fixture = new ClanRosterFixture(8))
        {
            fixture.ReverseAndRename();
            yield return ModsRoundTrip();
            Assert.That(Settings().Players.Single(p => p.ClanName == "Sirians").AiDifficulty, Is.EqualTo(AiDifficultyTier.Knight));
            Assert.That(Settings().Players.Length, Is.EqualTo(7));
            var labels = Find<Toggle>("Player1").GetComponentsInChildren<Text>().Where(t => t.text == "<b>Human</b>").ToArray();
            Assert.That(labels, Is.Not.Empty);
            Assert.That(labels.All(t => !t.supportRichText), Is.True, "Mod names must be literal, not UI markup.");
            Assert.That(labels.Any(t => ((Color32)t.color).Equals(new Color32(30, 180, 90, 255))), Is.True);
            yield return Click(RoleIcon(1));
            Assert.That(labels.All(t => t.text == "<b>Human</b>"), Is.True, "Role changes cannot overwrite a clan label.");
        }
        yield return ModsRoundTrip();
        Assert.That(Settings().Players.Single(p => p.ClanName == "Sirians").AiDifficulty, Is.EqualTo(AiDifficultyTier.Knight));
        Assert.That(ClanTexts(1).First().text, Is.EqualTo(originalName));
    }

    [UnityTest]
    public IEnumerator Mods_ReducedRosterHidesRowsAndRevertsWithoutLosingChoices()
    {
        yield return Click(RoleIcon(1));
        using (var fixture = new ClanRosterFixture(3))
        {
            yield return ModsRoundTrip();
            Assert.That(Settings().Players.Length, Is.EqualTo(3));
            Assert.That(GameObject.Find("Player4"), Is.Null);
            Assert.That(Find<Button>("StartButton").interactable, Is.True);
        }
        yield return ModsRoundTrip();
        Assert.That(Settings().Players.Length, Is.EqualTo(8));
        Assert.That(Settings().Players[0].AiDifficulty, Is.EqualTo(AiDifficultyTier.Knight));
    }

    private IEnumerator ModsRoundTrip()
    {
        yield return Click(Find<Button>("AdvancedModsButton").GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("ContinueButton") != null, "Mods screen");
        Assert.That(Find<Button>("ContinueButton").interactable, Is.True);
        yield return Click(Find<Button>("ContinueButton").GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("ShowAiCombatToggle") != null, "Mod roster return");
        Canvas.ForceUpdateCanvases();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Mods_PageBoundaries_NineFourteenFifteenThirtyTwo()
    {
        foreach (int count in new[] { 9, 14, 15, 32 })
        {
            using (var fixture = new ClanRosterFixture(count))
            {
                yield return ModsRoundTrip();
                var expected = Settings().Players.Select(Identity).ToArray();
                Assert.That(expected.Length, Is.EqualTo(count));
                for (int page = 0; page <= (count - 1) / 7; page++)
                {
                    Assert.That(Find<Text>("ClanPageLabel").text, Is.EqualTo($"{page * 7 + 1}-{Math.Min(page * 7 + 7, count)} / {count}"));
                    Assert.That(Settings().Players.Select(Identity), Is.EqualTo(expected), "Paging cannot change player state.");
                    if (page < (count - 1) / 7)
                        yield return Click(Find<Button>("NextClans").GetComponent<RectTransform>());
                }
                Assert.That(Find<Button>("NextClans").interactable, Is.False);
            }
            yield return ModsRoundTrip();
            Assert.That(Settings().Players.Length, Is.EqualTo(8));
            Assert.That(GameObject.Find("ClanPager"), Is.Null);
        }
    }

    [UnityTest]
    public IEnumerator Mods_ZeroOrOnePlayableClan_CannotStartOrLeakClicks()
    {
        foreach (int count in new[] { 0, 1 })
        {
            using (var fixture = new ClanRosterFixture(count))
            {
                yield return ModsRoundTrip();
                Assert.That(Settings().Players.Length, Is.EqualTo(count));
                Assert.That(Find<Button>("StartButton").interactable, Is.False);
                yield return Click(Find<Button>("StartButton").GetComponent<RectTransform>());
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameSetup"));
                Assert.That(Game.IsInitialized(), Is.False);
            }
            yield return ModsRoundTrip();
        }
    }

    [UnityTest]
    public IEnumerator Mods_FlavorOverlay_ChangesMembershipNameAndBothColors_ThenRestores()
    {
        yield return Click(Find<Button>("AdvancedModsButton").GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("ContinueButton") != null, "Mods screen");
        var pack = Find<Toggle>("PackToggle:pack-illurian-legends-flavor");
        if (!pack.isOn) yield return Click(pack.GetComponent<RectTransform>());
        yield return Click(Find<Button>("ContinueButton").GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("ShowAiCombatToggle") != null, "Flavor selected");
        var path = Path.Combine(UnityModKitSelection.PluginModRoot, "FeaturePacks", "pack-illurian-legends-flavor", "overlays", "mod-overlay.json");
        var bytes = File.ReadAllBytes(path);
        var oldName = ClanTexts(1).First().text;
        var oldColors = ClanTexts(1).Select(t => t.color).ToArray();
        try
        {
            var overlay = JObject.Parse(File.ReadAllText(path));
            overlay["clans"] = new JArray(
                new JObject { ["shortName"] = "Sirians", ["displayName"] = "Human", ["primaryColor"] = "(20, 150, 210)", ["secondaryColor"] = "(70, 20, 90)" },
                new JObject { ["shortName"] = "StormGiants", ["displayName"] = "Storm Giants", ["playable"] = false });
            File.WriteAllText(path, overlay.ToString());
            ModFactory.ResetCache();
            yield return ModsRoundTrip();
            Assert.That(Settings().Players.Length, Is.EqualTo(7));
            Assert.That(Settings().Players.Any(p => p.ClanName == "StormGiants"), Is.False);
            var labels = Find<Toggle>("Player1").GetComponentsInChildren<Text>().Where(t => t.text == "Human" && !t.supportRichText).ToArray();
            Assert.That(labels.Length, Is.EqualTo(2));
            Assert.That((Color32)labels[0].color, Is.EqualTo(new Color32(70, 20, 90, 255)));
            Assert.That((Color32)labels[1].color, Is.EqualTo(new Color32(20, 150, 210, 255)));
            yield return Click(RoleIcon(1));
            Assert.That(labels.All(t => t.text == "Human"), Is.True);
            Assert.That(Settings().Players[0].AiDifficulty, Is.EqualTo(AiDifficultyTier.Knight));
        }
        finally { File.WriteAllBytes(path, bytes); ModFactory.ResetCache(); }
        yield return ModsRoundTrip();
        Assert.That(Settings().Players.Length, Is.EqualTo(8));
        Assert.That(ClanTexts(1).First().text, Is.EqualTo(oldName));
        Assert.That(ClanTexts(1).Select(t => t.color), Is.EqualTo(oldColors));
    }

    [UnityTest]
    public IEnumerator Mods_CancelPreservesWorldEmptyPackSelectionAndPlayerChoices()
    {
        yield return SelectWorld("Mini-Illuria");
        yield return Click(RoleIcon(1));
        yield return Click(Checkbox("ShowAiCombatToggle"));
        var expected = Settings();
        yield return ModsRoundTrip();
        Assert.That(Settings().WorldName, Is.EqualTo("Mini-Illuria"));
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.PackIds, Is.Empty);
        yield return Click(Find<Button>("AdvancedModsButton").GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("BackButton") != null, "Mods screen");
        yield return Click(Find<Toggle>("PackToggle:pack-illurian-legends-flavor").GetComponent<RectTransform>());
        yield return Click(Find<Button>("BackButton").GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("ShowAiCombatToggle") != null, "Cancelled Mods");
        Assert.That(Settings().WorldName, Is.EqualTo(expected.WorldName));
        Assert.That(Settings().Players.Select(Identity), Is.EqualTo(expected.Players.Select(Identity)));
        Assert.That(Settings().ShowAiCombat, Is.EqualTo(expected.ShowAiCombat));
        Assert.That(UnityModKitRuntimeSelection.CurrentSelection.PackIds, Is.Empty);
    }

    private sealed class ClanRosterFixture : IDisposable
    {
        private readonly string clanPath = Path.Combine(UnityModKitSelection.PluginModRoot, "Clan.json");
        private readonly string cityPath = Path.Combine(UnityModKitSelection.PluginModRoot, "Worlds", "Illuria", "City.json");
        private readonly byte[] clanBytes;
        private readonly byte[] cityBytes;
        public ClanRosterFixture(int count)
        {
            clanBytes = File.ReadAllBytes(clanPath);
            cityBytes = File.ReadAllBytes(cityPath);
            try
            {
                var clans = JArray.Parse(File.ReadAllText(clanPath));
                var playable = clans.OfType<JObject>().Where(c => (string)c["ShortName"] != "Neutral").ToArray();
                for (int i = 0; i < playable.Length; i++) playable[i]["Playable"] = i < count;
                var cities = JArray.Parse(File.ReadAllText(cityPath));
                var neutral = cities.OfType<JObject>().Where(c => string.IsNullOrEmpty((string)c["ClanName"]) || (string)c["ClanName"] == "Neutral").ToArray();
                for (int i = playable.Length; i < count; i++)
                {
                    var clan = (JObject)playable[0].DeepClone();
                    var name = "ExtraClan" + (i - playable.Length + 1);
                    clan["ShortName"] = name;
                    clan["DisplayName"] = "Extra Clan " + (i - playable.Length + 1);
                    clan["VisualClanName"] = "Sirians";
                    clan["Playable"] = true;
                    clans.Add(clan);
                    neutral[i - playable.Length]["ClanName"] = name;
                }
                File.WriteAllText(clanPath, clans.ToString());
                File.WriteAllText(cityPath, cities.ToString());
                ModFactory.ResetCache();
            }
            catch { Dispose(); throw; }
        }
        public void ReverseAndRename()
        {
            var clans = JArray.Parse(File.ReadAllText(clanPath));
            var reordered = new JArray(clans.Reverse().ToArray());
            // Use an ID not renamed again by the selected default flavor pack.
            var first = reordered.OfType<JObject>().First(c => (string)c["ShortName"] == "HorseLords");
            first.Remove();
            reordered.Insert(0, first);
            first["DisplayName"] = "<b>Human</b>";
            first["PrimaryColor"] = "(30, 180, 90)";
            File.WriteAllText(clanPath, reordered.ToString());
            ModFactory.ResetCache();
        }
        public void Dispose()
        {
            File.WriteAllBytes(clanPath, clanBytes);
            File.WriteAllBytes(cityPath, cityBytes);
            ModFactory.ResetCache();
        }
    }

    [UnityTest]
    public IEnumerator ClanCheckboxes_All256Subsets_ChangeOnlyTheIntendedClan()
    {
        var all = Settings().Players.Select(player => player.ClanName).ToArray();
        Assert.That(all.Length, Is.EqualTo(8));
        // Gray-code traversal changes exactly one checkbox per subset.
        int previous = 255;
        for (int step = 0; step < 256; step++)
        {
            int mask = 255 ^ (step ^ (step >> 1));
            for (int i = 0; i < 8; i++)
                if (((previous ^ mask) & (1 << i)) != 0)
                    yield return Click(Checkbox("Player" + (i + 1)));
            var expected = all.Where((_, i) => (mask & (1 << i)) != 0).ToArray();
            Assert.That(Settings().Players.Select(player => player.ClanName), Is.EqualTo(expected), "Clan subset " + mask);
            Assert.That(Settings().Players.All(player => player.IsHuman), Is.True);
            Assert.That(Find<Button>("StartButton").IsInteractable(), Is.EqualTo(expected.Length >= 2), "Start validation for subset " + mask);
            previous = mask;
        }
    }

    [UnityTest]
    public IEnumerator EveryWarriorIcon_CyclesAllFiveRolesWithoutChangingSelection()
    {
        var clans = Settings().Players.Select(player => player.ClanName).ToArray();
        for (int row = 1; row <= 8; row++)
        {
            var icon = RoleIcon(row);
            var bounds = icon.rect;
            AssertHandler(icon, icon.gameObject);
            AssertRolePortrait(row, 0);
            for (int press = 1; press <= 5; press++)
            {
                yield return Click(icon);
                var players = Settings().Players;
                var player = players[row - 1];
                Assert.That(players.Select(item => item.ClanName), Is.EqualTo(clans));
                Assert.That(RoleText(row).text, Is.EqualTo(roles[press % 5]));
                Assert.That(player.IsHuman, Is.EqualTo(press == 5));
                Assert.That(player.AiDifficulty, Is.EqualTo(difficulties[press % 5]));
                AssertRolePortrait(row, press % 5);
                Assert.That(icon.rect, Is.EqualTo(bounds), "Changing portrait must not resize the click target.");
                Assert.That(players.Where((_, i) => i != row - 1).All(item => item.IsHuman), Is.True);
            }
            yield return Click(RoleText(row).rectTransform);
            Assert.That(RoleText(row).text, Is.EqualTo("Knight"), "Role text remains an alternative target.");
            AssertRolePortrait(row, 1);
            for (int i = 0; i < 4; i++) yield return Click(icon);
        }
    }

    private void AssertRolePortrait(int row, int role)
    {
        var image = RoleIcon(row).GetComponent<Image>();
        var expected = Resources.Load<Sprite>("UI/PlayerRoles/" + roles[role]);
        Assert.That(expected, Is.Not.Null, "Every role portrait must be packaged as a runtime resource.");
        Assert.That(image.sprite, Is.SameAs(expected), "Portrait must agree with the role label and difficulty.");
        Assert.That(image.overrideSprite, Is.SameAs(expected), "A legacy override must not hide the selected portrait.");
        Assert.That(image.preserveAspect, Is.True);
        Assert.That(expected.rect.size, Is.EqualTo(new Vector2(32, 30)));
        Assert.That(expected.texture.filterMode, Is.EqualTo(FilterMode.Point));
        Assert.That(expected.texture.mipmapCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator RolePortraits_KeyboardAndReselectionKeepDifficultyInSync()
    {
        var icon = RoleIcon(2);
        yield return Click(icon);
        for (int role = 1; role < 5; role++)
        {
            AssertRolePortrait(2, role);
            Assert.That(Settings().Players[1].AiDifficulty, Is.EqualTo(difficulties[role]));
            if (role < 4) yield return KeyPress(Key.Enter);
        }
        var clan = Settings().Players[1].ClanName;
        yield return Click(Checkbox("Player2"));
        Assert.That(Settings().Players.Any(player => player.ClanName == clan), Is.False);
        AssertRolePortrait(2, 4);
        yield return Click(Checkbox("Player2"));
        Assert.That(Settings().Players.Single(player => player.ClanName == clan).AiDifficulty, Is.EqualTo(AiDifficultyTier.Warlord));
        AssertRolePortrait(2, 4);
        yield return Click(icon);
        AssertRolePortrait(2, 0);
        Assert.That(Settings().Players[1].IsHuman, Is.True);
        Assert.That(Settings().Players[1].AiDifficulty, Is.Null);
    }

    [UnityTest]
    public IEnumerator RolePortraits_AreDistinctPixelArtWithTransparentBorders()
    {
        var fingerprints = new HashSet<string>();
        foreach (var role in roles)
        {
            var sprite = Resources.Load<Sprite>("UI/PlayerRoles/" + role);
            Assert.That(sprite, Is.Not.Null, role);
#if UNITY_EDITOR
            var path = UnityEditor.AssetDatabase.GetAssetPath(sprite);
            var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
            Assert.That(importer.textureCompression, Is.EqualTo(UnityEditor.TextureImporterCompression.Uncompressed), role);
            var texture = new Texture2D(2, 2);
            try
            {
                var bytes = File.ReadAllBytes(path);
                Assert.That(texture.LoadImage(bytes), Is.True);
                Assert.That(texture.width, Is.EqualTo(32));
                Assert.That(texture.height, Is.EqualTo(30));
                var pixels = texture.GetPixels32();
                Assert.That(pixels.Count(pixel => pixel.a == 255), Is.GreaterThan(100), role);
                Assert.That(pixels[0].a, Is.Zero, role);
                Assert.That(pixels[31].a, Is.Zero, role);
                Assert.That(fingerprints.Add(Convert.ToBase64String(bytes)), Is.True, "Roles need distinct artwork.");
            }
            finally { UnityEngine.Object.Destroy(texture); }
#endif
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator RolePortraits_AllStatesRenderInSettings()
    {
        for (int row = 1; row <= roles.Length; row++)
        {
            for (int press = 1; press < row; press++) yield return Click(RoleIcon(row));
            AssertRolePortrait(row, row - 1);
        }
        yield return GameSetupModSettingsFlowTests.CaptureSetupCanvas(RoleIcon(1).gameObject,
            "player-role-portraits-camera-projection.png");
    }

    [UnityTest]
    public IEnumerator UnimplementedOptions_RejectPointerAndKeyboardWithoutSideEffects()
    {
        var before = Settings().Players.Select(Identity).ToArray();
        foreach (var name in new[] { "RandomStartToggle", "InteractiveToggle" })
        {
            Assert.That(Find<Toggle>(name).IsInteractable(), Is.False);
            yield return Click(Checkbox(name));
            yield return KeyPress(Key.Enter);
            Assert.That(Settings().RandomStartLocations, Is.False);
            Assert.That(Settings().InteractiveUI, Is.True);
            Assert.That(Settings().Players.Select(Identity), Is.EqualTo(before));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameSetup"));
        }
    }

    [UnityTest]
    public IEnumerator WorldPicker_EveryOption_ValidatesAvailabilityWithoutStarting()
    {
        var worlds = Find<Dropdown>("WorldDropdown").options.Select(option => option.text).ToArray();
        foreach (var world in worlds)
        {
            yield return SelectWorld(world);
            Assert.That(Settings().WorldName, Is.EqualTo(world));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameSetup"));
            if (new[] { "Illuria", "Mini-Illuria", "TestWorld" }.Contains(world))
                Assert.That(Find<Button>("StartButton").IsInteractable(), Is.True, world);
            else
            {
                Assert.That(Find<Button>("StartButton").IsInteractable(), Is.False, world);
                yield return Click(Find<Button>("StartButton").GetComponent<RectTransform>());
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameSetup"));
            }
        }
    }

    [UnityTest] public IEnumerator Start_IlluriaTwoNoncontiguousClans() => StartRoster("Illuria", new[] { 1, 5 });
    [UnityTest] public IEnumerator Start_IlluriaFourNoncontiguousClans() => StartRoster("Illuria", new[] { 1, 3, 6, 8 });
    [UnityTest] public IEnumerator Start_IlluriaAllEightClans() => StartRoster("Illuria", Enumerable.Range(1, 8).ToArray());
    [UnityTest] public IEnumerator Start_MiniIlluria() => StartRoster("Mini-Illuria", null);
    [UnityTest] public IEnumerator Start_TestWorld() => StartRoster("TestWorld", null);

    [UnityTest]
    public IEnumerator NativeContract_MenuUsesSystemPointerWithoutCustomHotspot()
    {
#if UNITY_EDITOR
        Assert.That(UnityEditor.PlayerSettings.defaultCursor, Is.Null,
            "Menus must not bypass the bounded game cursor with a raw hardware texture.");
        Assert.That(UnityEditor.PlayerSettings.cursorHotspot, Is.EqualTo(Vector2.zero));
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NativeContract_MiniIlluriaUsesValidatedModRoot()
    {
        yield return StartRoster("Mini-Illuria", null);
        var manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        Assert.That(Path.GetFullPath(manager.ModPath), Is.EqualTo(Path.GetFullPath(UnityModKitSelection.PluginModRoot)),
            "An authored scene must not silently use the editor-only Assets/Mod copy.");
        Assert.That(manager.WorldName, Is.EqualTo("Mini-Illuria"));
        Assert.That(Game.Current.Players.Count, Is.EqualTo(8));
    }

    [UnityTest]
    public IEnumerator NativeContract_MiniIlluriaMinimapFitsWorld()
    {
        yield return StartRoster("Mini-Illuria", null);
        AssertMinimapFitsWorld();
        var camera = GameObject.FindGameObjectWithTag("MinimapCamera").GetComponent<Camera>();
        camera.Render();
        var previous = RenderTexture.active;
        var capture = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = camera.targetTexture;
            capture.ReadPixels(new Rect(0, 0, capture.width, capture.height), 0, 0);
            capture.Apply();
            Assert.That(capture.GetPixels32().Distinct().Take(9).Count(), Is.GreaterThan(8), "Minimap must render actual terrain, not an empty target.");
            var folder = Path.Combine(Application.dataPath, "../Library/WismUiCaptures");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "mini-illuria-minimap.png"), capture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.Destroy(capture);
        }
    }

    private static void AssertMinimapFitsWorld()
    {
        var map = GameObject.Find("Minimap").GetComponent<RectTransform>();
        var panel = GameObject.Find("MinimapPanel").GetComponent<RectTransform>();
        var camera = GameObject.FindGameObjectWithTag("MinimapCamera").GetComponent<Camera>();
        var aspect = World.Current.Map.GetLength(0) / (float)World.Current.Map.GetLength(1);
        Assert.That(map.rect.width / map.rect.height, Is.EqualTo(aspect).Within(.005f));
        Assert.That(camera.targetTexture.width / (float)camera.targetTexture.height, Is.EqualTo(aspect).Within(.005f));
        Assert.That(GameObject.Find("Minimap").GetComponent<RawImage>().texture, Is.SameAs(camera.targetTexture));
        Assert.That(panel.rect.width - map.rect.width, Is.GreaterThan(0f).And.LessThan(2f));
        Assert.That(camera.orthographicSize, Is.EqualTo(World.Current.Map.GetLength(1) / 2f).Within(.01f));
        Assert.That(panel.GetComponent<BoxCollider2D>().size, Is.EqualTo(map.rect.size));
        var canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
        var corners = new Vector3[4];
        panel.GetWorldCorners(corners);
        foreach (var corner in corners)
        {
            var screen = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, corner);
            Assert.That(screen.x, Is.InRange(-1f, Screen.width + 1f), "Minimap frame must fit horizontally.");
            Assert.That(screen.y, Is.InRange(-1f, Screen.height + 1f), "Minimap frame must fit vertically.");
        }
        var frame = ScreenBounds(panel);
        Assert.That(Screen.width - frame.xMax, Is.EqualTo(4f).Within(1f), "Minimap must dock right, not merely remain visible.");
        Assert.That(Screen.height - frame.yMax, Is.EqualTo(4f).Within(1f), "Minimap must dock to the top edge.");
    }

    [UnityTest]
    public IEnumerator Minimap_DocksTopRight_AfterWorldStartAndViewportResize()
    {
        yield return StartRoster("Mini-Illuria", null);
        foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 800),
            new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(1024, 768) })
        {
            yield return SetViewport(size.x, size.y);
            yield return null;
            yield return null;
            AssertMinimapFitsWorld();
            var bounds = ScreenBounds(Find<RectTransform>("MinimapPanel"));
            Assert.That(Screen.width - bounds.xMax, Is.EqualTo(4f).Within(1f), "Right docking: " + size);
            Assert.That(Screen.height - bounds.yMax, Is.EqualTo(4f).Within(1f), "Top docking: " + size);
        }
    }

    [UnityTest]
    public IEnumerator Splash_CompleteCompositionFits_AcrossViewportResize()
    {
        SceneManager.LoadScene("SplashScreen");
        yield return null;
        var splash = UnityEngine.Object.FindAnyObjectByType<Assets.Scripts.UI.Panels.SplashScreen>();
        Assert.That(splash, Is.Not.Null);
        // Hold only the scene timer, leaving the production layout lifecycle active.
        splash.StopAllCoroutines();
        var panel = Find<RectTransform>("SplashScreenPanel");
        foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 800),
            new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(1024, 768) })
        {
            yield return SetViewport(size.x, size.y);
            yield return null;
            yield return null;
            var union = ScreenBounds(panel);
            foreach (var image in panel.GetComponentsInChildren<Image>())
            {
                var bounds = ScreenBounds(image.rectTransform);
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(-1f), image.name + " left: " + size);
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(-1f), image.name + " bottom: " + size);
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(Screen.width + 1f), image.name + " right: " + size);
                Assert.That(bounds.yMax, Is.LessThanOrEqualTo(Screen.height + 1f), image.name + " top: " + size);
                union = Rect.MinMaxRect(Mathf.Min(union.xMin, bounds.xMin), Mathf.Min(union.yMin, bounds.yMin),
                    Mathf.Max(union.xMax, bounds.xMax), Mathf.Max(union.yMax, bounds.yMax));
            }
            Assert.That(union.center.x, Is.EqualTo(Screen.width / 2f).Within(1f));
            Assert.That(union.center.y, Is.EqualTo(Screen.height / 2f).Within(1f));
            Assert.That(Mathf.Max(union.width / Screen.width, union.height / Screen.height), Is.EqualTo(1f).Within(.01f),
                "Fit the composition without shrinking it unnecessarily.");
            Assert.That(panel.localScale.x, Is.EqualTo(panel.localScale.y).Within(.0001f));
            Assert.That(Game.IsInitialized(), Is.False, "Splash layout must not start gameplay.");
        }
        yield return GameSetupModSettingsFlowTests.CaptureSetupCanvas(panel.gameObject, "splash-fit-camera-projection.png");
    }

    private static Rect ScreenBounds(RectTransform rect)
    {
        var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var points = corners.Select(corner => RectTransformUtility.WorldToScreenPoint(camera, corner)).ToArray();
        return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y),
            points.Max(point => point.x), points.Max(point => point.y));
    }

    private IEnumerator StartRoster(string world, int[] selected)
    {
        yield return SelectWorld(world);
        if (selected != null)
            for (int i = 1; i <= 8; i++)
                if (Find<Toggle>("Player" + i).isOn != selected.Contains(i))
                    yield return Click(Checkbox("Player" + i));
        var rows = Enumerable.Range(1, 8).Where(i => Find<Toggle>("Player" + i).isOn).ToArray();
        for (int i = 1; i < rows.Length; i++)
            for (int press = 0; press < (i - 1) % 4 + 1; press++) yield return Click(RoleIcon(rows[i]));
        yield return Click(Checkbox("ShowAiCombatToggle"));
        var expected = Settings();
        for (int i = 0; i < rows.Length; i++) AssertRolePortrait(rows[i], i == 0 ? 0 : (i - 1) % 4 + 1);
        var cityInfos = ModFactory.LoadCityInfos(System.IO.Path.Combine(ModFactory.ModPath, ModFactory.WorldsPath, world)).ToArray();
        Assert.That(Find<Button>("StartButton").IsInteractable(), Is.True, world);
        yield return Click(Find<Button>("StartButton").GetComponent<RectTransform>());
        yield return WaitFor(() => SceneManager.GetActiveScene().name == world && Game.IsInitialized(), "Start " + world);
        Assert.That(Game.Current.Players.Select(player => player.Clan.ShortName), Is.EqualTo(expected.Players.Select(player => player.ClanName)));
        foreach (var player in expected.Players)
        {
            var actual = Game.Current.Players.Single(item => item.Clan.ShortName == player.ClanName);
            Assert.That(actual.IsHuman, Is.EqualTo(player.IsHuman), player.ClanName);
            if (player.AiDifficulty.HasValue) Assert.That(actual.AiDifficulty, Is.EqualTo(player.AiDifficulty.Value));
            Assert.That(actual.Capitol, Is.Not.Null, player.ClanName);
        }
        foreach (var info in cityInfos.Where(info => !string.IsNullOrWhiteSpace(info.ClanName) && info.ClanName != "Neutral"))
        {
            var city = World.Current.GetCities().Single(item => item.ShortName == info.ShortName);
            var expectedClan = expected.Players.Any(player => player.ClanName == info.ClanName) ? info.ClanName : "Neutral";
            Assert.That(city.Clan.ShortName, Is.EqualTo(expectedClan), "Capital ownership: " + info.ShortName);
        }
        Assert.That(UnityEngine.Object.FindAnyObjectByType<UnityManager>().ShowAiCombat, Is.False);
        AssertMinimapFitsWorld();
    }

    [UnityTest]
    public IEnumerator ModsButton_NavigatesOnce_AndContinueReturnsToSettings()
    {
        yield return Click(Find<Button>("AdvancedModsButton").GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("ContinueButton") != null, "Mods screen");
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ModSettings"));
        Assert.That(Game.IsInitialized(), Is.False, "Mods must not invoke the cloned Load callback.");
        yield return Click(Find<Button>("ContinueButton").GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("ShowAiCombatToggle") != null, "Return from Mods");
        Assert.That(Settings().WorldName, Is.EqualTo("Illuria"));
        Assert.That(Find<Button>("StartButton").IsInteractable(), Is.True);
    }

    private IEnumerator SelectWorld(string world)
    {
        var dropdown = Find<Dropdown>("WorldDropdown");
        int target = dropdown.options.FindIndex(option => option.text == world);
        Assert.That(target, Is.GreaterThanOrEqualTo(0), world);
        if (target == dropdown.value) yield break;
        int current = dropdown.value;
        yield return Click(dropdown.GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("Dropdown List") != null, "World dropdown open");
        for (int i = current; i != target; i += target > current ? 1 : -1)
            yield return KeyPress(target > current ? Key.DownArrow : Key.UpArrow);
        yield return KeyPress(Key.Enter);
        yield return WaitFor(() => GameObject.Find("Dropdown List") == null, "World dropdown closed");
        Assert.That(dropdown.value, Is.EqualTo(target));
    }

    [UnityTest] public IEnumerator Mods_LongClanNames_1024x768() => ModsLongClanNamesFitBeforeRoleTargets(1024, 768);
    [UnityTest] public IEnumerator Mods_LongClanNames_1280x800() => ModsLongClanNamesFitBeforeRoleTargets(1280, 800);
    [UnityTest] public IEnumerator Mods_LongClanNames_1920x1080() => ModsLongClanNamesFitBeforeRoleTargets(1920, 1080);

    private IEnumerator ModsLongClanNamesFitBeforeRoleTargets(int width, int height)
    {
        yield return SetViewport(width, height);
        var report = UnityModKitSelection.Inspect("classic-warlords",
            new[] { "pack-illurian-legends-flavor" }, "Illuria", UnityModKitSelection.PluginModRoot);
        Assert.That(report.isGreen, Is.True);
        UnityModKitRuntimeSelection.Set(report);
        yield return Click(Find<Button>("AdvancedModsButton").GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("ContinueButton") != null, "Mods screen");
        yield return Click(Find<Button>("ContinueButton").GetComponent<RectTransform>());
        yield return WaitFor(() => GameObject.Find("ShowAiCombatToggle") != null, "Modded settings");
        Canvas.ForceUpdateCanvases();
        Assert.That(ClanTexts(1).First().text, Is.EqualTo("Sirians of the Dawn"));
        Assert.That(ClanTexts(8).First().text, Is.EqualTo("Bane's Black Host"));
        for (int row = 1; row <= 8; row++)
        {
            AssertClanLabelFits(row);
            AssertHandler(RoleIcon(row), RoleIcon(row).gameObject);
            yield return Click(RoleIcon(row));
            AssertRolePortrait(row, 1);
        }
        yield return GameSetupModSettingsFlowTests.CaptureSetupCanvas(RoleIcon(1).gameObject,
            $"long-clan-labels-{width}x{height}-camera-projection.png");
    }

    private IEnumerable<Text> ClanTexts(int row) => GameObject.Find("Player" + row).GetComponentsInChildren<Text>()
        .Where(text => !roles.Contains(text.text.Trim()));

    private void AssertClanLabelFits(int row)
    {
        var icon = RoleIcon(row);
        foreach (var text in ClanTexts(row))
        {
            var right = text.rectTransform.TransformPoint(new Vector3(text.rectTransform.rect.xMax, 0, 0));
            var left = icon.TransformPoint(new Vector3(icon.rect.xMin, 0, 0));
            Assert.That(icon.parent.InverseTransformPoint(right).x, Is.LessThan(icon.parent.InverseTransformPoint(left).x),
                "Clan label must end before the role target: " + text.text);
            Assert.That(text.cachedTextGenerator.characterCountVisible, Is.EqualTo(text.text.Length),
                "Complete clan name must render: " + text.text);
            Assert.That(text.cachedTextGenerator.lineCount, Is.EqualTo(1), "Clan name must stay on one row.");
        }
    }

    [UnityTest] public IEnumerator PointerSweep_1024x768() => SweepViewport(1024, 768);
    [UnityTest] public IEnumerator PointerSweep_1280x800() => SweepViewport(1280, 800);
    [UnityTest] public IEnumerator PointerSweep_1920x1080() => SweepViewport(1920, 1080);

    private static IEnumerator SetViewport(int width, int height)
    {
        var editorType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("WismUnity.Playground.UnityPlaygroundCli")).First(type => type != null);
        var method = editorType.GetMethod("TryApplyEditorGameViewSize", BindingFlags.Static | BindingFlags.NonPublic);
        object[] arguments = { width, height, "Settings pointer matrix", null };
        Assert.That(method.Invoke(null, arguments), Is.True, arguments[3]?.ToString());
        yield return WaitFor(() => Screen.width == width && Screen.height == height, "Windowless viewport");
        Canvas.ForceUpdateCanvases();
    }

    private IEnumerator SweepViewport(int width, int height)
    {
        yield return SetViewport(width, height);
        for (int row = 1; row <= 8; row++)
        {
            AssertClanLabelFits(row);
            var before = Settings().Players.Select(Identity).ToArray();
            var checkbox = Checkbox("Player" + row);
            foreach (var location in new[] { new Vector2(.1f, .1f), new Vector2(.9f, .1f), new Vector2(.9f, .9f), new Vector2(.1f, .9f) })
            {
                var local = checkbox.rect.min + Vector2.Scale(checkbox.rect.size, location);
                var canvas = checkbox.GetComponentInParent<Canvas>().rootCanvas;
                var point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, checkbox.TransformPoint(local));
                yield return ClickPoint(point);
                Assert.That(Find<Toggle>("Player" + row).isOn, Is.False, "Checkbox edge " + row + " / " + location);
                yield return ClickPoint(point);
                Assert.That(Settings().Players.Select(Identity), Is.EqualTo(before));
            }
            yield return Click(RoleIcon(row));
            Assert.That(RoleText(row).text, Is.EqualTo("Knight"));
            for (int i = 0; i < 4; i++) yield return Click(RoleIcon(row));
        }
        yield return Click(Checkbox("ShowAiCombatToggle"));
        Assert.That(Settings().ShowAiCombat, Is.False);
        Assert.That(Settings().Players.All(player => player.IsHuman), Is.True);
    }

    [UnityTest]
    public IEnumerator Load_EmptySlotsAreDisabled_CancelReturnsToSettings()
    {
        var originalDirectory = PersistanceManager.SaveDirectory;
        var directory = Path.Combine(Application.temporaryCachePath, "SettingsLoad-" + Guid.NewGuid().ToString("N"));
        try
        {
            PersistanceManager.SaveDirectory = directory;
            yield return Click(Find<Button>("LoadButton").GetComponent<RectTransform>());
            yield return WaitFor(() => UnityEngine.Object.FindAnyObjectByType<SaveLoadPicker>()?.IsInitialized() == true, "Load picker");
            var picker = UnityEngine.Object.FindAnyObjectByType<SaveLoadPicker>();
            var rows = picker.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("Button", StringComparison.Ordinal)).ToArray();
            Assert.That(rows.Length, Is.EqualTo(8));
            Assert.That(rows.All(button => !button.IsInteractable()), Is.True);
            var cancel = picker.GetComponentsInChildren<Button>().Single(button => button.GetComponentInChildren<Text>()?.text == "Cancel");
            yield return Click(cancel.GetComponent<RectTransform>());
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "GameSetup", "Cancel load returns to settings, not a placeholder game");
        }
        finally
        {
            PersistanceManager.SaveDirectory = originalDirectory;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [UnityTest]
    public IEnumerator Load_SavedRosterAndWorldOverrideCurrentSetup()
    {
        var originalDirectory = PersistanceManager.SaveDirectory;
        var originalSnapshot = PersistanceManager.GetLastSnapshot();
        var directory = Path.Combine(Application.temporaryCachePath, "SettingsLoad-" + Guid.NewGuid().ToString("N"));
        try
        {
            PersistanceManager.SaveDirectory = directory;
            yield return StartRoster("TestWorld", null);
            yield return WismTestAction.WaitForNewHeroOffer();
            yield return WismTestAction.AcceptNewHeroOffer();
            var unity = UnityEngine.Object.FindAnyObjectByType<UnityManager>();
            yield return WaitFor(() => unity.GetComponent<InputManager>().InputMode == InputMode.Game, "Save fixture ready");
            var expected = Game.Current.Players.Select(player => player.Clan.ShortName + ":" + player.IsHuman + ":" + player.AiDifficulty + ":" + player.Capitol?.ShortName).ToArray();
            PersistanceManager.Save("WISM3.SAV", "Settings regression fixture", unity);
            UnityManager.SetNewGameSettings(null);
            UnityModKitRuntimeSelection.Clear();
            SceneManager.LoadScene("GameSetup");
            yield return WaitFor(() => GameObject.Find("LoadButton") != null, "Return to setup");
            yield return null;
            yield return Click(Find<Button>("LoadButton").GetComponent<RectTransform>());
            yield return WaitFor(() => UnityEngine.Object.FindAnyObjectByType<SaveLoadPicker>()?.IsInitialized() == true, "Load picker");
            var picker = UnityEngine.Object.FindAnyObjectByType<SaveLoadPicker>();
            var rows = picker.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("Button", StringComparison.Ordinal)).ToArray();
            Assert.That(rows.Where(button => button.IsInteractable()).Select(button => button.name), Is.EqualTo(new[] { "Button3" }));
            yield return Click(rows.Single(button => button.name == "Button3").GetComponent<RectTransform>());
            yield return Click(picker.GetComponentsInChildren<Button>().Single(button => button.GetComponentInChildren<Text>()?.text == "Load").GetComponent<RectTransform>());
            yield return WaitFor(() => UnityEngine.Object.FindAnyObjectByType<UnityManager>() is UnityManager loaded &&
                !loaded.AwaitingInitialLoad && loaded.GetComponent<InputManager>().InputMode == InputMode.Game &&
                SceneManager.GetActiveScene().name == "TestWorld", "Saved game loaded in its own world scene");
            Assert.That(Game.Current.Players.Select(player => player.Clan.ShortName + ":" + player.IsHuman + ":" + player.AiDifficulty + ":" + player.Capitol?.ShortName), Is.EqualTo(expected));
            Assert.That(UnityEngine.Object.FindAnyObjectByType<UnityManager>().GameManager.WorldName, Is.EqualTo("TestWorld"));
            Assert.That(UnityEngine.Object.FindAnyObjectByType<UnityManager>().ShowAiCombat, Is.False);
        }
        finally
        {
            PersistanceManager.SaveDirectory = originalDirectory;
            typeof(PersistanceManager).GetMethod("SetLastSnapshot", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { originalSnapshot });
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static RectTransform Checkbox(string name) => GameObject.Find(name).transform.Find("Background").GetComponent<RectTransform>();
    private static RectTransform RoleIcon(int row) => GameObject.Find("Player" + row).GetComponentsInChildren<Image>()
        .Single(image => image.transform.parent.name == "Player" + row && image.name.StartsWith("Image", StringComparison.Ordinal)).rectTransform;
    private Text RoleText(int row) => GameObject.Find("Player" + row).GetComponentsInChildren<Text>().Single(text => roles.Contains(text.text.Trim()));

    private static UnityNewGameEntity Settings() => (UnityNewGameEntity)typeof(GameSetup)
        .GetMethod("GetGameSettings", BindingFlags.NonPublic | BindingFlags.Instance)
        .Invoke(UnityEngine.Object.FindAnyObjectByType<GameSetup>(), null);

    private static T Find<T>(string name) where T : Component => GameObject.Find(name).GetComponent<T>();

    private static Vector2 Point(RectTransform rect)
    {
        var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
        return RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            rect.TransformPoint(rect.rect.center));
    }

    private static void AssertHandler(RectTransform rect, GameObject expected)
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Point(rect) }, hits);
        Assert.That(hits, Is.Not.Empty, expected.name + " must be hit through the authored canvas.");
        var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Assert.That(handler, Is.EqualTo(expected), expected.name + " was intercepted by " + hits[0].gameObject.name);
    }

    private IEnumerator Click(RectTransform rect)
    {
        AssertWindowless();
        Canvas.ForceUpdateCanvases();
        yield return ClickPoint(Point(rect));
    }

    private IEnumerator ClickPoint(Vector2 point)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return null;
        yield return null;
    }

    private IEnumerator KeyPress(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private static IEnumerator WaitFor(Func<bool> condition, string description)
    {
        var deadline = Time.realtimeSinceStartup + 20;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, description);
        AssertWindowless();
    }

    private static void AssertWindowless()
    {
#if UNITY_EDITOR_WIN
        if (!Application.isBatchMode) return;
        uint owner = GetCurrentProcessId();
        int visible = 0;
        Assert.That(EnumWindows((window, _) => {
            GetWindowThreadProcessId(window, out var process);
            if (process == owner && IsWindowVisible(window)) visible++;
            return true;
        }, IntPtr.Zero), Is.True);
        Assert.That(visible, Is.Zero, "Background settings tests must never show desktop windows.");
#endif
    }

#if UNITY_EDITOR_WIN
    private delegate bool WindowVisitor(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentProcessId();
#endif
}
