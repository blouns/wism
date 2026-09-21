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
            Screen.SetResolution(width, height, false);
            yield return null;
            yield return null;
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
        finally { Screen.SetResolution(oldWidth, oldHeight, false); }
    }

    private Button MenuButton(string name) => unity.GameMenu.GetComponentsInChildren<Button>(true).Single(button => button.name == name);
    private Vector2 MenuPoint(string name)
    {
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
