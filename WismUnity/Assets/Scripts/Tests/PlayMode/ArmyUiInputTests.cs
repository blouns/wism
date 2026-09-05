using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BindingFlags = System.Reflection.BindingFlags;
using Assets.Scripts.Managers;
using Assets.Scripts.Tests.PlayMode.Common;
using Assets.Scripts.UnityGame.ModKit;
using Assets.Scripts.UnityGame.Persistance.Entities;
using Assets.Tests.PlayMode;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

[UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.LinuxEditor, RuntimePlatform.OSXEditor)]
public sealed class ArmyUiInputTests : IPrebuildSetup, IPostBuildCleanup
{
    private Mouse mouse;
    private Keyboard keyboard;
    private Touchscreen touch;
    private UnityManager unity;
    private GameManager manager;
    private InputManager input;
    private Hero hero;
    private InputSettings originalInputSettings;
    private InputSettings proofInputSettings;
    private readonly List<string> traces = new List<string>();

    public void Setup() => TestSceneBuildManager.AddTestScenesToBuildSettings("Assets/Scenes/Test");
    public void Cleanup() => TestSceneBuildManager.RemoveTestScenesFromBuildSettings("Assets/Scenes/Test");

    [UnitySetUp]
    public IEnumerator StartWorld()
    {
        Game.Unload();
        UnityModKitRuntimeSelection.Clear();
        ModFactory.ModPath = GameManager.DefaultModPath;
        ModFactory.WorldPath = "TestWorld";
        ModFactory.ActiveFeaturePackIds = new List<string>();
        ModFactory.ResetCache();
        UnityManager.SetNewGameSettings(new UnityNewGameEntity
        {
            InteractiveUI = false, IsNewGame = true, RandomSeed = 1990, WorldName = "TestWorld",
            Players = new[] {
                new UnityPlayerEntity { ClanName = "Sirians", IsHuman = true },
                new UnityPlayerEntity { ClanName = "LordBane", IsHuman = true }
            }
        });
        SceneManager.LoadScene("Scenes/Test/TestWorld");
        yield return WaitFor(() => Game.IsInitialized() && SceneManager.GetActiveScene().name == "TestWorld");
        unity = GameObject.FindGameObjectWithTag("UnityManager").GetComponent<UnityManager>();
        manager = unity.GetComponent<GameManager>();
        input = unity.GetComponent<InputManager>();
        manager.StandardTime = 0f;
        manager.WarTime = 0f;
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer();
        yield return WaitFor(() => input.InputMode == InputMode.Game && unity.ExecutionMode == ExecutionMode.Running);
        hero = Game.Current.GetCurrentPlayer().GetArmies().OfType<Hero>().First();
        if (Game.Current.ArmiesSelected())
        {
            manager.DeselectArmies();
            yield return WaitFor(() => !Game.Current.ArmiesSelected());
        }
        mouse = InputSystem.AddDevice<Mouse>("ArmyProofMouse");
        keyboard = InputSystem.AddDevice<Keyboard>("ArmyProofKeyboard");
        touch = InputSystem.AddDevice<Touchscreen>("ArmyProofTouch");
        originalInputSettings = InputSystem.settings;
        proofInputSettings = UnityEngine.Object.Instantiate(originalInputSettings);
        proofInputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        proofInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        proofInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = proofInputSettings;
        InputSystem.EnableDevice(mouse);
        InputSystem.EnableDevice(keyboard);
        InputSystem.EnableDevice(touch);
        traces.Clear();
        yield return new WaitForSecondsRealtime(0.1f);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator StopWorld()
    {
        foreach (var device in new InputDevice[] { mouse, keyboard, touch })
            if (device != null && device.added) InputSystem.RemoveDevice(device);
        if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
        if (proofInputSettings != null) UnityEngine.Object.Destroy(proofInputSettings);
        var cleanup = SceneManager.CreateScene("ArmyInputCleanup");
        SceneManager.SetActiveScene(cleanup);
        var scene = SceneManager.GetSceneByName("TestWorld");
        if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        UnityManager.SetNewGameSettings(null);
        Game.Unload();
    }

    [UnityTest]
    public IEnumerator MousePress_SelectsOriginalStackWithoutDoubleClickDelay()
    {
        var original = hero.Tile;
        var position = ScreenPoint(original);
        int frame = Time.frameCount;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
        yield return null;
        Assert.That(input.LastPrimaryActionFrame - frame, Is.InRange(0, 1), $"Press dispatch must occur by the next frame. input={input.enabled}/{input.InputMode}; pressed={mouse.leftButton.wasPressedThisFrame}; current={Pointer.current}; mode={InputSystem.settings.updateMode}");
        Assert.That(input.LastPrimaryAction, Is.EqualTo("army.select"));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position + new Vector2(120, 0) });
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        Assert.That(Game.Current.GetSelectedArmies().Single(), Is.SameAs(hero));
        Assert.That(hero.Tile, Is.SameAs(original));
        yield return new WaitForSecondsRealtime(0.45f);
        Assert.That(hero.Tile, Is.SameAs(original), "No delayed click may move the selected army to the new pointer location.");
        Trace("mouse.select", position);
        Capture("mouse-selected");
    }

    [UnityTest]
    public IEnumerator DisabledOverlayAndHover_DoNotLeakMouseOrTouchToMap()
    {
        var position = ScreenPoint(hero.Tile);
        var overlay = new GameObject("ArmyProofOverlay", typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var target = new GameObject("DisabledAction", typeof(RectTransform), typeof(Image), typeof(Button));
        target.transform.SetParent(overlay.transform, false);
        var rect = target.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(80, 80);
        target.GetComponent<Button>().interactable = false;
        int callbacks = 0;
        target.GetComponent<Button>().onClick.AddListener(() => callbacks++);
        Canvas.ForceUpdateCanvases();
        var before = State();
        try
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            Assert.That(State(), Is.EqualTo(before), "Hover must not mutate game state.");
            yield return Click(position);
            Assert.That(input.LastPrimaryAction, Is.EqualTo("rejected"));
            target.SetActive(false);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position + new Vector2(100, 0) });
            yield return new WaitForSecondsRealtime(0.45f);
            Assert.That(State(), Is.EqualTo(before), "A dismissed overlay must not leave a delayed map click.");
            target.SetActive(true);
            Canvas.ForceUpdateCanvases();
            yield return Tap(position);
            Assert.That(input.LastPrimaryAction, Is.EqualTo("rejected"));
            Assert.That(State(), Is.EqualTo(before));
            Assert.That(callbacks, Is.Zero);
            Trace("overlay.reject-mouse-touch-hover", position);
        }
        finally { UnityEngine.Object.Destroy(overlay); }
    }

    [UnityTest]
    public IEnumerator TouchAndKeyboard_ReachRealArmySelection()
    {
        var position = ScreenPoint(hero.Tile);
        yield return Tap(position);
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        Assert.That(input.LastPrimaryAction, Is.EqualTo("army.select"));
        Assert.That(Game.Current.GetSelectedArmies().Single(), Is.SameAs(hero));
        manager.DeselectArmies();
        yield return WaitFor(() => !Game.Current.ArmiesSelected());
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.N));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        Assert.That(Game.Current.GetSelectedArmies().Single(), Is.SameAs(hero));
        Trace("touch.select-keyboard.next", position);
    }

    [UnityTest]
    public IEnumerator Attack_RejectsDistanceThenCapturesThroughPointerInput()
    {
        var enemy = Game.Current.Players[1];
        var city = enemy.Capitol;
        enemy.HireHero(city.Tile);
        var defender = enemy.GetArmies().OfType<Hero>().First();
        hero.Strength = 9;
        defender.Strength = 1;
        city.Defense = 0;
        yield return Click(ScreenPoint(hero.Tile));
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        int count = Game.Current.GetCurrentPlayer().GetCities().Count;
        var before = State();
        yield return Click(ScreenPoint(city.Tile));
        Assert.That(input.LastPrimaryAction, Is.EqualTo("rejected"));
        Assert.That(State(), Is.EqualTo(before), "Distant attack cannot mutate ownership, movement, or selection.");

        // Arrange adjacency through the existing command path; the attack itself is device input.
        manager.MoveSelectedArmies(city.X - 1, city.Y);
        yield return new WaitForLastCommand(manager.ControllerProvider);
        yield return null;
        yield return Click(ScreenPoint(city.Tile));
        Assert.That(input.LastPrimaryAction, Is.EqualTo("army.attack"));
        yield return new WaitForLastCommand(manager.ControllerProvider);
        Assert.That(city.Clan, Is.SameAs(hero.Player.Clan));
        Assert.That(defender.IsDead, Is.True);
        Assert.That(hero.IsDead, Is.False);
        Assert.That(hero.Player.GetCities().Count, Is.EqualTo(count + 1));
        Trace("mouse.attack-capture", ScreenPoint(city.Tile));
        Capture("attack-captured");
    }

    private IEnumerator Click(Vector2 position)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        yield return null;
    }

    [UnityTest]
    public IEnumerator DoubleClick_UpgradesOriginalStackWithoutMovingIt()
    {
        var tile = hero.Tile;
        hero.Player.HireHero(tile);
        Assert.That(tile.GetAllArmies().Count, Is.GreaterThan(1));
        var position = ScreenPoint(tile);
        yield return Click(position);
        var firstFrame = input.LastPrimaryActionFrame;
        yield return Click(position);
        Assert.That(input.LastPrimaryActionFrame, Is.GreaterThan(firstFrame), "The second press must be dispatched.");
        Assert.That(input.LastPrimaryAction, Is.EqualTo("army.select-all").Or.EqualTo("army.select-all-pending"));
        yield return WaitFor(() => input.LastPrimaryAction == "army.select-all");
        yield return new WaitForLastCommand(manager.ControllerProvider);
        Assert.That(Game.Current.GetSelectedArmies(), Is.EquivalentTo(tile.GetAllArmies()));
        Assert.That(hero.Tile, Is.SameAs(tile));
        Trace("mouse.double-select-original-stack", position);
    }

    [UnityTest]
    public IEnumerator PipelineExercise_ObservesRealInputAndRestoresDevices()
    {
        var bridge = EditorType("WismUnity.EditorBridge.WismArmyInputExercise");
        var requestType = bridge.GetNestedType("Request");
        var request = Activator.CreateInstance(requestType);
        var point = ScreenPoint(hero.Tile);
        requestType.GetField("modality").SetValue(request, "mouse");
        requestType.GetField("x").SetValue(request, point.x);
        requestType.GetField("y").SetValue(request, point.y);
        var settings = InputSystem.settings;
        var devices = InputSystem.devices.ToArray();
        var queued = bridge.GetMethod("Begin").Invoke(null, new[] { request });
        Assert.That(Property(queued, "status"), Is.EqualTo("Queued"));
        Assert.That(Property(queued, "executed"), Is.False);
        object status = queued;
        yield return WaitFor(() => {
            status = bridge.GetMethod("Status").Invoke(null, null);
            return (string)Property(status, "status") != "Queued";
        });
        Assert.That(Property(status, "status"), Is.EqualTo("Observed"));
        Assert.That(Property(status, "action"), Is.EqualTo("army.select"));
        Assert.That(Property(status, "executed"), Is.True);
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        Assert.That(Game.Current.GetSelectedArmies().Single(), Is.SameAs(hero));
        Assert.That(InputSystem.settings, Is.SameAs(settings));
        Assert.That(InputSystem.devices.ToArray(), Is.EquivalentTo(devices));
        Trace("pipeline.mouse-select", point);
    }

    private static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);

    private static Type EditorType(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(assembly => assembly.GetType(name)).First(type => type != null);

    private IEnumerator Tap(Vector2 position)
    {
        InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = position });
        yield return null;
        InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = position });
        yield return null;
    }

    private Vector2 ScreenPoint(Tile tile)
    {
        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        var point = (Vector2)camera.WorldToScreenPoint(unity.WorldTilemap.ConvertGameToUnityVector(tile.X, tile.Y));
        Assert.That(point.x, Is.InRange(0f, (float)Screen.width));
        Assert.That(point.y, Is.InRange(0f, (float)Screen.height));
        Assert.That(unity.WorldTilemap.GetTileAtScreenPosition(camera, point), Is.SameAs(tile));
        return point;
    }

    private static IEnumerator WaitFor(Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 15f;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "Bounded input proof wait expired.");
    }

    private string State() => string.Join("|", Game.Current.Players.Select(player =>
        player.Clan.ShortName + ":" + player.GetCities().Count + ":" +
        string.Join(",", player.GetArmies().Select(army => $"{army.Id}@{army.X}:{army.Y}:{army.MovesRemaining}")))) +
        ";selected=" + string.Join(",", Game.Current.GetSelectedArmies()?.Select(army => army.Id) ?? Enumerable.Empty<int>()) +
        ";command=" + manager.ControllerProvider.CommandController.GetLastCommand().Id;

    private void Trace(string workflow, Vector2 point)
    {
        traces.Add($"{workflow};frame={input.LastPrimaryActionFrame};dispatch={input.LastPrimaryAction};point={point};state={State()}");
        var root = Path.Combine(Application.dataPath, "../Library/WismUiCaptures");
        Directory.CreateDirectory(root);
        File.WriteAllLines(Path.Combine(root, TestContext.CurrentContext.Test.Name + ".trace.txt"), traces);
    }

    private static void Capture(string name)
    {
        // Batch mode has no Game View texture. This is map-camera evidence,
        // not a screen-space UI baseline; reuse the playground render path.
        var root = Path.Combine(Application.dataPath, "../Library/WismUiCaptures");
        Directory.CreateDirectory(root);
        var capture = EditorType("WismUnity.Playground.UnityPlaygroundCli")
            .GetMethod("CaptureScreenshot", BindingFlags.Static | BindingFlags.NonPublic);
        var path = (string)capture.Invoke(null, new object[] { root, $"map-camera-{name}-{Screen.width}x{Screen.height}" });
        var texture = new Texture2D(2, 2);
        try
        {
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True);
            Assert.That(texture.width, Is.EqualTo(Screen.width));
            Assert.That(texture.height, Is.EqualTo(Screen.height));
            var pixels = texture.GetPixels32();
            Assert.That(pixels.Select(pixel => (pixel.r, pixel.g, pixel.b)).Distinct().Take(20).Count(), Is.GreaterThan(10), "Rendered proof cannot be blank.");
        }
        finally { UnityEngine.Object.Destroy(texture); }
    }
}
