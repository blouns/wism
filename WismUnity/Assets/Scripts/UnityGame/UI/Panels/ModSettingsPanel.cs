using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.UnityGame.ModKit;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wism.Client.Modules.Profiles;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class ModSettingsPanel : MonoBehaviour
{
    const string GameSetupScene = "GameSetup";
    static readonly Color PanelColor = new Color(0.48f, 0.48f, 0.46f, 0.98f);
    static readonly Color FieldColor = new Color(0.86f, 0.86f, 0.84f, 1f);
    static readonly Color RowColor = new Color(0.38f, 0.38f, 0.36f, 1f);
    static readonly Color RowSelectedColor = new Color(0.52f, 0.54f, 0.44f, 1f);
    static readonly Color TextColor = new Color(0.03f, 0.03f, 0.03f, 1f);
    static readonly Color GreenColor = new Color(0.00f, 0.46f, 0.12f, 1f);
    static readonly Color RedColor = new Color(0.78f, 0.00f, 0.00f, 1f);
    static readonly Color MutedTextColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    readonly HashSet<string> selectedPacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    Dropdown profileDropdown;
    Dropdown worldDropdown;
    Text statusText;
    Text detailText;
    Text packHintText;
    Text worldDetailText;
    RawImage worldPreviewImage;
    Transform packList;
    Button continueButton;
    string[] profileIds = new string[0];
    string[] worldIds = new string[0];
    FeaturePackManifest[] packs = new FeaturePackManifest[0];
    UnityModKitSelectionReport currentReport;
    bool isRefreshing;
    bool initializedSelection;

    readonly struct PackHealthStatus
    {
        public PackHealthStatus(string label, Color color, string reason)
        {
            Label = label;
            Color = color;
            Reason = reason;
        }

        public string Label { get; }
        public Color Color { get; }
        public string Reason { get; }
    }

    void Start()
    {
        BuildUi();
        Refresh();
    }

    void BuildUi()
    {
        EnsureCamera();
        EnsureEventSystem();
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
        CreateBodyText(root.transform, "Choose a profile, world, and feature packs before starting a new game.");

        profileDropdown = CreateDropdown(root.transform, "Profile");
        profileDropdown.gameObject.name = "ProfileDropdown";
        profileDropdown.onValueChanged.AddListener(_ => OnProfileChanged());

        worldDropdown = CreateDropdown(root.transform, "World");
        worldDropdown.gameObject.name = "WorldDropdown";
        worldDropdown.onValueChanged.AddListener(_ => Evaluate());

        var worldDetails = CreateWorldDetails(root.transform);
        worldDetails.gameObject.name = "WorldDetails";

        CreateBodyText(root.transform, "Feature Packs");
        var scroll = CreateScroll(root.transform);
        scroll.gameObject.name = "PackScroll";
        packList = scroll.content;
        packList.gameObject.name = "PackList";
        packHintText = CreateText(root.transform, "Click a feature pack row or checkbox to select or unselect it.", 12, FontStyle.Normal);
        packHintText.gameObject.name = "PackHintText";
        packHintText.color = MutedTextColor;
        packHintText.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement(packHintText.gameObject, 32);

        statusText = CreateText(root.transform, "Status", 16, FontStyle.Bold);
        statusText.gameObject.name = "StatusText";
        LayoutElement(statusText.gameObject, 24);
        detailText = CreateText(root.transform, string.Empty, 13, FontStyle.Normal);
        detailText.gameObject.name = "DetailText";
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.verticalOverflow = VerticalWrapMode.Truncate;
        LayoutElement(detailText.gameObject, 62);

        var actions = CreateHorizontal(root.transform);
        actions.gameObject.name = "ActionsRow";
        LayoutElement(actions, 38);
        CreateButton(actions.transform, "Back", Back).gameObject.name = "BackButton";
        CreateButton(actions.transform, "Refresh", Refresh).gameObject.name = "RefreshButton";
        continueButton = CreateButton(actions.transform, "Continue", Continue);
        continueButton.gameObject.name = "ContinueButton";
    }

    void Refresh()
    {
        isRefreshing = true;
        var previousProfile = Current(profileDropdown, profileIds);
        var previousWorld = Current(worldDropdown, worldIds);
        var previousPacks = new HashSet<string>(selectedPacks, StringComparer.OrdinalIgnoreCase);

        profileIds = DiscoverIds(Path.Combine(UnityModKitSelection.PluginModRoot, "Profiles"), "classic-warlords");
        worldIds = DiscoverIds(Path.Combine(UnityModKitSelection.PluginModRoot, "Worlds"), "Mini-Illuria");
        packs = ModularGameProfileCatalog.DiscoverPacksFromModRoot(UnityModKitSelection.PluginModRoot).ToArray();

        var profile = profileIds.Contains(previousProfile, StringComparer.OrdinalIgnoreCase)
            ? previousProfile
            : "classic-warlords";
        ResetDropdown(profileDropdown, profileIds, profile);

        if (!initializedSelection)
        {
            ApplyProfileDefaults(profile);
            previousWorld = DefaultWorldForProfile(profile);
            initializedSelection = true;
        }
        else
        {
            selectedPacks.Clear();
            foreach (var packId in previousPacks.Where(PackExists))
            {
                selectedPacks.Add(packId);
            }
        }

        var world = worldIds.Contains(previousWorld, StringComparer.OrdinalIgnoreCase)
            ? previousWorld
            : DefaultWorldForProfile(profile);
        ResetDropdown(worldDropdown, worldIds, world);
        RebuildPackRows();
        isRefreshing = false;
        Evaluate();
    }

    void OnProfileChanged()
    {
        if (isRefreshing)
        {
            return;
        }

        var profile = Current(profileDropdown, profileIds);
        ApplyProfileDefaults(profile);
        ResetDropdown(worldDropdown, worldIds, DefaultWorldForProfile(profile));
        RebuildPackRows();
        Evaluate();
    }

    void ApplyProfileDefaults(string profile)
    {
        selectedPacks.Clear();
        foreach (var packId in DefaultPacksForProfile(profile).Where(PackExists))
        {
            selectedPacks.Add(packId);
        }
    }

    void RebuildPackRows()
    {
        foreach (Transform child in packList)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        foreach (var pack in packs)
        {
            var health = PackHealth(pack);
            var row = CreatePackRow(packList, pack, health);
            var toggle = row.GetComponent<Toggle>();
            var check = row.transform.Find("PackCheck").GetComponent<Text>();
            var name = row.transform.Find("PackName").GetComponent<Text>();
            var state = row.transform.Find("PackState").GetComponent<Text>();
            var background = row.GetComponent<Image>();

            void UpdateRow(bool selected)
            {
                check.text = selected ? "[x]" : "[ ]";
                name.text = PackDisplayName(pack) + "\n" + PackDescription(pack);
                state.text = health.Label;
                state.color = health.Color;
                background.color = selected ? RowSelectedColor : RowColor;
            }

            row.GetComponent<PackRowHint>().Configure(
                () => SetPackHint(PackHint(pack, health)),
                () => SetPackHint("Click a feature pack row or checkbox to select or unselect it."));

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

                UpdateRow(selected);
                SetPackHint($"{PackDisplayName(pack)}: {(selected ? "selected" : "not selected")}. {PackDescription(pack)} {health.Reason}");
                Evaluate();
            });

            var isSelected = selectedPacks.Contains(pack.Id);
            toggle.SetIsOnWithoutNotify(isSelected);
            UpdateRow(isSelected);
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

        var sceneLabel = ResolveWorldSceneLabel(world, currentReport.unityScene);
        var sceneAvailable = !string.IsNullOrWhiteSpace(sceneLabel);
        var green = currentReport.isGreen && sceneAvailable;
        statusText.text = green
            ? "Green: verified stack can start a new game."
            : "Red: fix selection before continuing.";
        statusText.color = green ? GreenColor : RedColor;
        detailText.text = BuildDetail(sceneAvailable, sceneLabel);
        UpdateWorldDetails(world);
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

    string BuildDetail(bool sceneAvailable, string sceneLabel)
    {
        var lines = new List<string>
        {
            "Profile: " + currentReport.profileId,
            "World: " + currentReport.worldName,
            "Feature Packs: " + (currentReport.activePackIds == null || currentReport.activePackIds.Length == 0
                ? "None"
                : string.Join(", ", currentReport.activePackIds)),
            "Compatibility: " + currentReport.compatibilityStatus,
            "Fingerprint: " + (currentReport.contentFingerprint ?? string.Empty),
            "Scene: " + (sceneAvailable ? sceneLabel : "Missing")
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
        if (currentReport == null ||
            !currentReport.isGreen ||
            string.IsNullOrWhiteSpace(ResolveWorldSceneLabel(currentReport.worldName, currentReport.unityScene)))
        {
            return;
        }

        UnityModKitRuntimeSelection.Set(currentReport);
        SceneManager.LoadScene(GameSetupScene);
    }

    void Back()
    {
        UnityModKitRuntimeSelection.Clear();
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

    static string ResolveWorldSceneLabel(string world, string unityScene)
    {
        foreach (var path in CandidateWorldScenes(world, unityScene))
        {
            if (IsSceneAvailable(path))
            {
                return path;
            }
        }

        return string.Empty;
    }

    static IEnumerable<string> CandidateWorldScenes(string world, string unityScene)
    {
        yield return "Assets/Scenes/" + world + ".unity";
        yield return "Assets/Scenes/Test/" + world + ".unity";

        if (!string.IsNullOrWhiteSpace(unityScene) && ScenePathMatchesWorld(unityScene, world))
        {
            yield return unityScene;
        }
    }

    static bool ScenePathMatchesWorld(string scenePath, string world)
    {
        var fileName = Path.GetFileNameWithoutExtension(scenePath.Replace('\\', '/'));
        return string.Equals(fileName, world, StringComparison.OrdinalIgnoreCase);
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

    static string PackDisplayName(FeaturePackManifest pack)
    {
        return string.IsNullOrWhiteSpace(pack.DisplayName) ? pack.Id : pack.DisplayName;
    }

    static PackHealthStatus PackHealth(FeaturePackManifest pack)
    {
        if (!pack.SchemaVersion.HasValue && string.IsNullOrWhiteSpace(pack.Version))
        {
            return new PackHealthStatus("Invalid", RedColor, "Missing schemaVersion and version metadata, so this pack is loadable but not Green verified.");
        }

        if (!pack.SchemaVersion.HasValue)
        {
            return new PackHealthStatus("Invalid", RedColor, "Missing schemaVersion metadata, so this pack is loadable but not Green verified.");
        }

        if (string.IsNullOrWhiteSpace(pack.Version))
        {
            return new PackHealthStatus("Invalid", RedColor, "Missing version metadata, so this pack is loadable but not Green verified.");
        }

        return new PackHealthStatus("Verified", GreenColor, "Green verification metadata is present.");
    }

    bool PackExists(string packId)
    {
        return packs.Any(pack => string.Equals(pack.Id, packId, StringComparison.OrdinalIgnoreCase));
    }

    static string[] DefaultPacksForProfile(string profile)
    {
        try
        {
            var selection = ModularGameProfileCatalog.ResolveFromModRoot(
                UnityModKitSelection.PluginModRoot,
                string.IsNullOrWhiteSpace(profile) ? "classic-warlords" : profile,
                null);
            return selection.PackIds.ToArray();
        }
        catch
        {
            return ReadProfileArray(profile, "enabledPacks");
        }
    }

    static string DefaultWorldForProfile(string profile)
    {
        try
        {
            var selection = ModularGameProfileCatalog.ResolveFromModRoot(
                UnityModKitSelection.PluginModRoot,
                string.IsNullOrWhiteSpace(profile) ? "classic-warlords" : profile,
                null);
            return !string.IsNullOrWhiteSpace(selection.Launch.World)
                ? selection.Launch.World
                : selection.BaseWorld;
        }
        catch
        {
            var launchWorld = ReadProfileString(profile, "launch", "world");
            if (!string.IsNullOrWhiteSpace(launchWorld))
            {
                return launchWorld;
            }

            var baseWorld = ReadProfileString(profile, "baseWorld");
            return string.IsNullOrWhiteSpace(baseWorld) ? "Mini-Illuria" : baseWorld;
        }
    }

    static string[] ReadProfileArray(string profile, string property)
    {
        try
        {
            var token = LoadProfileToken(profile);
            return token[property] is JArray values
                ? values.Select(value => value.ToString()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray()
                : Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    static string ReadProfileString(string profile, string property)
    {
        try
        {
            return LoadProfileToken(profile)[property]?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    static string ReadProfileString(string profile, string parent, string property)
    {
        try
        {
            return LoadProfileToken(profile)[parent]?[property]?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    static JObject LoadProfileToken(string profile)
    {
        var profileId = string.IsNullOrWhiteSpace(profile) ? "classic-warlords" : profile;
        var path = Path.Combine(UnityModKitSelection.PluginModRoot, "Profiles", profileId, "profile.json");
        return JObject.Parse(File.ReadAllText(path));
    }

    static string PackDescription(FeaturePackManifest pack)
    {
        return string.IsNullOrWhiteSpace(pack.Description)
            ? "No description provided."
            : pack.Description;
    }

    static string PackHint(FeaturePackManifest pack, PackHealthStatus health)
    {
        return $"{PackDisplayName(pack)}: {PackDescription(pack)} {health.Reason}";
    }

    void SetPackHint(string message)
    {
        if (packHintText != null)
        {
            packHintText.text = message;
        }
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
        camera.backgroundColor = new Color(0.20f, 0.20f, 0.20f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Outline));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        panel.GetComponent<Image>().color = PanelColor;
        var outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.10f, 0.10f, 0.10f, 1f);
        outline.effectDistance = new Vector2(4f, -4f);
        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 12, 12);
        layout.spacing = 5;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        return panel;
    }

    static void CreateHeader(Transform parent, string text)
    {
        var header = CreateText(parent, text, 22, FontStyle.Bold);
        LayoutElement(header.gameObject, 28);
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
        go.GetComponent<Image>().color = FieldColor;
        var dropdown = go.GetComponent<Dropdown>();
        var labelText = CreateText(go.transform, string.Empty, 14, FontStyle.Normal);
        labelText.alignment = TextAnchor.MiddleLeft;
        Stretch(labelText.rectTransform, 12, 0, -28, 0);
        dropdown.captionText = labelText;
        CreateDropdownTemplate(go.transform, dropdown);
        LayoutElement(go, 34);
        return dropdown;
    }

    GameObject CreateWorldDetails(Transform parent)
    {
        var row = new GameObject("World Details Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        LayoutElement(row, 72);

        var previewFrame = new GameObject("WorldPreviewFrame", typeof(RectTransform), typeof(Image));
        previewFrame.transform.SetParent(row.transform, false);
        previewFrame.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1f);
        LayoutElement(previewFrame, 72, 120, 0);

        var preview = new GameObject("WorldPreview", typeof(RectTransform), typeof(RawImage));
        preview.transform.SetParent(previewFrame.transform, false);
        Stretch(preview.GetComponent<RectTransform>(), 6, 6, -6, 6);
        worldPreviewImage = preview.GetComponent<RawImage>();
        worldPreviewImage.color = Color.white;
        worldPreviewImage.raycastTarget = false;

        worldDetailText = CreateText(row.transform, string.Empty, 12, FontStyle.Normal);
        worldDetailText.gameObject.name = "WorldDetailText";
        worldDetailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        worldDetailText.verticalOverflow = VerticalWrapMode.Truncate;
        LayoutElement(worldDetailText.gameObject, 72, 680, 0);

        return row;
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
        template.GetComponent<Image>().color = FieldColor;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(template.transform, false);
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = FieldColor;
        viewportImage.raycastTarget = false;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
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
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image));
        item.transform.SetParent(content.transform, false);
        item.GetComponent<Image>().color = new Color(0.70f, 0.70f, 0.68f, 1f);
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
        go.GetComponent<Image>().color = RowColor;
        LayoutElement(go, 108);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(go.transform, false);
        Stretch(viewport.GetComponent<RectTransform>(), 0, 0, 0, 0);
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = RowColor;
        viewportImage.raycastTarget = false;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
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
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = go.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.horizontal = false;
        return scroll;
    }

    static GameObject CreatePackRow(Transform parent, FeaturePackManifest pack, PackHealthStatus health)
    {
        var row = new GameObject("PackToggle:" + pack.Id, typeof(RectTransform), typeof(Image), typeof(Toggle), typeof(HorizontalLayoutGroup), typeof(PackRowHint));
        row.transform.SetParent(parent, false);
        var rowImage = row.GetComponent<Image>();
        rowImage.color = RowColor;
        rowImage.raycastTarget = true;
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 0, 0);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        LayoutElement(row, 42);

        var toggle = row.GetComponent<Toggle>();
        toggle.targetGraphic = row.GetComponent<Image>();

        var check = CreateText(row.transform, "[ ]", 13, FontStyle.Bold);
        check.gameObject.name = "PackCheck";
        check.alignment = TextAnchor.MiddleLeft;
        LayoutElement(check.gameObject, 26, 34, 0);

        var name = CreateText(row.transform, PackDisplayName(pack) + "\n" + PackDescription(pack), 12, FontStyle.Normal);
        name.gameObject.name = "PackName";
        name.alignment = TextAnchor.MiddleLeft;
        name.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement(name.gameObject, 42, 520, 0);

        var state = CreateText(row.transform, health.Label, 13, FontStyle.Bold);
        state.gameObject.name = "PackState";
        state.alignment = TextAnchor.MiddleRight;
        state.color = health.Color;
        LayoutElement(state.gameObject, 26, 110, 0);

        return row;
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
        go.GetComponent<Image>().color = new Color(0.34f, 0.34f, 0.32f, 1f);
        var button = go.GetComponent<Button>();
        button.onClick.AddListener(action);
        var label = CreateText(go.transform, text, 14, FontStyle.Bold);
        label.alignment = TextAnchor.MiddleCenter;
        Stretch(label.rectTransform, 0, 0, 0, 0);
        LayoutElement(go, 36);
        return button;
    }

    static Text CreateText(Transform parent, string text, int size, FontStyle style)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.text = text;
        label.font = ResolveFont();
        label.fontSize = size;
        label.fontStyle = style;
        label.color = TextColor;
        label.alignByGeometry = true;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement(go, Math.Max(24, size + 8));
        return label;
    }

    static Font resolvedFont;

    static Font ResolveFont()
    {
        if (resolvedFont != null)
        {
            return resolvedFont;
        }

        resolvedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                       Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (resolvedFont != null && resolvedFont.material != null && resolvedFont.material.mainTexture != null)
        {
            resolvedFont.material.mainTexture.filterMode = FilterMode.Bilinear;
        }

        return resolvedFont;
    }

    void UpdateWorldDetails(string world)
    {
        if (worldDetailText == null || worldPreviewImage == null)
        {
            return;
        }

        var summary = LoadWorldSummary(world);
        worldDetailText.text = summary.Description;
        worldPreviewImage.texture = summary.Preview;
    }

    static WorldSummary LoadWorldSummary(string world)
    {
        var worldRoot = Path.Combine(UnityModKitSelection.PluginModRoot, "Worlds", world ?? string.Empty);
        var cityPath = Path.Combine(worldRoot, "City.json");
        var locationPath = Path.Combine(worldRoot, "Location.json");
        var mapPath = Path.Combine(worldRoot, "Map.json");
        var cityCount = CountJsonArray(cityPath);
        var locationCount = CountJsonArray(locationPath);
        var map = LoadMapTiles(mapPath);
        var ownedStartCount = CountOwnedStarts(cityPath);
        var preview = BuildMapPreview(map);
        var dimensions = map.Width > 0 && map.Height > 0
            ? $"{map.Width} x {map.Height}"
            : "Unknown size";
        var description = $"{world}\nMap: {dimensions}    Cities: {cityCount}    Sites: {locationCount}\nStarting clans: {ownedStartCount}. Start is blocked when selected clans do not have city starts on this world.";
        return new WorldSummary(description, preview);
    }

    static int CountJsonArray(string path)
    {
        try
        {
            return File.Exists(path) ? JArray.Parse(File.ReadAllText(path)).Count : 0;
        }
        catch
        {
            return 0;
        }
    }

    static int CountOwnedStarts(string cityPath)
    {
        try
        {
            if (!File.Exists(cityPath))
            {
                return 0;
            }

            var clans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var city in JArray.Parse(File.ReadAllText(cityPath)).OfType<JObject>())
            {
                var clan = city["ClanName"]?.ToString();
                if (!string.IsNullOrWhiteSpace(clan) && !string.Equals(clan, "Neutral", StringComparison.OrdinalIgnoreCase))
                {
                    clans.Add(clan);
                }
            }

            return clans.Count;
        }
        catch
        {
            return 0;
        }
    }

    static MapPreviewData LoadMapTiles(string mapPath)
    {
        var tiles = new List<MapPreviewTile>();
        try
        {
            if (!File.Exists(mapPath))
            {
                return new MapPreviewData(tiles, 0, 0);
            }

            var token = JToken.Parse(File.ReadAllText(mapPath));
            var tileArray = token.Type == JTokenType.Array ? (JArray)token : token["Tiles"] as JArray;
            if (tileArray == null)
            {
                return new MapPreviewData(tiles, 0, 0);
            }

            foreach (var tile in tileArray.OfType<JObject>())
            {
                tiles.Add(new MapPreviewTile(
                    tile["X"]?.Value<int>() ?? 0,
                    tile["Y"]?.Value<int>() ?? 0,
                    tile["TerrainShortName"]?.ToString() ?? string.Empty));
            }
        }
        catch
        {
            return new MapPreviewData(new List<MapPreviewTile>(), 0, 0);
        }

        var width = tiles.Count == 0 ? 0 : tiles.Max(tile => tile.X) + 1;
        var height = tiles.Count == 0 ? 0 : tiles.Max(tile => tile.Y) + 1;
        return new MapPreviewData(tiles, width, height);
    }

    static Texture2D BuildMapPreview(MapPreviewData map)
    {
        var width = Mathf.Max(1, map.Width);
        var height = Mathf.Max(1, map.Height);
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        var pixels = Enumerable.Repeat(new Color(0.22f, 0.22f, 0.22f, 1f), width * height).ToArray();

        foreach (var tile in map.Tiles)
        {
            var y = height - 1 - Mathf.Clamp(tile.Y, 0, height - 1);
            var x = Mathf.Clamp(tile.X, 0, width - 1);
            pixels[x + y * width] = TerrainColor(tile.Terrain);
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    static Color TerrainColor(string terrain)
    {
        var value = terrain ?? string.Empty;
        if (value.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new Color(0.05f, 0.24f, 0.58f, 1f);
        }

        if (value.IndexOf("mount", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new Color(0.55f, 0.55f, 0.55f, 1f);
        }

        if (value.IndexOf("forest", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new Color(0.03f, 0.34f, 0.10f, 1f);
        }

        if (value.IndexOf("road", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new Color(0.58f, 0.55f, 0.50f, 1f);
        }

        return new Color(0.10f, 0.52f, 0.14f, 1f);
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

    readonly struct WorldSummary
    {
        public WorldSummary(string description, Texture2D preview)
        {
            Description = description;
            Preview = preview;
        }

        public string Description { get; }
        public Texture2D Preview { get; }
    }

    readonly struct MapPreviewData
    {
        public MapPreviewData(List<MapPreviewTile> tiles, int width, int height)
        {
            Tiles = tiles;
            Width = width;
            Height = height;
        }

        public List<MapPreviewTile> Tiles { get; }
        public int Width { get; }
        public int Height { get; }
    }

    readonly struct MapPreviewTile
    {
        public MapPreviewTile(int x, int y, string terrain)
        {
            X = x;
            Y = y;
            Terrain = terrain;
        }

        public int X { get; }
        public int Y { get; }
        public string Terrain { get; }
    }
}

public sealed class PackRowHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Action enter;
    Action exit;

    public void Configure(Action onEnter, Action onExit)
    {
        enter = onEnter;
        exit = onExit;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        enter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        exit?.Invoke();
    }
}
