using System.Collections;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;

public sealed partial class ArmyUiInputTests
{
    [UnityTest]
    public IEnumerator LiveRegression_DefendAdvancesOnceAndEmptySelectionDoesNothing()
    {
        var army = Game.Current.GetCurrentPlayer().ConscriptArmy(
            Wism.Client.Modules.Infos.ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[2, 2]);
        yield return Tap(ScreenPoint(hero.Tile));
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
            new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.D));
        yield return null;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        yield return WaitFor(() => hero.IsDefending && Game.Current.GetSelectedArmies() != null && Game.Current.GetSelectedArmies().Contains(army));
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(Game.Current.GetSelectedArmies().Single(), Is.SameAs(army));
        manager.DeselectArmies();
        yield return WaitFor(() => !Game.Current.ArmiesSelected());
        var before = State();
        manager.DefendSelectedArmies();
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(State(), Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator LiveRegression_EndTurnRequiresAltE()
    {
        var player = Game.Current.GetCurrentPlayer();
        var before = State();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
            new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E));
        yield return null;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(State(), Is.EqualTo(before), "Plain E must not end the turn.");
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
            new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.LeftAlt, UnityEngine.InputSystem.Key.E));
        yield return null;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        yield return WaitFor(() => Game.Current.GetCurrentPlayer() != player);
    }

    [UnityTest]
    public IEnumerator LiveRegression_RenewProductionSpansWindowAndKeepsAnswerSemantics()
    {
        var asset = (GameObject)EditorType("UnityEditor.AssetDatabase")
            .GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(System.Type) })
            .Invoke(null, new object[] { "Assets/Prefab/UI/Panels/RenewProductionPanel.prefab", typeof(GameObject) });
        var existing = UnityUtilities.GameObjectHardFind("RenewProductionPanel");
        var instance = Object.Instantiate(asset, existing.transform.parent, false);
        var box = instance.GetComponent<YesNoBox>();
        int width = Screen.width, height = Screen.height;
        bool interactive = unity.InteractiveUI;
        unity.InteractiveUI = true;
        yield return null;
        var before = State();
        try
        {
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(3440, 1440) })
            {
                ApplyViewport(size.x, size.y);
                yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y);
                box.AskFullWidth("Marthos 1st Light Infantry - Produced!");
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4];
                ((RectTransform)instance.transform).GetWorldCorners(corners);
                var canvas = instance.GetComponentInParent<Canvas>();
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Assert.That(RectTransformUtility.WorldToScreenPoint(camera, corners[0]).x, Is.EqualTo(0).Within(1));
                Assert.That(RectTransformUtility.WorldToScreenPoint(camera, corners[2]).x, Is.EqualTo(Screen.width).Within(1));
                Assert.That(State(), Is.EqualTo(before), "Opening a report cannot mutate gameplay.");
                box.Yes(); Assert.That(box.Answer, Is.True);
                box.Clear(); box.No(); Assert.That(box.Answer, Is.False);
                box.Clear(); box.Cancel(); Assert.That(box.Cancelled, Is.True);
                box.Clear(); Assert.That(box.Answer, Is.Null); Assert.That(box.Cancelled, Is.False);
            }
        }
        finally
        {
            unity.InteractiveUI = interactive;
            Object.Destroy(instance);
            ApplyViewport(width, height);
        }
    }

    [UnityTest]
    public IEnumerator ProductionPicker_ClassicControlsRemainVisibleAcrossViewports()
    {
        int width = Screen.width, height = Screen.height;
        var city = Game.Current.GetCurrentPlayer().Capitol;
        var before = State();
        var training = city.Barracks.ArmyInTraining;
        var panelField = typeof(UnityManager).GetField("productionPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var originalPanel = (GameObject)panelField.GetValue(unity);
        var asset = (GameObject)EditorType("UnityEditor.AssetDatabase")
            .GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(System.Type) })
            .Invoke(null, new object[] { "Assets/Prefab/UI/Panels/CityProductionPanel.prefab", typeof(GameObject) });
        Assert.That(asset, Is.Not.Null, "Exercise the shipped picker, not only the test-scene copy.");
        var shippedPanel = Object.Instantiate(asset, originalPanel.transform.parent, false);
        panelField.SetValue(unity, shippedPanel);
        OpenProductionPicker(false);
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        try
        {
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(3440, 1440) })
            {
                ApplyViewport(size.x, size.y);
                yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y);
                yield return null;
                Canvas.ForceUpdateCanvases();
                string capturePath = null;
                yield return CaptureProductionPicker(picker, path => capturePath = path);
                Assert.That(picker.GetPanelMode(), Is.EqualTo(ProductionPanelMode.SingleCity));
                var summary = picker.transform.Find("WismProductionPanelSummary");
                Assert.That(summary == null || !summary.gameObject.activeInHierarchy, Is.True,
                    "P-then-city must not display the management summary over its army choices.");
                foreach (var button in picker.GetComponentsInChildren<Button>())
                {
                    var hit = button.GetComponent<WismHitArea>();
                    Assert.That(hit, Is.Not.Null, button.name);
                    var bounds = hit.GetVisualScreenBounds();
                    Assert.That(bounds.width, Is.GreaterThan(0), button.name);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(-1), button.name);
                    Assert.That(bounds.xMax, Is.LessThanOrEqualTo(Screen.width + 1), button.name);
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(-1), button.name);
                    Assert.That(bounds.yMax, Is.LessThanOrEqualTo(Screen.height + 1), button.name);
                    var hits = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = bounds.center }, hits);
                    Assert.That(hits.Count, Is.GreaterThan(0), button.name);
                    Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button),
                        "A visible picker control must not be covered by another panel: " + button.name);
                }
                var slots = picker.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("ArmyButton")).ToArray();
                Assert.That(slots.Length, Is.EqualTo(city.Barracks.GetProductionKinds().Count));
                foreach (var slot in slots)
                {
                    Assert.That(slot.transform.Find("ArmyKind").GetComponent<Image>().sprite, Is.Not.Null);
                    var text = slot.transform.Find("ArmyInfo").GetComponent<Text>();
                    Assert.That(text.font, Is.Not.Null);
                    Assert.That(text.text, Does.Contain("t /"));
                    AssertProductionTextRendered(text, capturePath);
                }
            }
            Assert.That(State(), Is.EqualTo(before), "Opening and resizing cannot mutate gameplay.");
            Assert.That(city.Barracks.ArmyInTraining, Is.SameAs(training));
            picker.OnArmy1Click();
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("ArmyButton1"));
            Assert.That(city.Barracks.ArmyInTraining, Is.SameAs(training), "Choosing an option is not starting production.");
        }
        finally
        {
            picker.OnExitClick();
            panelField.SetValue(unity, originalPanel);
            Object.Destroy(shippedPanel);
            ApplyViewport(width, height);
        }
    }

    [UnityTest]
    public IEnumerator ProductionPicker_ManagementThenSingleCityNeverLeaksManagementChrome()
    {
        var player = Game.Current.GetCurrentPlayer();
        OpenProductionPicker(true);
        yield return null;
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        var summary = picker.transform.Find("WismProductionPanelSummary");
        Assert.That(summary, Is.Not.Null);
        Assert.That(summary.gameObject.activeInHierarchy, Is.True);
        var bounds = ((RectTransform)summary).anchorMin;
        Assert.That(bounds.y, Is.EqualTo(1), "Management details must sit above, not cover, the picker.");
        Assert.That(((RectTransform)summary).rect.height, Is.EqualTo(44f).Within(0.1f));
        Assert.That(summary.GetComponent<Image>().sprite, Is.SameAs(picker.GetComponent<Image>().sprite));
        Assert.That(summary.GetComponent<Image>().color, Is.EqualTo(Color.white));
        Assert.That(summary.Find("ProductionMinimapOverlay"), Is.Null, "Do not create a second minimap.");
        Assert.That(summary.Find("ProductionJumpRow").gameObject.activeSelf, Is.False, "No blank route buttons for an idle city.");
        var classicFont = picker.GetComponentsInChildren<Button>().Single(button => button.name == "ExitButton")
            .GetComponentInChildren<Text>().font;
        foreach (var text in summary.GetComponentsInChildren<Text>())
            Assert.That(text.font, Is.SameAs(classicFont), text.name);
        ClickProductionButton(picker, "NextProductionCityButton");
        Assert.That(picker.gameObject.activeSelf, Is.True, "A cloned navigation button must not inherit Exit.");
        ClickProductionButton(picker, "PreviousProductionCityButton");
        Assert.That(picker.gameObject.activeSelf, Is.True);
        int width = Screen.width, height = Screen.height;
        try
        {
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(3440, 1440) })
            {
                ApplyViewport(size.x, size.y);
                yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y);
                yield return null;
                string capturePath = null;
                yield return CaptureProductionPicker(picker, path => capturePath = path);
                foreach (var text in summary.GetComponentsInChildren<Text>())
                    AssertProductionTextRendered(text, capturePath);
                var children = summary.Find("ProductionNavigationRow").Cast<Transform>().ToArray();
                for (int i = 1; i < children.Length; i++)
                {
                    var left = new Vector3[4];
                    var right = new Vector3[4];
                    ((RectTransform)children[i - 1]).GetWorldCorners(left);
                    ((RectTransform)children[i]).GetWorldCorners(right);
                    Assert.That(left[2].x, Is.LessThanOrEqualTo(right[0].x), "Navigation controls must not overlap.");
                }
            }
        }
        finally { ApplyViewport(width, height); }
        picker.OnExitClick();
        OpenProductionPicker(false);
        yield return null;
        Assert.That(summary.gameObject.activeInHierarchy, Is.False);
        Assert.That(picker.GetPanelMode(), Is.EqualTo(ProductionPanelMode.SingleCity));
        picker.OnExitClick();
        OpenProductionPicker(true);
        yield return null;
        Assert.That(summary.gameObject.activeInHierarchy, Is.True);
        Assert.That(picker.GetComponentsInChildren<RectTransform>(true).Count(rect => rect.name == summary.name), Is.EqualTo(1));
        picker.OnExitClick();
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
    }

    private void OpenProductionPicker(bool management)
    {
        var player = Game.Current.GetCurrentPlayer();
        typeof(UnityManager).GetMethod(management ? "ShowProductionManagementPanel" : "ShowProductionPanel",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(unity, new object[] { management ? (object)player : player.Capitol });
    }

    [UnityTest]
    public IEnumerator ProductionPicker_CompactRoutesNavigateWithoutClosingOrChangingProduction()
    {
        var player = Game.Current.GetCurrentPlayer();
        var source = player.Capitol;
        var destination = Game.Current.Players.First(other => other != player).Capitol;
        player.ClaimCity(destination);
        OpenProductionPicker(true);
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        ClickProductionButton(picker, "ArmyButton1");
        ClickProductionButton(picker, "LocButton");
        picker.SelectDestination(destination);
        yield return WaitFor(() => source.Barracks.ProducingArmy());
        picker.OnExitClick();
        OpenProductionPicker(true);
        yield return null;
        var training = source.Barracks.ArmyInTraining;
        var before = State();
        var summary = (RectTransform)picker.transform.Find("WismProductionPanelSummary");
        Assert.That(summary.rect.height, Is.EqualTo(84f).Within(0.1f));
        var to = summary.GetComponentsInChildren<Button>().Single(button => button.name == "DestinationProductionCityButton");
        Assert.That(to.GetComponentInChildren<Text>().text, Is.EqualTo("To " + destination.DisplayName));
        ClickProductionButton(picker, to.name);
        Assert.That(picker.gameObject.activeSelf, Is.True);
        Assert.That(picker.GetViewModel().SelectedCity.City, Is.SameAs(destination));
        yield return CaptureProductionPicker(picker, _ => { }, "-routed");
        ClickProductionButton(picker, "SourceProductionCityButton1");
        Assert.That(picker.gameObject.activeSelf, Is.True);
        Assert.That(picker.GetViewModel().SelectedCity.City, Is.SameAs(source));
        ClickProductionButton(picker, "NextProductionCityButton");
        Assert.That(picker.GetViewModel().SelectedCity.City, Is.SameAs(destination));
        ClickProductionButton(picker, "PreviousProductionCityButton");
        Assert.That(picker.GetViewModel().SelectedCity.City, Is.SameAs(source));
        Assert.That(source.Barracks.ArmyInTraining, Is.SameAs(training));
        Assert.That(State(), Is.EqualTo(before), "City navigation must not issue gameplay commands.");
        picker.OnExitClick();
    }

    [UnityTest]
    public IEnumerator ProductionPicker_StartStopAndReopenPreserveCurrentProductionDisplay()
    {
        yield return ExerciseProductionStartStop(false);
    }

    [UnityTest]
    public IEnumerator ProductionPicker_ManagementProdDismissesAndRestoresGameInput()
    {
        yield return ExerciseProductionStartStop(true);
    }

    private IEnumerator ExerciseProductionStartStop(bool management)
    {
        var city = Game.Current.GetCurrentPlayer().Capitol;
        OpenProductionPicker(management);
        var picker = GameObject.FindGameObjectWithTag("CityProductionPanel").GetComponent<CityProduction>();
        picker.OnProdClick();
        Assert.That(picker.gameObject.activeSelf, Is.True, "Prod without a selected army must do nothing.");
        Assert.That(city.Barracks.ProducingArmy(), Is.False);
        foreach (var name in new[] { "ArmyButton1", "ArmyButton2", "ArmyButton3", "ArmyButton4" })
        {
            ClickProductionButton(picker, name);
            yield return null;
            Assert.That(city.Barracks.ProducingArmy(), Is.False, "Selecting an option must not start production.");
        }
        ClickProductionButton(picker, "ProdButton");
        Assert.That(picker.gameObject.activeSelf, Is.False, "Prod closes both production flows like Exit.");
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
        Assert.That(unity.ProductionMode, Is.EqualTo(ProductionMode.None));
        yield return WaitFor(() => city.Barracks.ProducingArmy());
        Assert.That(city.Barracks.ArmyInTraining.ArmyInfo.ShortName,
            Is.EqualTo(city.Barracks.GetProductionKinds()[3].ArmyInfoName));
        OpenProductionPicker(management);
        yield return null;
        var current = picker.transform.Find("ClassicProductionPicker/CurrentArmyKind");
        Assert.That(current.gameObject.activeInHierarchy, Is.True);
        Assert.That(current.GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(picker.transform.Find("ClassicProductionPicker/TurnsRemainingText").GetComponent<Text>().text,
            Is.EqualTo(city.Barracks.ArmyInTraining.TurnsToProduce + "t"));
        ClickProductionButton(picker, "StopButton");
        yield return WaitFor(() => !city.Barracks.ProducingArmy());
        ClickProductionButton(picker, "ExitButton");
        OpenProductionPicker(management);
        yield return null;
        Assert.That(current.gameObject.activeInHierarchy, Is.False);
        Assert.That(picker.transform.Find("ClassicProductionPicker/TurnsRemainingText").GetComponent<Text>().text, Is.EqualTo("None"));
        picker.OnExitClick();
    }

    private static void ClickProductionButton(CityProduction picker, string name)
    {
        var button = picker.GetComponentsInChildren<Button>().Single(item => item.name == name);
        Assert.That(button.IsInteractable(), Is.True, name);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }

    private static IEnumerator CaptureProductionPicker(CityProduction picker, System.Action<string> captured, string suffix = "")
    {
        return CaptureClassicPanel(picker, captured, "production-picker-" + picker.GetPanelMode() + suffix);
    }

    private static IEnumerator CaptureClassicPanel(Component picker, System.Action<string> captured, string name)
    {
        // Batch Game View has no screen texture. Render the real overlay through the
        // camera temporarily, then restore it; interaction assertions use the original canvas.
        var canvas = picker.GetComponentInParent<Canvas>().rootCanvas;
        var mode = canvas.renderMode;
        var camera = canvas.worldCamera;
        var distance = canvas.planeDistance;
        int order = canvas.sortingOrder;
        var depths = canvas.GetComponentsInChildren<RectTransform>(true)
            .Where(rect => rect != canvas.transform).Select(rect => (rect, position: rect.localPosition)).ToArray();
        try
        {
            if (mode == RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = Camera.main.nearClipPlane + 1;
                // Screen overlays composite after camera canvases, irrespective of their sorting orders.
                int cameraOrder = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                    .Where(other => other != canvas && other.renderMode != RenderMode.ScreenSpaceOverlay)
                    .Select(other => other.sortingOrder).DefaultIfEmpty(0).Max();
                canvas.sortingOrder = Mathf.Min(short.MaxValue, cameraOrder + 1);
                // Overlay order ignores local Z; a camera render must flatten it to match that contract.
                foreach (var item in depths)
                    item.rect.localPosition = new Vector3(item.position.x, item.position.y, 0);
                Canvas.ForceUpdateCanvases();
            }
            // Canvas render-mode/sorting changes need a rendered frame, not only a layout rebuild.
            yield return null;
            Canvas.ForceUpdateCanvases();
            captured(Capture(name + "-camera-projection"));
        }
        finally
        {
            foreach (var item in depths) item.rect.localPosition = item.position;
            canvas.renderMode = mode;
            canvas.worldCamera = camera;
            canvas.planeDistance = distance;
            canvas.sortingOrder = order;
            Canvas.ForceUpdateCanvases();
        }
        yield return null;
    }

    private static void AssertProductionTextRendered(Text text, string path)
    {
        var corners = new Vector3[4];
        text.rectTransform.GetWorldCorners(corners);
        var canvas = text.GetComponentInParent<Canvas>().rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 lower = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 upper = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        var texture = new Texture2D(2, 2);
        try
        {
            Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes(path)), Is.True);
            int ink = 0;
            for (int y = Mathf.Max(0, Mathf.CeilToInt(lower.y)); y < Mathf.Min(texture.height, upper.y); y++)
            for (int x = Mathf.Max(0, Mathf.CeilToInt(lower.x)); x < Mathf.Min(texture.width, upper.x); x++)
            {
                Color pixel = texture.GetPixel(x, y);
                if (pixel.r < 0.25f && pixel.g < 0.25f && pixel.b < 0.25f) ink++;
            }
            Assert.That(ink, Is.GreaterThan(15), "Production values must actually render in both rows: " + text.transform.parent.name);
        }
        finally { Object.Destroy(texture); }
    }
}
