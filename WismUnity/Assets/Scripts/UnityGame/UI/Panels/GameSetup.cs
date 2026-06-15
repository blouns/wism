using Assets.Scripts.Managers;
using Assets.Scripts.UnityGame.ModKit;
using Assets.Scripts.UnityGame.Persistance.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Modules.Profiles;
using Toggle = UnityEngine.UI.Toggle;

public class GameSetup : MonoBehaviour
{
    private const string ModSettingsScene = "ModSettings";
    private const string ModSettingsButtonName = "AdvancedModsButton";
    private const string ValidationTextName = "GameSetupValidationText";

    [SerializeField]
    private Toggle[] playerToggles;
    [SerializeField]
    private int nextScene;

    private string worldName;
    private Button startButton;
    private Text validationText;
    private ClanInfo[] availableClans = new ClanInfo[0];
    private bool isInitializing;

    public void Start()
    {
        if (this.playerToggles == null || this.playerToggles.Length == 0)
        {
            throw new InvalidOperationException("Must have at least one player.");
        }

        isInitializing = true;
        try
        {
            this.worldName = UnityModKitRuntimeSelection.HasSelection
                ? UnityModKitRuntimeSelection.CurrentSelection.World
                : ResolveDefaultWorldName();
            EnsureWorldDropdownOptions(this.worldName);
            EnsureDefaultModKitSelection();
            if (UnityModKitRuntimeSelection.HasSelection)
            {
                this.worldName = UnityModKitRuntimeSelection.CurrentSelection.World;
                TrySelectWorldInPanel(this.worldName);
            }
        }
        finally
        {
            isInitializing = false;
        }

        this.startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        EnsureModSettingsButton();
        NormalizeShortcutLabels();
        EnsureValidationText();
        RefreshAvailableClans();
        ConfigurePlayerRows();
        UpdateStartValidation();
    }

    public void LoadButton()
    {
        LoadGame();
    }

    public void StartButton()
    {
        UnityNewGameEntity settings = GetGameSettings();

        if (!AreValidGameSettings(settings))
        {
            return;
        }

        StartNewGame(settings);
    }

    public void OnWorldChange()
    {
        if (isInitializing)
        {
            return;
        }

        this.worldName = GetWorldNameFromPanel();
        if (UnityModKitRuntimeSelection.HasSelection &&
            !string.Equals(UnityModKitRuntimeSelection.CurrentSelection.World, this.worldName, StringComparison.OrdinalIgnoreCase))
        {
            UnityModKitRuntimeSelection.Clear();
        }

        RefreshAvailableClans();
        ConfigurePlayerRows();
        UpdateStartValidation();
    }

    public void ModSettingsButton()
    {
        SceneManager.LoadScene(ModSettingsScene);
    }

    private void LoadGame()
    {
        UnityNewGameEntity settings = new UnityNewGameEntity();
        settings.IsNewGame = false;
        UnityManager.SetNewGameSettings(settings);

        LoadScene(this.worldName);
    }

    private void StartNewGame(UnityNewGameEntity settings)
    {
        UnityManager.SetNewGameSettings(settings);

        LoadScene(settings.WorldName);
    }

    private void LoadScene(string worldName)
    {
        var selectedScene = ResolveSelectedUnityScene();
        if (!string.IsNullOrWhiteSpace(selectedScene))
        {
            SceneManager.LoadScene(selectedScene);
            SceneManager.UnloadSceneAsync(1);
            return;
        }

        string scenePath = "Scenes/";

#if DEBUG
        if (worldName.Contains("Test"))
        {
            scenePath += "Test/";
        }
#endif

        SceneManager.LoadScene(scenePath + worldName);
        SceneManager.UnloadSceneAsync(1);
    }

    private static string ResolveSelectedUnityScene()
    {
        var unityScene = UnityModKitRuntimeSelection.LastReport == null
            ? string.Empty
            : UnityModKitRuntimeSelection.LastReport.unityScene;
        if (string.IsNullOrWhiteSpace(unityScene))
        {
            return string.Empty;
        }

        var normalized = unityScene.Replace('\\', '/');
        if (normalized.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - ".unity".Length);
        }

        return normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring("Assets/".Length)
            : normalized;
    }

    private bool AreValidGameSettings(UnityNewGameEntity settings)
    {
        var validation = ValidateGameSettings(settings);
        if (!validation.IsValid)
        {
            Debug.LogError(validation.Message);
        }

        UpdateValidationText(validation);
        return validation.IsValid;
    }

    private GameSetupValidation ValidateGameSettings(UnityNewGameEntity settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (settings.Players.Length < 2)
        {
            return GameSetupValidation.Invalid("Select at least two players.");
        }

        if (string.IsNullOrWhiteSpace(settings.WorldName))
        {
            return GameSetupValidation.Invalid("Choose a world.");
        }

        if (settings.ModKitSelection != null)
        {
            var report = UnityModKitSelection.Inspect(
                settings.ModKitSelection.ProfileId,
                settings.ModKitSelection.PackIds,
                settings.ModKitSelection.World,
                UnityModKitSelection.PluginModRoot);
            if (!report.isGreen)
            {
                return GameSetupValidation.Invalid(report.outcome);
            }

            ModFactory.ModPath = report.modRoot;
            ModFactory.WorldPath = report.worldName;
            ModFactory.ActiveFeaturePackIds = new List<string>(report.activePackIds);
            ModFactory.ResetCache();
        }
        else
        {
            ModFactory.ModPath = ResolveDefaultModRoot();
            ModFactory.WorldPath = settings.WorldName;
            ModFactory.ActiveFeaturePackIds = new List<string>();
            ModFactory.ResetCache();
        }

        // Load Mod cities for world and compare to number of players
        // Must have enough cities for all the players
        IList<CityInfo> cityInfos = null;
        try
        {
            cityInfos = ModFactory.LoadCityInfos(
            @$"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{settings.WorldName}");
        }
        catch
        {
            return GameSetupValidation.Invalid("Could not load the world: " + settings.WorldName);
        }

        if (cityInfos == null || cityInfos.Count < settings.Players.Length)
        {
            return GameSetupValidation.Invalid("World must have at least one city for each selected player.");
        }

        var startClans = new HashSet<string>(
            cityInfos
                .Where(city => !string.IsNullOrWhiteSpace(city.ClanName) && !string.Equals(city.ClanName, "Neutral", StringComparison.OrdinalIgnoreCase))
                .Select(city => city.ClanName),
            StringComparer.OrdinalIgnoreCase);
        var missingStartClans = settings.Players
            .Select(player => player.ClanName)
            .Where(clan => !startClans.Contains(clan))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missingStartClans.Length > 0)
        {
            return GameSetupValidation.Invalid("World has no starting city for: " + string.Join(", ", missingStartClans));
        }

        return GameSetupValidation.Valid("Ready.");
    }

    private UnityNewGameEntity GetGameSettings()
    {
        UnityNewGameEntity settings = new UnityNewGameEntity();
        settings.Players = GetSelectedPlayersFromPanel();
        settings.WorldName = this.worldName;
        settings.RandomStartLocations = false;
        settings.InteractiveUI = true;
        settings.IsNewGame = true;
        settings.RandomSeed = GameManager.DefaultRandom;
        UnityModKitRuntimeSelection.ApplyTo(settings);

        return settings;
    }

    private UnityPlayerEntity[] GetSelectedPlayersFromPanel()
    {
        var playerEntities = new List<UnityPlayerEntity>();
        for (int i = 0; i < this.playerToggles.Length; i++)
        {
            if (this.playerToggles[i].isOn)
            {
                var playerEntity = new UnityPlayerEntity();
                playerEntity.IsHuman = true;
                playerEntity.ClanName = GetClanName(i);
                playerEntities.Add(playerEntity);
            }
        }

        return playerEntities.ToArray();
    }

    private string GetClanName(int i)
    {
        if (i < 0 || i >= availableClans.Length)
        {
            throw new InvalidOperationException("No clan is available for player row " + i + ".");
        }

        return availableClans[i].ShortName;
    }

    private static string GetWorldNameFromPanel()
    {
        var dropdown = GameObject.Find("WorldDropdown")
            .GetComponent<Dropdown>();
        var index = dropdown.value;
        return dropdown.options[index].text;
    }

    private void SetWorldName(string worldName)
    {
        if (string.IsNullOrWhiteSpace(worldName))
        {
            throw new ArgumentException($"'{nameof(worldName)}' cannot be null or whitespace.", nameof(worldName));
        }

        this.worldName = GetWorldNameFromPanel();
    }

    private void RefreshAvailableClans()
    {
        ApplyCurrentModContext();
        try
        {
            availableClans = ModFactory.LoadClanInfos(ModFactory.ModPath)
                .Where(clan => !string.Equals(clan.ShortName, "Neutral", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
        catch (Exception ex)
        {
            Debug.LogError("Could not load clans: " + ex.Message);
            availableClans = new ClanInfo[0];
        }
    }

    private void ApplyCurrentModContext()
    {
        if (UnityModKitRuntimeSelection.HasSelection)
        {
            var report = UnityModKitSelection.Inspect(
                UnityModKitRuntimeSelection.CurrentSelection.ProfileId,
                UnityModKitRuntimeSelection.CurrentSelection.PackIds,
                UnityModKitRuntimeSelection.CurrentSelection.World,
                UnityModKitSelection.PluginModRoot);
            if (report.isLoadable)
            {
                ModFactory.ModPath = report.modRoot;
                ModFactory.WorldPath = report.worldName;
                ModFactory.ActiveFeaturePackIds = new List<string>(report.activePackIds);
                ModFactory.ResetCache();
                return;
            }
        }

        ModFactory.ModPath = ResolveDefaultModRoot();
        ModFactory.WorldPath = this.worldName;
        ModFactory.ActiveFeaturePackIds = new List<string>();
        ModFactory.ResetCache();
    }

    private void ConfigurePlayerRows()
    {
        var startClans = LoadStartClanNames(this.worldName);
        for (int i = 0; i < this.playerToggles.Length; i++)
        {
            var toggle = this.playerToggles[i];
            var hasClan = i < availableClans.Length;
            toggle.interactable = hasClan;
            toggle.isOn = hasClan && startClans.Contains(availableClans[i].ShortName);

            var label = toggle.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = hasClan ? availableClans[i].DisplayName : "Unavailable";
            }
        }
    }

    private HashSet<string> LoadStartClanNames(string world)
    {
        try
        {
            var cityInfos = ModFactory.LoadCityInfos(@$"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{world}");
            return new HashSet<string>(
                cityInfos
                    .Where(city => !string.IsNullOrWhiteSpace(city.ClanName) && !string.Equals(city.ClanName, "Neutral", StringComparison.OrdinalIgnoreCase))
                    .Select(city => city.ClanName),
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(availableClans.Take(Math.Min(2, availableClans.Length)).Select(clan => clan.ShortName), StringComparer.OrdinalIgnoreCase);
        }
    }

    private void UpdateStartValidation()
    {
        var validation = ValidateGameSettings(GetGameSettings());
        if (this.startButton != null)
        {
            this.startButton.interactable = validation.IsValid;
        }

        UpdateValidationText(validation);
    }

    private void UpdateValidationText(GameSetupValidation validation)
    {
        if (validationText == null)
        {
            return;
        }

        validationText.text = validation.Message;
        validationText.color = validation.IsValid ? new Color(0f, 0.35f, 0.08f, 1f) : new Color(0.70f, 0f, 0f, 1f);
    }

    private void EnsureValidationText()
    {
        var existing = GameObject.Find(ValidationTextName);
        if (existing != null)
        {
            validationText = existing.GetComponent<Text>();
            return;
        }

        var startButtonObject = GameObject.Find("StartButton");
        if (startButtonObject == null)
        {
            return;
        }

        var textObject = new GameObject(ValidationTextName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(startButtonObject.transform.parent, false);
        validationText = textObject.GetComponent<Text>();
        validationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        validationText.fontSize = 14;
        validationText.fontStyle = FontStyle.Bold;
        validationText.alignment = TextAnchor.MiddleCenter;
        validationText.raycastTarget = false;

        var rect = textObject.GetComponent<RectTransform>();
        var startRect = startButtonObject.GetComponent<RectTransform>();
        rect.anchorMin = startRect.anchorMin;
        rect.anchorMax = startRect.anchorMax;
        rect.pivot = startRect.pivot;
        rect.sizeDelta = new Vector2(520f, 28f);
        rect.anchoredPosition = startRect.anchoredPosition + new Vector2(-155f, 34f);
    }

    private static void NormalizeShortcutLabels()
    {
        SetShortcutLabel("StartButton", "S", "tart");
        SetShortcutLabel(ModSettingsButtonName, "M", "ods...");
    }

    private static void SetShortcutLabel(string objectName, string shortcut, string rest)
    {
        var buttonObject = GameObject.Find(objectName);
        if (buttonObject == null)
        {
            return;
        }

        foreach (var label in buttonObject.GetComponentsInChildren<Text>(true))
        {
            label.supportRichText = true;
            label.color = Color.black;
            label.text = $"<color=#b80000>{shortcut}</color>{rest}";
        }
    }

    private static void EnsureModSettingsButton()
    {
        if (GameObject.Find(ModSettingsButtonName) != null)
        {
            return;
        }

        var loadButtonObject = GameObject.Find("LoadButton");
        var startButtonObject = GameObject.Find("StartButton");
        if (loadButtonObject == null || startButtonObject == null)
        {
            return;
        }

        var buttonObject = Instantiate(loadButtonObject, loadButtonObject.transform.parent, false);
        buttonObject.name = ModSettingsButtonName;

        var button = buttonObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SceneManager.LoadScene(ModSettingsScene));

        foreach (var label in buttonObject.GetComponentsInChildren<Text>(true))
        {
            label.supportRichText = true;
            label.color = Color.black;
            label.text = "<color=#b80000>M</color>ods...";
        }

        var rect = buttonObject.GetComponent<RectTransform>();
        var loadRect = loadButtonObject.GetComponent<RectTransform>();
        var startRect = startButtonObject.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(
            (loadRect.anchoredPosition.x + startRect.anchoredPosition.x) / 2f,
            loadRect.anchoredPosition.y);
        rect.sizeDelta = new Vector2(180f, loadRect.sizeDelta.y);
    }

    private static void EnsureDefaultModKitSelection()
    {
        if (UnityModKitRuntimeSelection.HasSelection)
        {
            return;
        }

        var report = UnityModKitSelection.Inspect(
            ModularGameProfileCatalog.DefaultProfileId,
            null,
            string.Empty,
            UnityModKitSelection.PluginModRoot);
        if (report.isLoadable)
        {
            UnityModKitRuntimeSelection.Set(report);
        }
    }

    private static string ResolveDefaultWorldName()
    {
        try
        {
            var selection = ModularGameProfileCatalog.ResolveFromModRoot(
                UnityModKitSelection.PluginModRoot,
                ModularGameProfileCatalog.DefaultProfileId,
                null);
            if (!string.IsNullOrWhiteSpace(selection.Launch.World))
            {
                return selection.Launch.World;
            }

            if (!string.IsNullOrWhiteSpace(selection.BaseWorld))
            {
                return selection.BaseWorld;
            }
        }
        catch
        {
            // Fall back to the compiled Unity default if plugin MOD data is unavailable.
        }

        return GameManager.DefaultWorld;
    }

    private static string ResolveDefaultModRoot()
    {
        return Directory.Exists(UnityModKitSelection.PluginModRoot)
            ? UnityModKitSelection.PluginModRoot
            : GameManager.DefaultModPath;
    }

    private static void EnsureWorldDropdownOptions(string preferredWorld)
    {
        var dropdownObject = GameObject.Find("WorldDropdown");
        if (dropdownObject == null)
        {
            return;
        }

        var dropdown = dropdownObject.GetComponent<Dropdown>();
        if (dropdown == null)
        {
            return;
        }

        var worldsRoot = Path.Combine(ResolveDefaultModRoot(), ModFactory.WorldsPath);
        if (!Directory.Exists(worldsRoot))
        {
            TrySelectWorldInPanel(preferredWorld);
            return;
        }

        var worlds = Directory.GetDirectories(worldsRoot)
            .Select(Path.GetFileName)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (worlds.Count == 0)
        {
            TrySelectWorldInPanel(preferredWorld);
            return;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(worlds);
        var index = worlds.FindIndex(world =>
            string.Equals(world, preferredWorld, StringComparison.OrdinalIgnoreCase));
        dropdown.value = Math.Max(0, index);
        dropdown.RefreshShownValue();
    }

    private static void TrySelectWorldInPanel(string worldName)
    {
        var dropdownObject = GameObject.Find("WorldDropdown");
        if (dropdownObject == null)
        {
            return;
        }

        var dropdown = dropdownObject.GetComponent<Dropdown>();
        if (dropdown == null)
        {
            return;
        }

        var index = dropdown.options.FindIndex(option =>
            string.Equals(option.text, worldName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return;
        }

        dropdown.value = index;
        dropdown.RefreshShownValue();
    }

    private readonly struct GameSetupValidation
    {
        private GameSetupValidation(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }

        public bool IsValid { get; }
        public string Message { get; }

        public static GameSetupValidation Valid(string message)
        {
            return new GameSetupValidation(true, message);
        }

        public static GameSetupValidation Invalid(string message)
        {
            return new GameSetupValidation(false, message);
        }
    }
}
