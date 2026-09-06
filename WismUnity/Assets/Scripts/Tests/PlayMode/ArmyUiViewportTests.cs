using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Assets.Scripts.UI;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;

public sealed partial class ArmyUiInputTests
{
    [UnityTest]
    public IEnumerator Viewport_MatrixExercisesScaledOverlayAndArmyTileBoundaries()
    {
        var originalWidth = Screen.width;
        var originalHeight = Screen.height;
        var camera = Camera.main;
        var follow = camera.GetComponent<CameraFollow>();
        var originalScale = follow.scale;
        var rows = new List<ViewportRow>();
        var overlay = new GameObject("ViewportBoundaryOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        var target = new GameObject("BlockedMapAction", typeof(RectTransform), typeof(Image), typeof(Button), typeof(WismHitArea));
        target.transform.SetParent(overlay.transform, false);
        var rect = target.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.sizeDelta = new Vector2(24, 24);
        target.GetComponent<Button>().interactable = false;
        var hit = target.GetComponent<WismHitArea>();
        try
        {
            follow.scale = 1;
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(3440, 1440) })
            foreach (var uiScale in new[] { 1f, 1.25f, 1.5f })
            {
                ApplyViewport(size.x, size.y);
                yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y);
                AssertBatchWindowless();
                scaler.scaleFactor = uiScale;
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(canvas.scaleFactor, Is.EqualTo(uiScale).Within(0.001f));
                var center = ScreenPoint(hero.Tile);
                rect.anchoredPosition = center / uiScale;
                hit.RefreshGeometry();
                Canvas.ForceUpdateCanvases();
                var visual = hit.GetVisualScreenBounds();
                var effective = hit.GetEffectiveScreenBounds(WismUiInputModality.SimulatedTouch);
                Assert.That(visual.width, Is.EqualTo(24 * uiScale).Within(0.1f));
                Assert.That(effective.width, Is.EqualTo(44 * uiScale).Within(0.1f), "Effective bounds must use logical, not physical, units.");
                var before = State();
                foreach (var offset in new[] { Vector2.zero, new Vector2(-21, 0), new Vector2(21, 0), new Vector2(0, -21), new Vector2(0, 21) })
                {
                    var point = center + offset * uiScale;
                    yield return Click(point);
                    Assert.That(input.LastPrimaryAction, Is.EqualTo("rejected"));
                    yield return Tap(point);
                    Assert.That(input.LastPrimaryAction, Is.EqualTo("rejected"));
                    Assert.That(State(), Is.EqualTo(before));
                }
                Assert.That(hit.GetVisualScreenBounds(), Is.EqualTo(visual), "Presses cannot move visual geometry.");
                target.SetActive(false);
                var originalTile = hero.Tile;
                int accepted = 0;
                foreach (var x in new[] { -0.49f, 0f, 0.49f })
                foreach (var y in new[] { -0.49f, 0f, 0.49f })
                {
                    input.SetInputMode(InputMode.UI);
                    input.SetInputMode(InputMode.Game);
                    var world = unity.WorldTilemap.ConvertGameToUnityVector(hero.X, hero.Y) + new Vector3(x, y, 0);
                    var point = (Vector2)camera.WorldToScreenPoint(world);
                    Assert.That(unity.WorldTilemap.GetTileAtScreenPosition(camera, point), Is.SameAs(originalTile));
                    if (accepted % 2 == 0) yield return Click(point); else yield return Tap(point);
                    yield return WaitFor(() => Game.Current.ArmiesSelected());
                    Assert.That(Game.Current.GetSelectedArmies().Single(), Is.SameAs(hero));
                    Assert.That(hero.Tile, Is.SameAs(originalTile));
                    accepted++;
                    manager.DeselectArmies();
                    yield return WaitFor(() => !Game.Current.ArmiesSelected());
                }
                foreach (var offset in new[] { Vector3.left, Vector3.right, Vector3.up, Vector3.down })
                {
                    var world = unity.WorldTilemap.ConvertGameToUnityVector(hero.X, hero.Y) + offset * 0.51f;
                    Assert.That(unity.WorldTilemap.GetTileAtScreenPosition(camera, camera.WorldToScreenPoint(world)), Is.Not.SameAs(originalTile), "A neighboring tile cannot be stolen by an expanded army target.");
                }
                rows.Add(new ViewportRow { width = Screen.width, height = Screen.height, uiScale = canvas.scaleFactor,
                    visualWidth = visual.width, effectiveWidth = effective.width, acceptedPresses = accepted,
                    rejectedOverlayPresses = 10, cameraBounds = camera.pixelRect,
                    capture = Path.GetFileName(Capture($"matrix-{uiScale:0.00}")) });
                target.SetActive(true);
            }
            Assert.That(rows.Count, Is.EqualTo(12));
        }
        finally
        {
            var root = Path.Combine(Application.dataPath, "../Library/WismUiCaptures");
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "army-viewport-matrix.json"), JsonUtility.ToJson(new ViewportReport { rows = rows.ToArray() }, true));
            UnityEngine.Object.Destroy(overlay);
            follow.scale = originalScale;
            ApplyViewport(originalWidth, originalHeight);
        }
    }

    [Serializable]
    private sealed class ViewportReport { public ViewportRow[] rows; }

    [Serializable]
    private sealed class ViewportRow
    {
        public int width, height, acceptedPresses, rejectedOverlayPresses;
        public float uiScale, visualWidth, effectiveWidth;
        public Rect cameraBounds;
        public string capture;
    }

    [UnityTest]
    public IEnumerator Viewport_UsesActualGameViewDimensions()
    {
        var originalWidth = Screen.width;
        var originalHeight = Screen.height;
        try
        {
            ApplyViewport(1024, 768);
            yield return WaitFor(() => Screen.width == 1024 && Screen.height == 768,
                () => $"Actual viewport is {Screen.width}x{Screen.height}");
            AssertBatchWindowless();
            Capture("viewport-smoke");
        }
        finally { ApplyViewport(originalWidth, originalHeight); }
    }

    [UnityTest]
    public IEnumerator Viewport_RejectsOutsideCameraAndNonfiniteCoordinates()
    {
        var camera = Camera.main;
        var originalRect = camera.rect;
        var originalSize = camera.orthographicSize;
        var follow = camera.GetComponent<CameraFollow>();
        var originalScale = follow.scale;
        var before = State();
        try
        {
            follow.scale = 0;
            camera.orthographicSize = 0.5f;
            camera.rect = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
            camera.transform.position = new Vector3(5, 5, -10);
            var bounds = camera.pixelRect;
            var points = new[] {
                new Vector2(bounds.xMin - 1, bounds.center.y),
                new Vector2(bounds.xMax, bounds.center.y),
                new Vector2(bounds.center.x, bounds.yMin - 1),
                new Vector2(bounds.center.x, bounds.yMax),
                new Vector2(float.NaN, bounds.center.y),
                new Vector2(bounds.center.x, float.PositiveInfinity)
            };
            foreach (var point in points)
                Assert.That(unity.WorldTilemap.GetTileAtScreenPosition(camera, point), Is.Null, $"Outside viewport: {point}");
            foreach (var point in points.Take(4))
            {
                yield return Click(point);
                Assert.That(input.LastPrimaryAction, Is.EqualTo("rejected"));
                yield return Tap(point);
                Assert.That(input.LastPrimaryAction, Is.EqualTo("rejected"));
                Assert.That(State(), Is.EqualTo(before));
            }
        }
        finally
        {
            camera.rect = originalRect;
            camera.orthographicSize = originalSize;
            follow.scale = originalScale;
        }
    }

    [UnityTest]
    public IEnumerator Viewport_ZoomRefreshesBoundsAndNextArmyFramesOffscreenSelection()
    {
        var camera = Camera.main;
        var follow = camera.GetComponent<CameraFollow>();
        var originalScale = follow.scale;
        try
        {
            follow.scale = 4;
            follow.ConfigureBoundsFromCurrentWorld();
            var previous = follow.yMinClamp;
            follow.scale = 12;
            yield return null;
            yield return null;
            Assert.That(follow.yMinClamp, Is.EqualTo(camera.orthographicSize).Within(0.001f));
            Assert.That(follow.yMinClamp, Is.LessThan(previous));
            follow.target = null;
            follow.SetCameraTarget(new Vector3(8, 8, 0));
            var worldPoint = unity.WorldTilemap.ConvertGameToUnityVector(hero.X, hero.Y);
            Assert.That(new Rect(0, 0, 1, 1).Contains(camera.WorldToViewportPoint(worldPoint)), Is.False);
            var tile = hero.Tile;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.N));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return WaitFor(() => Game.Current.ArmiesSelected());
            yield return null;
            Assert.That(Game.Current.GetSelectedArmies().Single(), Is.SameAs(hero));
            Assert.That(hero.Tile, Is.SameAs(tile));
            var framed = camera.WorldToViewportPoint(worldPoint);
            Assert.That(framed.x, Is.InRange(0.05f, 0.95f));
            Assert.That(framed.y, Is.InRange(0.05f, 0.95f));
            Trace("keyboard.offscreen-next-camera", ScreenPoint(tile));
            Capture("offscreen-next-framed");
        }
        finally { follow.scale = originalScale; }
    }

    private static void ApplyViewport(int width, int height)
    {
        AssertBatchWindowless();
        var method = EditorType("WismUnity.Playground.UnityPlaygroundCli")
            .GetMethod("TryApplyEditorGameViewSize", BindingFlags.Static | BindingFlags.NonPublic);
        object[] arguments = { width, height, "Army viewport proof", null };
        Assert.That(method.Invoke(null, arguments), Is.True, arguments[3]?.ToString());
        AssertBatchWindowless();
    }

    private static void AssertBatchWindowless()
    {
#if UNITY_EDITOR_WIN
        if (!Application.isBatchMode) return;
        int visible = 0;
        uint owner = GetCurrentProcessId();
        bool enumerated = EnumWindows((window, _) => {
            GetWindowThreadProcessId(window, out var process);
            if (process == owner && IsWindowVisible(window)) visible++;
            return true;
        }, IntPtr.Zero);
        Assert.That(enumerated, Is.True, "Window enumeration must succeed before declaring a background run quiet.");
        Assert.That(visible, Is.Zero, "Batch viewport proof must not display desktop windows.");
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
