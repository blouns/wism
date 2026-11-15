using System.Collections;
using System.IO;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using Assets.Tests.PlayMode;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;

public sealed partial class ArmyUiInputTests
{
    [UnityTest]
    public IEnumerator PlayerControls_MenuCancelAndApplyDoNotChangeCurrentOwner()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var player = Game.Current.GetCurrentPlayer();
        var before = TerminalState();
        yield return Click(MenuPoint("OpenGameMenu"));
        yield return Click(MenuPoint("PlayerControl"));
        var dropdown = unity.GameMenu.PlayerControls.GetComponentsInChildren<Dropdown>()
            .Single(value => value.name == "Control" + player.Clan.ShortName);
        dropdown.value = 4;
        yield return Click(MenuPoint("CancelControls"));
        Assert.That(unity.GetPendingPlayerControls(), Is.Empty);
        Assert.That(TerminalState(), Is.EqualTo(before));
        foreach (int difficulty in new[] { 0, 1, 2, 3 })
        {
            yield return Click(MenuPoint("OpenGameMenu"));
            yield return Click(MenuPoint("PlayerControl"));
            dropdown = unity.GameMenu.PlayerControls.GetComponentsInChildren<Dropdown>()
                .Single(value => value.name == "Control" + player.Clan.ShortName);
            dropdown.value = difficulty + 1;
            yield return Click(MenuPoint("ApplyControls"));
            Assert.That(player.IsHuman, Is.True, "Applying a choice must not interrupt this turn.");
            var pending = unity.GetPendingPlayerControls().Single();
            Assert.That(pending.IsHuman, Is.False);
            Assert.That((int)pending.AiDifficulty.Value, Is.EqualTo(difficulty));
        }
        Assert.That(unity.QueuePlayerControl(player.Clan.ShortName, true, player.AiDifficulty), Is.True);
        Assert.That(unity.GetPendingPlayerControls(), Is.Empty, "Selecting the existing control cancels a pending change.");
        Assert.That(unity.QueuePlayerControl("missing-clan", false, AiDifficultyTier.Knight), Is.False);
        Assert.That(unity.QueuePlayerControl(player.Clan.ShortName, false, (AiDifficultyTier)99), Is.False);
    }

    [UnityTest]
    public IEnumerator PlayerControls_HumanBecomesAiOnlyOnItsFollowingTurn()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var target = Game.Current.GetCurrentPlayer();
        Assert.That(unity.QueuePlayerControl(target.Clan.ShortName, false, AiDifficultyTier.Baron), Is.True);
        manager.EndTurn();
        yield return WaitFor(() => Game.Current.GetCurrentPlayer() != target);
        Assert.That(target.IsHuman, Is.True);
        Assert.That(unity.GetPendingPlayerControls(), Has.Length.EqualTo(1));
        yield return Assets.Scripts.Tests.PlayMode.Common.WismTestAction.WaitForNewHeroOffer();
        yield return Assets.Scripts.Tests.PlayMode.Common.WismTestAction.AcceptNewHeroOffer();
        yield return WaitFor(() => input.CanAcceptGameplayInput && input.InputMode == InputMode.Game);
        manager.EndTurn();
        yield return WaitFor(() => Game.Current.GetCurrentPlayer() == target && !target.IsHuman);
        Assert.That(target.AiDifficulty, Is.EqualTo(AiDifficultyTier.Baron));
        Assert.That(unity.GetPendingPlayerControls(), Is.Empty);
        Assert.That(input.InputMode, Is.EqualTo(InputMode.AITurn));
    }

    [UnityTest]
    public IEnumerator PlayerControls_AiBecomesHumanAtItsTurnBeforeAnyAiAction()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var target = Game.Current.Players.Single(player => player != Game.Current.GetCurrentPlayer());
        target.IsHuman = false;
        Assert.That(unity.QueuePlayerControl(target.Clan.ShortName, true, target.AiDifficulty), Is.True);
        Assert.That(target.IsHuman, Is.False);
        manager.EndTurn();
        yield return WaitFor(() => Game.Current.GetCurrentPlayer() == target && target.IsHuman);
        Assert.That(unity.GetPendingPlayerControls(), Is.Empty);
        Assert.That(input.InputMode, Is.Not.EqualTo(InputMode.AITurn));
        yield return Assets.Scripts.Tests.PlayMode.Common.WismTestAction.WaitForNewHeroOffer();
        yield return Assets.Scripts.Tests.PlayMode.Common.WismTestAction.AcceptNewHeroOffer();
        yield return WaitFor(() => input.CanAcceptGameplayInput && input.InputMode == InputMode.Game);
    }

    [UnityTest]
    public IEnumerator PlayerControls_SaveRestoresPendingAndRejectsInvalidChoicesWithoutMutation()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var oldDirectory = PersistanceManager.SaveDirectory;
        var oldSnapshot = PersistanceManager.GetLastSnapshot();
        var args = System.Environment.GetCommandLineArgs();
        int option = System.Array.IndexOf(args, "-wism-profile-output");
        var root = option >= 0 && option + 1 < args.Length ? args[option + 1] : Application.temporaryCachePath;
        var directory = Path.Combine(root, "control-save-" + System.Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "controls.json");
        try
        {
            var player = Game.Current.GetCurrentPlayer();
            Assert.That(unity.QueuePlayerControl(player.Clan.ShortName, false, AiDifficultyTier.Warlord), Is.True);
            PersistanceManager.SaveDirectory = directory;
            PersistanceManager.Save("controls.json", "Pending control", unity);
            var valid = File.ReadAllText(path);
            Assert.That(unity.QueuePlayerControl(player.Clan.ShortName, true, player.AiDifficulty), Is.True);
            var oldGame = Game.Current;
            typeof(GameManager).GetMethod("LoadGame", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(manager, new object[] { path });
            yield return WaitFor(() => Game.Current != oldGame && input.CanAcceptGameplayInput);
            Assert.That(Game.Current.GetCurrentPlayer().IsHuman, Is.True);
            Assert.That(unity.GetPendingPlayerControls().Single().AiDifficulty, Is.EqualTo(AiDifficultyTier.Warlord));
            var game = Game.Current;
            var snapshot = PersistanceManager.GetLastSnapshot();
            foreach (var fault in new[] { "duplicate", "unknown", "difficulty", "legacy" })
            {
                var json = Newtonsoft.Json.Linq.JObject.Parse(valid);
                var choices = (Newtonsoft.Json.Linq.JArray)json["PendingPlayerControls"];
                if (fault == "duplicate") choices.Add(choices[0].DeepClone());
                if (fault == "unknown") choices[0]["ClanName"] = "missing-clan";
                if (fault == "difficulty") choices[0]["AiDifficulty"] = 99;
                if (fault == "legacy") json.Remove("SaveFormat");
                File.WriteAllText(path, json.ToString());
                Assert.Throws<Wism.Client.Data.SaveCompatibilityException>(() => PersistanceManager.LoadEntities(path, unity), fault);
                Assert.That(Game.Current, Is.SameAs(game), fault);
                Assert.That(PersistanceManager.GetLastSnapshot(), Is.SameAs(snapshot), fault);
                Assert.That(unity.GetPendingPlayerControls().Single().AiDifficulty, Is.EqualTo(AiDifficultyTier.Warlord), fault);
            }
        }
        finally
        {
            PersistanceManager.SaveDirectory = oldDirectory;
            typeof(PersistanceManager).GetMethod("SetLastSnapshot", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { oldSnapshot });
            if (File.Exists(path)) File.Delete(path);
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
    }

    [UnityTest]
    public IEnumerator GameMenu_SoundPreferenceRoundtripCannotUnmuteValidation()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        Assert.That(UnityManager.RequiresSilentAudio(Application.isBatchMode, System.Environment.GetCommandLineArgs()), Is.True);
        var before = TerminalState();
        yield return Click(MenuPoint("OpenGameMenu"));
        var toggle = unity.GameMenu.GetComponentsInChildren<Toggle>(true).Single(value => value.name == "SoundEnabled");
        var rect = (RectTransform)toggle.transform;
        Canvas.ForceUpdateCanvases();
        var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        bool original = unity.SoundEnabled;
        yield return Click(point);
        Assert.That(unity.SoundEnabled, Is.EqualTo(!original));
        Assert.That(AudioListener.volume, Is.Zero);
        yield return Click(point);
        Assert.That(unity.SoundEnabled, Is.EqualTo(original));
        Assert.That(AudioListener.volume, Is.Zero, "Sound On must not defeat forced process-local mute.");
        unity.GameMenu.Close();

        var oldSnapshot = PersistanceManager.GetLastSnapshot();
        var oldDirectory = PersistanceManager.SaveDirectory;
        var args = System.Environment.GetCommandLineArgs();
        int option = System.Array.IndexOf(args, "-wism-profile-output");
        var root = option >= 0 && option + 1 < args.Length ? args[option + 1] : Application.temporaryCachePath;
        var directory = Path.Combine(root, "sound-save-proof-" + System.Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "sound.json");
        try
        {
            PersistanceManager.SaveDirectory = directory;
            foreach (bool enabled in new[] { false, true })
            {
                unity.SoundEnabled = enabled;
                PersistanceManager.Save("sound.json", "Sound preference", unity);
                PersistanceManager.LoadEntities(path, unity);
                unity.SoundEnabled = !enabled;
                typeof(PersistanceManager).GetMethod("LoadLastSnapshot", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null, new object[] { unity });
                Assert.That(unity.SoundEnabled, Is.EqualTo(enabled));
                Assert.That(AudioListener.volume, Is.Zero);
            }
            var legacy = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
            legacy.Remove("SoundEnabled");
            File.WriteAllText(path, legacy.ToString());
            Assert.That(PersistanceManager.LoadEntities(path, unity).SoundEnabled, Is.True,
                "Existing valid saves without the additive preference keep the default.");
            Assert.That(TerminalState(), Is.EqualTo(before));
        }
        finally
        {
            unity.SoundEnabled = original;
            PersistanceManager.SaveDirectory = oldDirectory;
            if (File.Exists(path)) File.Delete(path);
            if (Directory.Exists(directory)) Directory.Delete(directory);
            typeof(PersistanceManager).GetMethod("SetLastSnapshot", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { oldSnapshot });
        }
    }

    [UnityTest]
    public IEnumerator GameMenu_SoundPolicyHonorsAllForcedSilentModes()
    {
        Assert.That(UnityManager.RequiresSilentAudio(false, null), Is.False);
        Assert.That(UnityManager.RequiresSilentAudio(true, new string[0]), Is.True);
        foreach (var argument in new[] { "-wism-mute-audio", "-noaudio", "-runTests", "-wism-autoplay", "-NOAUDIO" })
            Assert.That(UnityManager.RequiresSilentAudio(false, new[] { argument }), Is.True, argument);
        Assert.That(UnityManager.RequiresSilentAudio(false, new[] { "-unrelated" }), Is.False);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GameMenu_AboutIsReadableAndNeverChangesCampaign()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var before = TerminalState();
        foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1920, 1080) })
        {
            ApplyViewport(size.x, size.y);
            yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y);
            yield return Click(MenuPoint("OpenGameMenu"));
            yield return Click(MenuPoint("AboutWism"));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("CloseAbout"));
            var details = unity.GameMenu.GetComponentsInChildren<Text>().Single(text => text.name == "AboutDetails");
            Assert.That(details.text, Does.Contain(Application.version));
            Assert.That(details.text, Does.Contain("independent"));
            Canvas.ForceUpdateCanvases();
            Assert.That(details.preferredHeight, Is.LessThanOrEqualTo(details.rectTransform.rect.height + 1));
            var corners = new Vector3[4];
            details.rectTransform.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Assert.That(corner.x, Is.InRange(0f, (float)Screen.width));
                Assert.That(corner.y, Is.InRange(0f, (float)Screen.height));
            }
            yield return Click(MenuPoint("CloseAbout"));
            Assert.That(unity.GameMenu.IsOpen, Is.False);
            Assert.That(TerminalState(), Is.EqualTo(before));
        }
    }

    [UnityTest]
    public IEnumerator MenuActions_ProductionAndOwnedCitiesUseExistingModes()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        yield return Click(MenuPoint("OpenOrdersMenu"));
        Canvas.ForceUpdateCanvases();
        foreach (var button in unity.GameMenu.GetComponentsInChildren<Button>())
        {
            var corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Assert.That(corner.x, Is.InRange(-1f, Screen.width + 1f), button.name);
                Assert.That(corner.y, Is.InRange(-1f, Screen.height + 1f), button.name);
            }
        }
        Assert.That(MenuButton("ActionProduction").interactable, Is.True);
        yield return Click(MenuPoint("ActionProduction"));
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Assert.That(unity.ProductionMode, Is.EqualTo(ProductionMode.SelectCity));
        unity.ProductionMode = ProductionMode.None;
        yield return Click(MenuPoint("OpenOrdersMenu"));
        yield return Click(MenuPoint("ActionManageProduction"));
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Assert.That(unity.ProductionMode, Is.EqualTo(ProductionMode.CitySelected));
        Assert.That(GameObject.FindGameObjectWithTag("CityProductionPanel").activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator MenuActions_EndTurnUsesNormalTurnTransition()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var previousPlayer = Game.Current.GetCurrentPlayer();
        yield return Click(MenuPoint("OpenOrdersMenu"));
        Assert.That(MenuButton("ActionEndTurn").interactable, Is.True);
        yield return Click(MenuPoint("ActionEndTurn"));
        yield return WaitFor(() => Game.Current.GetCurrentPlayer() != previousPlayer);
        Assert.That(unity.GameMenu.IsOpen, Is.False);
    }

    [UnityTest]
    public IEnumerator MenuActions_NextAndSkipRemainArmyOrdersNotApplicationExit()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        int exits = 0;
        unity.GameMenu.ApplicationExit = () => exits++;
        yield return Click(MenuPoint("OpenOrdersMenu"));
        Assert.That(MenuButton("ActionSkipArmy").interactable, Is.False);
        Assert.That(MenuButton("ActionSkipArmy").GetComponentInChildren<Text>().text, Does.Contain("Select an army"));
        yield return Click(MenuPoint("ActionNextArmy"));
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        yield return new WaitForLastCommand(manager.ControllerProvider);
        yield return Click(MenuPoint("OpenOrdersMenu"));
        yield return Click(MenuPoint("ActionSkipArmy"));
        yield return new WaitForLastCommand(manager.ControllerProvider);
        Assert.That(manager.ControllerProvider.CommandController.GetLastCommand().GetType().Name, Is.EqualTo("QuitArmyCommand"));
        Assert.That(exits, Is.Zero);
        Assert.That(unity.GameMenu.IsOpen, Is.False);
    }

    [UnityTest]
    public IEnumerator MenuActions_HeroTakeAndModalAiGuardsShareDispatcher()
    {
        var item = GroundItem("StaffOfMight");
        yield return SelectItemHero();
        yield return new WaitForLastCommand(manager.ControllerProvider);
        yield return Click(MenuPoint("OpenViewMenu"));
        int command = unity.LastCommandId;
        foreach (GameplayAction action in System.Enum.GetValues(typeof(GameplayAction)))
            Assert.That(input.TryExecuteGameplayAction(action), Is.False, "Modal guard: " + action);
        Assert.That(unity.LastCommandId, Is.EqualTo(command));
        yield return Click(MenuPoint("ActionTakeItem"));
        yield return WaitFor(() => hero.HasItems() && hero.Items.Contains(item));
        Assert.That(hero.Items.Count(value => value == item), Is.EqualTo(1));
        Assert.That(hero.Tile.Items.Contains(item), Is.False);
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.enabled = false;
        var player = Game.Current.GetCurrentPlayer();
        player.IsHuman = false;
        try
        {
            foreach (GameplayAction action in System.Enum.GetValues(typeof(GameplayAction)))
            {
                Assert.That(input.GameplayActionUnavailableReason(action), Is.Not.Null);
                Assert.That(input.TryExecuteGameplayAction(action), Is.False, "AI guard: " + action);
            }
        }
        finally { player.IsHuman = true; }
    }

    [UnityTest]
    public IEnumerator GameMenu_CancelDoesNotQuitArmyOrMutateGame()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        yield return Click(ScreenPoint(hero.Tile));
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var before = TerminalState();
        var lastCommand = unity.LastCommandId;
        int exits = 0;
        unity.GameMenu.ApplicationExit = () => exits++;
        yield return Click(MenuPoint("OpenGameMenu"));
        Assert.That(unity.GameMenu.IsOpen, Is.True);
        yield return Click(MenuPoint("ExitGame"));
        Assert.That(unity.GameMenu.IsConfirmingExit, Is.True);
        Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("CancelExit"));
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        yield return Click(MenuPoint("CancelExit"));
        Assert.That(unity.GameMenu.IsConfirmingExit, Is.False);
        yield return PressJourneyKey(Key.Escape);
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
        Assert.That(TerminalState(), Is.EqualTo(before));
        Assert.That(unity.LastCommandId, Is.EqualTo(lastCommand));
        Assert.That(exits, Is.Zero);
    }

    [UnityTest]
    public IEnumerator GameMenu_KeyboardExitAndCancelDoNotSkipSelectedArmy()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        yield return Click(ScreenPoint(hero.Tile));
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var before = TerminalState();
        int exits = 0;
        unity.GameMenu.ApplicationExit = () => exits++;
        InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.LeftCtrl, Key.Q));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        yield return null;
        Assert.That(unity.GameMenu.IsConfirmingExit, Is.True);
        yield return PressJourneyKey(Key.Escape);
        yield return PressJourneyKey(Key.Escape);
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Assert.That(TerminalState(), Is.EqualTo(before));
        yield return PressJourneyKey(Key.X);
        Assert.That(unity.GameMenu.IsConfirmingExit, Is.True);
        Assert.That(exits, Is.Zero);
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator GameMenu_ConfirmExitsExactlyOnceWithoutArmyCommand()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        int exits = 0;
        unity.GameMenu.ApplicationExit = () => exits++;
        var before = TerminalState();
        unity.GameMenu.ConfirmExit();
        Assert.That(exits, Is.Zero, "No exit without visible confirmation.");
        yield return Click(MenuPoint("OpenGameMenu"));
        yield return Click(MenuPoint("ExitGame"));
        yield return Click(MenuPoint("ConfirmExit"));
        unity.GameMenu.ConfirmExit();
        Assert.That(exits, Is.EqualTo(1));
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator GameMenu_ExitAvailableOnAiTurnAndAfterGameOver()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        Game.Current.GetCurrentPlayer().IsHuman = false;
        input.TryBeginEndTurn();
        unity.GameMenu.Open(); // Open before the next AI processing frame.
        Assert.That(unity.GameMenu.IsOpen, Is.True);
        Assert.That(MenuButton("SaveGame").interactable, Is.False);
        Assert.That(MenuButton("LoadGame").interactable, Is.False);
        var before = TerminalState();
        yield return new WaitForFixedUpdate();
        yield return Click(MenuPoint("ExitGame"));
        Assert.That(unity.GameMenu.IsConfirmingExit, Is.True);
        Assert.That(TerminalState(), Is.EqualTo(before));
        unity.GameMenu.Cancel();
        unity.GameMenu.Close();
        Assert.That(input.InputMode, Is.EqualTo(InputMode.AITurn));
        Game.Current.Transition(GameState.GameOver);
        yield return new WaitForFixedUpdate();
        unity.GameMenu.RequestExit();
        Assert.That(unity.GameMenu.IsConfirmingExit, Is.True);
        unity.GameMenu.Cancel();
        unity.GameMenu.Close();
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
    }

    [UnityTest]
    public IEnumerator GameMenu_SaveAndLoadUseExistingPickers()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var before = TerminalState();
        yield return Click(MenuPoint("OpenGameMenu"));
        Assert.That(MenuButton("SaveGame").interactable, Is.True);
        yield return Click(MenuPoint("SaveGame"));
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Assert.That(input.InputMode, Is.EqualTo(InputMode.SaveGamePicker));
        Assert.That(TerminalState(), Is.EqualTo(before));
        unity.SaveLoadPicker.gameObject.SetActive(false);
        input.SetInputMode(InputMode.Game);
        yield return Click(MenuPoint("OpenGameMenu"));
        yield return Click(MenuPoint("LoadGame"));
        Assert.That(input.InputMode, Is.EqualTo(InputMode.LoadGamePicker));
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    [UnityTest] public IEnumerator GameMenu_Viewport1024() => MenuViewport(1024, 768);
    [UnityTest] public IEnumerator GameMenu_Viewport1280() => MenuViewport(1280, 720);
    [UnityTest] public IEnumerator GameMenu_Viewport1920() => MenuViewport(1920, 1080);
    [UnityTest] public IEnumerator GameMenu_ViewportUltrawide() => MenuViewport(2560, 1080);

    private IEnumerator MenuViewport(int width, int height)
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        AssertBatchWindowless();
        int oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            ApplyViewport(width, height);
            yield return null;
            yield return null;
            Assert.That(Screen.width, Is.EqualTo(width));
            Assert.That(Screen.height, Is.EqualTo(height));
            yield return Tap(MenuPoint("OpenGameMenu"));
            Assert.That(unity.GameMenu.IsOpen, Is.True);
            yield return Tap(MenuPoint("ExitGame"));
            Assert.That(unity.GameMenu.IsConfirmingExit, Is.True);
            Canvas.ForceUpdateCanvases();
            foreach (var name in new[] { "CancelExit", "ConfirmExit" })
            {
                var rect = (RectTransform)MenuButton(name).transform;
                Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(44));
                Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(44));
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Assert.That(corners.All(point => point.x >= 0 && point.y >= 0 && point.x <= Screen.width && point.y <= Screen.height), Is.True);
            }
            CaptureMenuCanvas(width, height);
            yield return Tap(MenuPoint("CancelExit"));
            unity.GameMenu.Close();
        }
        finally { ApplyViewport(oldWidth, oldHeight); }
    }

    private Button MenuButton(string name) => unity.GameMenu.GetComponentsInChildren<Button>(true).Single(button => button.name == name);
    private Vector2 MenuPoint(string name)
    {
        // Legacy journeys target a menu tab directly; reveal the collapsed navigation first.
        if (name == "OpenReports" || name == "OpenViewMenu" || name == "OpenOrdersMenu")
            unity.GameMenu.Open();
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)MenuButton(name).transform;
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }

    private void CaptureMenuCanvas(int width, int height)
    {
        var canvas = unity.GameMenu.GetComponentInChildren<Canvas>();
        CaptureUiCanvas(canvas, "game-menu-confirmation", width, height);
    }

    private void CaptureUiCanvas(Canvas canvas, string name, int width, int height)
    {
        var objects = canvas.GetComponentsInChildren<Transform>(true).Select(child => child.gameObject).ToArray();
        var layers = objects.Select(child => child.layer).ToArray();
        var cameraObject = new GameObject("OffscreenMenuProof", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        var target = new RenderTexture(width, height, 24);
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        var previousTarget = RenderTexture.active;
        try
        {
            foreach (var child in objects) child.layer = 5;
            camera.enabled = false;
            camera.cullingMask = 1 << 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(70, 82, 65, 255);
            camera.transform.position = new Vector3(-1000, -1000, -1000);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            Assert.That(texture.GetPixels32().Select(pixel => (pixel.r, pixel.g, pixel.b)).Distinct().Take(10).Count(), Is.GreaterThan(5));
            var root = Path.Combine(Application.dataPath, "../Library/WismUiCaptures");
            Directory.CreateDirectory(root);
            File.WriteAllBytes(Path.Combine(root, $"{name}-{width}x{height}.png"), texture.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            for (int i = 0; i < objects.Length; i++) objects[i].layer = layers[i];
            RenderTexture.active = previousTarget;
            camera.targetTexture = null;
            UnityEngine.Object.Destroy(texture);
            target.Release();
            UnityEngine.Object.Destroy(target);
            UnityEngine.Object.Destroy(cameraObject);
        }
    }
}
