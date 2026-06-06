using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.UnityGame.ModKit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wism.Client.Modules.Profiles;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class ModSettingsPanel : MonoBehaviour
{
    const string GameSetupScene = "GameSetup";

    readonly HashSet<string> selectedPacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    Dropdown profileDropdown;
    Dropdown worldDropdown;
    Text statusText;
    Text detailText;
    Transform packList;
    Button continueButton;
    string[] profileIds = new string[0];
    string[] worldIds = new string[0];
    FeaturePackManifest[] packs = new FeaturePackManifest[0];
    UnityModKitSelectionReport currentReport;

    void Start()
    {
        BuildUi();
        Refresh();
    }

    void BuildUi()
    {
        EnsureCamera();
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }
        ConfigureCanvas(canvas);

        var root = CreatePanel(transform, "Mod Settings", new Vector2(32f, 32f), new Vector2(-32f, -32f));
        LayoutElement(root, 0, 760, 0);
        CreateHeader(root.transform, "WISM Mod Settings");
        CreateBodyText(root.transform, "Choose a profile, world, and data-only feature packs before starting a new game.");

        profileDropdown = CreateDropdown(root.transform, "Profile");
        profileDropdown.gameObject.name = "ProfileDropdown";
        profileDropdown.onValueChanged.AddListener(_ => Evaluate());

        worldDropdown = CreateDropdown(root.transform, "World");
        worldDropdown.gameObject.name = "WorldDropdown";
        worldDropdown.onValueChanged.AddListener(_ => Evaluate());

        CreateBodyText(root.transform, "Feature Packs");
        var scroll = CreateScroll(root.transform);
        scroll.gameObject.name = "PackScroll";
        packList = scroll.content;
        packList.gameObject.name = "PackList";

        statusText = CreateText(root.transform, "Status", 16, FontStyle.Bold);
        statusText.gameObject.name = "StatusText";
        LayoutElement(statusText.gameObject, 28);
        detailText = CreateText(root.transform, string.Empty, 13, FontStyle.Normal);
        detailText.gameObject.name = "DetailText";
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.verticalOverflow = VerticalWrapMode.Truncate;
        LayoutElement(detailText.gameObject, 104);

        var actions = CreateHorizontal(root.transform);
        actions.gameObject.name = "ActionsRow";
        LayoutElement(actions, 44);
        CreateButton(actions.transform, "Back", () => SceneManager.LoadScene("SplashScreen")).gameObject.name = "BackButton";
        CreateButton(actions.transform, "Refresh", Refresh).gameObject.name = "RefreshButton";
        continueButton = CreateButton(actions.transform, "Continue", Continue);
        continueButton.gameObject.name = "ContinueButton";
    }

    void Refresh()
    {
        UnityModKitRuntimeSelection.Clear();
        selectedPacks.Clear();
        profileIds = DiscoverIds(Path.Combine(UnityModKitSelection.PluginModRoot, "Profiles"), "classic-warlords");
        worldIds = DiscoverIds(Path.Combine(UnityModKitSelection.PluginModRoot, "Worlds"), "Mini-Illuria");
        packs = ModularGameProfileCatalog.DiscoverPacksFromModRoot(UnityModKitSelection.PluginModRoot).ToArray();
        ResetDropdown(profileDropdown, profileIds, "classic-warlords");
        ResetDropdown(worldDropdown, worldIds, "TestWorld");
        RebuildPackRows();
        Evaluate();
    }

    void RebuildPackRows()
    {
        foreach (Transform child in packList)
        {
            Destroy(child.gameObject);
        }

        foreach (var pack in packs)
        {
            var row = CreateHorizontal(packList);
            row.name = "PackToggle:" + pack.Id;
            var toggle = row.AddComponent<Toggle>();
            var background = CreateText(row.transform, string.Empty, 1, FontStyle.Normal);
            background.gameObject.name = "Background";
            toggle.targetGraphic = background;
            var label = CreateText(row.transform, PackLabel(pack, false), 13, FontStyle.Normal);
            toggle.onValueChanged.AddListener(selected =>
            {
                if (selected)
                {
                    selectedPacks.Add(pack.Id);
                }
                else
                {
                    selectedPacks.Remove(pack.Id);
                }

                label.text = PackLabel(pack, selected);
                Evaluate();
            });
        }
    }

    void Evaluate()
    {
        var profile = Current(profileDropdown, profileIds);
        var world = Current(worldDropdown, worldIds);
        currentReport = UnityModKitSelection.Inspect(
            profile,
            selectedPacks.ToArray(),
            world,
            UnityModKitSelection.PluginModRoot);

        var sceneAvailable = IsWorldSceneAvailable(world);
        var green = currentReport.isGreen && sceneAvailable;
        statusText.text = green
            ? "Green: verified stack can start a new game."
            : "Red: fix selection before continuing.";
        detailText.text = BuildDetail(sceneAvailable);
        continueButton.interactable = green;

        if (green)
        {
            UnityModKitRuntimeSelection.Set(currentReport);
        }
        else
        {
            UnityModKitRuntimeSelection.Clear();
        }
    }

    string BuildDetail(bool sceneAvailable)
    {
        var lines = new List<string>
        {
            "Profile: " + currentReport.profileId,
            "World: " + currentReport.worldName,
            "Compatibility: " + currentReport.compatibilityStatus,
            "Fingerprint: " + (currentReport.contentFingerprint ?? string.Empty),
            "Scene: " + (sceneAvailable ? "Available" : "Missing")
        };

        foreach (var issue in currentReport.compatibilityIssues ?? Array.Empty<UnityModKitValidationIssueSummary>())
        {
            lines.Add($"{issue.severity}: {issue.code} - {issue.message}");
        }

        if (!sceneAvailable)
        {
            lines.Add("A matching Unity scene must exist in Assets/Scenes or Assets/Scenes/Test.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    void Continue()
    {
        Evaluate();
        if (currentReport == null || !currentReport.isGreen || !IsWorldSceneAvailable(currentReport.worldName))
        {
            return;
        }

        UnityModKitRuntimeSelection.Set(currentReport);
        SceneManager.LoadScene(GameSetupScene);
    }

    static string[] DiscoverIds(string root, string fallback)
    {
        var ids = Directory.Exists(root)
            ? Directory.GetDirectories(root)
                .Select(Path.GetFileName)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : new string[0];

        return ids.Length == 0 ? new[] { fallback } : ids;
    }

    static void ResetDropdown(Dropdown dropdown, string[] values, string preferred)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(values.ToList());
        var index = Array.FindIndex(values, value => string.Equals(value, preferred, StringComparison.OrdinalIgnoreCase));
        dropdown.value = Math.Max(0, index);
        dropdown.RefreshShownValue();
    }

    static string Current(Dropdown dropdown, string[] values)
    {
        if (values.Length == 0)
        {
            return string.Empty;
        }

        return values[Mathf.Clamp(dropdown.value, 0, values.Length - 1)];
    }

    static bool IsWorldSceneAvailable(string world)
    {
        return IsSceneAvailable("Assets/Scenes/" + world + ".unity") ||
               IsSceneAvailable("Assets/Scenes/Test/" + world + ".unity");
    }

    static bool IsSceneAvailable(string path)
    {
#if UNITY_EDITOR
        if (File.Exists(path))
        {
            return true;
        }
#endif
        return SceneUtility.GetBuildIndexByScenePath(path) >= 0;
    }

    static string PackLabel(FeaturePackManifest pack, bool selected)
    {
        var status = selected ? "Selected" : "Verified";
        if (!pack.SchemaVersion.HasValue || string.IsNullOrWhiteSpace(pack.Version))
        {
            status = "Invalid";
        }

        var name = string.IsNullOrWhiteSpace(pack.DisplayName) ? pack.Id : pack.DisplayName;
        return $"{name} ({status})";
    }

    static void EnsureCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        var cameraObject = new GameObject("ModSettings Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.10f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
    }

    static void ConfigureCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(920f, 620f);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 18, 18);
        layout.spacing = 10;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        return panel;
    }

    static void CreateHeader(Transform parent, string text)
    {
        CreateText(parent, text, 24, FontStyle.Bold);
    }

    static void CreateBodyText(Transform parent, string text)
    {
        CreateText(parent, text, 13, FontStyle.Normal);
    }

    static Dropdown CreateDropdown(Transform parent, string label)
    {
        CreateBodyText(parent, label);
        var go = new GameObject(label + " Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.22f, 1f);
        var dropdown = go.GetComponent<Dropdown>();
        var labelText = CreateText(go.transform, string.Empty, 14, FontStyle.Normal);
        labelText.alignment = TextAnchor.MiddleLeft;
        Stretch(labelText.rectTransform, 12, 0, -28, 0);
        dropdown.captionText = labelText;
        CreateDropdownTemplate(go.transform, dropdown);
        LayoutElement(go, 34);
        return dropdown;
    }

    static void CreateDropdownTemplate(Transform parent, Dropdown dropdown)
    {
        var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        template.transform.SetParent(parent, false);
        var templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.sizeDelta = new Vector2(0, 150);
        template.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(template.transform, false);
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;

        var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image));
        item.transform.SetParent(content.transform, false);
        item.GetComponent<Image>().color = new Color(0.24f, 0.24f, 0.24f, 1f);
        LayoutElement(item, 26);

        var itemLabel = CreateText(item.transform, "Option", 13, FontStyle.Normal);
        itemLabel.alignment = TextAnchor.MiddleLeft;
        Stretch(itemLabel.rectTransform, 10, 0, -10, 0);
        var toggle = item.GetComponent<Toggle>();
        toggle.targetGraphic = item.GetComponent<Image>();

        var scroll = template.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;

        template.SetActive(false);
        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
    }

    static ScrollRect CreateScroll(Transform parent)
    {
        var go = new GameObject("Pack Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 1f);
        LayoutElement(go, 132);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(go.transform, false);
        Stretch(viewport.GetComponent<RectTransform>(), 0, 0, 0, 0);
        viewport.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 1f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = new Vector2(12, 0);
        contentRect.offsetMax = new Vector2(-12, 0);
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        var scroll = go.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.horizontal = false;
        return scroll;
    }

    static GameObject CreateHorizontal(Transform parent)
    {
        var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        LayoutElement(go, 30);
        return go;
    }

    static Button CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(text + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.32f, 0.32f, 0.32f, 1f);
        var button = go.GetComponent<Button>();
        button.onClick.AddListener(action);
        var label = CreateText(go.transform, text, 14, FontStyle.Bold);
        label.alignment = TextAnchor.MiddleCenter;
        Stretch(label.rectTransform, 0, 0, 0, 0);
        LayoutElement(go, 40);
        return button;
    }

    static Text CreateText(Transform parent, string text, int size, FontStyle style)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.fontStyle = style;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement(go, Math.Max(24, size + 8));
        return label;
    }

    static void LayoutElement(GameObject go, float height)
    {
        LayoutElement(go, height, -1, 0);
    }

    static void LayoutElement(GameObject go, float height, float width, float flexibleHeight)
    {
        var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (height > 0)
        {
            element.minHeight = height;
            element.preferredHeight = height;
        }

        if (width > 0)
        {
            element.preferredWidth = width;
        }

        element.flexibleHeight = flexibleHeight;
    }

    static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, -top);
    }
}
