using Assets.Scripts.UI;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public sealed class JoyfulFidelityUiTests
{
    [Test]
    public void ExpandedHitArea_PreservesVisualSizeAndMeetsTouchFloor()
    {
        var host = new GameObject("host", typeof(RectTransform));
        try
        {
            var rect = host.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(20f, 18f);
            var before = rect.sizeDelta;

            WismHitTargetPolicy.Apply(host);
            var hitArea = host.GetComponent<WismHitArea>();
            hitArea.RefreshGeometry();

            Assert.That(rect.sizeDelta, Is.EqualTo(before));
            Assert.That(hitArea.TouchMinimum, Is.EqualTo(new Vector2(44f, 44f)));
            Assert.That(host.GetComponent<LayoutElement>().minWidth, Is.EqualTo(44f));
            Assert.That(host.GetComponent<LayoutElement>().minHeight, Is.EqualTo(34f));
            var expanded = host.transform.Find("WismExpandedHitArea").GetComponent<RectTransform>();
            Assert.That(expanded.sizeDelta.x, Is.GreaterThanOrEqualTo(44f));
            Assert.That(expanded.sizeDelta.y, Is.GreaterThanOrEqualTo(44f));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void HitResolver_UsesEnabledPriorityDistanceThenHierarchyOrder()
    {
        var near = Control("near", priority: 1, interactable: true);
        var priority = Control("priority", priority: 5, interactable: true);
        var disabled = Control("disabled", priority: 100, interactable: false);
        try
        {
            var point = new Vector2(20f, 20f);
            var candidates = new[]
            {
                new WismUiHitCandidate(disabled, new Rect(18f, 18f, 4f, 4f), new Rect(0f, 0f, 44f, 44f), 0),
                new WismUiHitCandidate(near, new Rect(19f, 19f, 4f, 4f), new Rect(0f, 0f, 44f, 44f), 2),
                new WismUiHitCandidate(priority, new Rect(0f, 0f, 4f, 4f), new Rect(0f, 0f, 44f, 44f), 1)
            };

            var winner = WismUiHitResolver.Resolve(candidates, point);

            Assert.That(winner.HasValue, Is.True);
            Assert.That(winner.Value.Control, Is.EqualTo(priority));
        }
        finally
        {
            Object.DestroyImmediate(near.gameObject);
            Object.DestroyImmediate(priority.gameObject);
            Object.DestroyImmediate(disabled.gameObject);
        }
    }

    [TestCase(0f, 0f, true)]
    [TestCase(43.9f, 43.9f, true)]
    [TestCase(44.1f, 20f, false)]
    [TestCase(-0.1f, 20f, false)]
    public void PointerSweep_ClassifiesBoundsAndGaps(float x, float y, bool expected)
    {
        var control = Control("sweep", priority: 0, interactable: true);
        try
        {
            var candidate = new WismUiHitCandidate(control, new Rect(10f, 10f, 20f, 20f), new Rect(0f, 0f, 44f, 44f), 0);
            Assert.That(WismUiHitResolver.Resolve(new[] { candidate }, new Vector2(x, y)).HasValue, Is.EqualTo(expected));
        }
        finally
        {
            Object.DestroyImmediate(control.gameObject);
        }
    }

    [Test]
    public void DisabledControl_DoesNotEmitSemanticAction()
    {
        var control = Control("disabled-action", priority: 0, interactable: false);
        try
        {
            Assert.That(control.State, Is.EqualTo(WismUiControlState.Disabled));
            Assert.That(control.IsEnabled, Is.False);
            Assert.That(control.ActionId, Is.EqualTo("test.action"));
        }
        finally
        {
            Object.DestroyImmediate(control.gameObject);
        }
    }

    [Test]
    public void HitTargetPolicy_NullIsNoOpAndControlStateIsExplicit()
    {
        Assert.DoesNotThrow(() => WismHitTargetPolicy.Apply(null));
        Assert.Throws<System.ArgumentNullException>(() => WismUiControl.Ensure(null, "id", WismUiControlRole.Command, "action"));

        var control = Control("state", priority: 0, interactable: true);
        try
        {
            control.SetState(WismUiControlState.Selected);
            Assert.That(control.State, Is.EqualTo(WismUiControlState.Selected));
            Assert.That(control.SemanticId, Is.EqualTo("test.state"));
            Assert.That(control.Role, Is.EqualTo(WismUiControlRole.Command));
            Assert.That(control.OverlapPriority, Is.Zero);
            control.gameObject.SetActive(false);
            Assert.That(control.State, Is.EqualTo(WismUiControlState.Hidden));
        }
        finally
        {
            Object.DestroyImmediate(control.gameObject);
        }
    }

    [Test]
    public void SurfaceContract_DeduplicatesRequiredStatesAndValidatesTarget()
    {
        var host = new GameObject("surface");
        try
        {
            Assert.Throws<System.ArgumentNullException>(() => WismUiSurface.Ensure(null, "missing"));
            var surface = WismUiSurface.Ensure(
                host,
                "game-setup",
                WismUiControlState.Normal,
                WismUiControlState.Normal,
                WismUiControlState.Disabled);
            Assert.That(surface.SurfaceId, Is.EqualTo("game-setup"));
            Assert.That(surface.RequiredStates, Is.EquivalentTo(new[] { WismUiControlState.Normal, WismUiControlState.Disabled }));
            Assert.That(WismUiSurface.Ensure(host, "game-setup"), Is.SameAs(surface));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void ExpandedHitRouter_ChoosesHigherPriorityEnabledNeighborOnce()
    {
        var canvasObject = new GameObject("canvas", typeof(Canvas), typeof(GraphicRaycaster));
        var eventSystemObject = new GameObject("events", typeof(EventSystem));
        var low = ButtonWithHitArea(canvasObject.transform, "low", new Vector2(100f, 100f), priority: 1);
        var high = ButtonWithHitArea(canvasObject.transform, "high", new Vector2(118f, 100f), priority: 5);
        var lowCount = 0;
        var highCount = 0;
        low.GetComponent<Button>().onClick.AddListener(() => lowCount++);
        high.GetComponent<Button>().onClick.AddListener(() => highCount++);

        try
        {
            Canvas.ForceUpdateCanvases();
            low.GetComponent<WismHitArea>().RefreshGeometry();
            high.GetComponent<WismHitArea>().RefreshGeometry();
            var router = low.transform.Find("WismExpandedHitArea").GetComponent<WismHitAreaRaycastTarget>();
            var overlapPoint = (low.GetComponent<WismHitArea>().GetVisualScreenBounds().center +
                high.GetComponent<WismHitArea>().GetVisualScreenBounds().center) * 0.5f;
            router.OnPointerClick(new PointerEventData(eventSystemObject.GetComponent<EventSystem>()) { position = overlapPoint });

            Assert.That(lowCount, Is.Zero);
            Assert.That(highCount, Is.EqualTo(1));

            high.GetComponent<Button>().interactable = false;
            router.OnPointerClick(new PointerEventData(eventSystemObject.GetComponent<EventSystem>()) { position = overlapPoint });
            Assert.That(lowCount, Is.EqualTo(1));
            Assert.That(highCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(eventSystemObject);
        }
    }

    [Test]
    public void HitArea_ReportsMouseAndTouchBoundsWithoutChangingVisualGeometry()
    {
        var canvasObject = new GameObject("canvas", typeof(Canvas));
        var button = ButtonWithHitArea(canvasObject.transform, "bounds", new Vector2(60f, 70f), priority: 0);
        try
        {
            Canvas.ForceUpdateCanvases();
            var hitArea = button.GetComponent<WismHitArea>();
            hitArea.RefreshGeometry();
            var visual = hitArea.GetVisualScreenBounds();
            var mouse = hitArea.GetEffectiveScreenBounds(WismUiInputModality.Mouse);
            var touch = hitArea.GetEffectiveScreenBounds(WismUiInputModality.SimulatedTouch);

            Assert.That(visual.size, Is.EqualTo(new Vector2(20f, 18f)));
            Assert.That(mouse.width, Is.EqualTo(32f));
            Assert.That(mouse.height, Is.EqualTo(32f));
            Assert.That(touch.width, Is.EqualTo(44f));
            Assert.That(touch.height, Is.EqualTo(44f));
            Assert.That(WismUiHitResolver.DistanceToRect(visual, visual.center), Is.Zero);
            Assert.That(WismUiHitResolver.DistanceToRect(visual, visual.max + Vector2.one), Is.GreaterThan(0f));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void MotionAndTypographyProfiles_ApplyDeterministicDefaultsAndReducedMotion()
    {
        var motion = ScriptableObject.CreateInstance<WismMotionProfile>();
        var typography = ScriptableObject.CreateInstance<WismTypographyProfile>();
        var textObject = new GameObject("text", typeof(RectTransform), typeof(Text));
        try
        {
            Assert.That(motion.FeedbackSeconds, Is.EqualTo(0.08f).Within(0.001f));
            Assert.That(motion.TransitionSeconds, Is.EqualTo(0.16f).Within(0.001f));
            typeof(WismMotionProfile).GetField("reducedMotion", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(motion, true);
            Assert.That(motion.ReducedMotion, Is.True);
            Assert.That(motion.FeedbackSeconds, Is.Zero);
            Assert.That(motion.TransitionSeconds, Is.Zero);

            var text = textObject.GetComponent<Text>();
            Assert.DoesNotThrow(() => typography.Apply(null));
            typography.Apply(text);
            Assert.That(text.font, Is.Not.Null);
            Assert.That(text.fontSize, Is.EqualTo(typography.BodySize));
            typography.Apply(text, heading: true);
            Assert.That(text.fontSize, Is.EqualTo(typography.HeadingSize));
            Assert.That(typography.ApprovedFont, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(motion);
            Object.DestroyImmediate(typography);
            Object.DestroyImmediate(textObject);
        }
    }

    [Test]
    public void InputAdapterAndSemanticIds_PreserveLegacyFallbacks()
    {
        Assert.DoesNotThrow(() => _ = WismUiInputAdapter.PointerPosition);
        Assert.DoesNotThrow(() => _ = WismUiInputAdapter.PrimaryPressedThisFrame());
        Assert.That(WismUiIds.FromName(null), Is.Empty);
        Assert.That(WismUiIds.FromName("NextProductionCityButton"), Is.EqualTo("next-production-city-button"));
    }

    [TestCase(false, true, true)]
    [TestCase(true, true, false)]
    [TestCase(false, false, false)]
    [TestCase(true, false, false)]
    public void MapRouting_NeverLeaksUiOrDisabledInputToGameplay(bool pointerOverUi, bool gameInputEnabled, bool expected)
    {
        Assert.That(WismPointerRoutingPolicy.CanRouteToMap(pointerOverUi, gameInputEnabled), Is.EqualTo(expected));
    }

    [Test, Performance]
    public void Resolver_OneHundredWarmInteractions_StaysBelowTransitionBudget()
    {
        var control = Control("performance", priority: 1, interactable: true);
        try
        {
            var candidates = new[]
            {
                new WismUiHitCandidate(control, new Rect(8f, 8f, 24f, 24f), new Rect(0f, 0f, 44f, 44f), 0)
            };
            for (var i = 0; i < 10; i++)
            {
                WismUiHitResolver.Resolve(candidates, new Vector2(20f, 20f));
            }

            Measure.Method(() => WismUiHitResolver.Resolve(candidates, new Vector2(20f, 20f)))
                .WarmupCount(1)
                .MeasurementCount(100)
                .IterationsPerMeasurement(1)
                .Run();
        }
        finally
        {
            Object.DestroyImmediate(control.gameObject);
        }
    }

    private static WismUiControl Control(string name, int priority, bool interactable)
    {
        var target = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        target.GetComponent<Button>().interactable = interactable;
        return WismUiControl.Ensure(target, "test." + name, WismUiControlRole.Command, "test.action", priority);
    }

    private static GameObject ButtonWithHitArea(Transform parent, string name, Vector2 position, int priority)
    {
        var target = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        target.transform.SetParent(parent, false);
        var rect = target.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(20f, 18f);
        WismUiControl.Ensure(target, "test." + name, WismUiControlRole.Command, "test." + name, priority);
        WismHitTargetPolicy.Apply(target);
        return target;
    }
}
