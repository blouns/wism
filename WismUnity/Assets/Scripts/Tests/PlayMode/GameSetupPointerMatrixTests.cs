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

[Category("SettingsE2E")]
public sealed class GameSetupPointerMatrixTests
{
    private Mouse mouse;
    private Keyboard keyboard;
    private InputSettings originalSettings;
    private InputSettings testSettings;
    private readonly string[] roles = { "Human", "Knight", "Baron", "Lord", "Warlord" };

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
            AssertHandler(icon, icon.gameObject);
            for (int press = 1; press <= 5; press++)
            {
                yield return Click(icon);
                var players = Settings().Players;
                var player = players[row - 1];
                Assert.That(players.Select(item => item.ClanName), Is.EqualTo(clans));
                Assert.That(RoleText(row).text, Is.EqualTo(roles[press % 5]));
                Assert.That(player.IsHuman, Is.EqualTo(press == 5));
                Assert.That(player.AiDifficulty, Is.EqualTo(press == 5 ? (AiDifficultyTier?)null : (AiDifficultyTier)(press - 1)));
                Assert.That(players.Where((_, i) => i != row - 1).All(item => item.IsHuman), Is.True);
            }
            yield return Click(RoleText(row).rectTransform);
            Assert.That(RoleText(row).text, Is.EqualTo("Knight"), "Role text remains an alternative target.");
            for (int i = 0; i < 4; i++) yield return Click(icon);
        }
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

    [UnityTest] public IEnumerator PointerSweep_1024x768() => SweepViewport(1024, 768);
    [UnityTest] public IEnumerator PointerSweep_1280x800() => SweepViewport(1280, 800);
    [UnityTest] public IEnumerator PointerSweep_1920x1080() => SweepViewport(1920, 1080);

    private IEnumerator SweepViewport(int width, int height)
    {
        var editorType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("WismUnity.Playground.UnityPlaygroundCli")).First(type => type != null);
        var method = editorType.GetMethod("TryApplyEditorGameViewSize", BindingFlags.Static | BindingFlags.NonPublic);
        object[] arguments = { width, height, "Settings pointer matrix", null };
        Assert.That(method.Invoke(null, arguments), Is.True, arguments[3]?.ToString());
        yield return WaitFor(() => Screen.width == width && Screen.height == height, "Windowless viewport");
        Canvas.ForceUpdateCanvases();
        for (int row = 1; row <= 8; row++)
        {
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
