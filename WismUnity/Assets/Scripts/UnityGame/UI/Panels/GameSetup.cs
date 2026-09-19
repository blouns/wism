using Assets.Scripts.Managers;
using Assets.Scripts.UnityGame.ModKit;
using Assets.Scripts.UnityGame.Persistance.Entities;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wism.Client.Core;
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
    private static readonly PlayerRole[] PlayerRoles =
    {
        new PlayerRole("Human", null),
        new PlayerRole("Knight", AiDifficultyTier.Knight),
        new PlayerRole("Baron", AiDifficultyTier.Baron),
        new PlayerRole("Lord", AiDifficultyTier.Lord),
        new PlayerRole("Warlord", AiDifficultyTier.Warlord)
    };
    private static readonly string[] PlayerRoleLabels = PlayerRoles.Select(role => role.Label).ToArray();
    private readonly Sprite[] playerRoleSprites = new Sprite[PlayerRoles.Length];
    private int[] playerRoleIndexes = Array.Empty<int>();
    private bool isInitializing;
    private readonly Dictionary<string, ClanChoice> clanChoices = new Dictionary<string, ClanChoice>(StringComparer.OrdinalIgnoreCase);
    private Text[][] clanLabels;
    private Text[][] roleLabels;
    private int clanPage;
    private GameObject clanPager;
    private Text pageLabel;
    private Button previousPage;
    private Button nextPage;
    private string rosterError;
    private static Dictionary<string, ClanChoice> pendingChoices;
    private static bool pendingCombat;

    private sealed class ClanChoice
    {
        public bool Selected;
        public int Role;
        public ClanChoice Copy() => new ClanChoice { Selected = Selected, Role = Role };
    }

    private int PageSize => availableClans.Length > playerToggles.Length ? playerToggles.Length - 1 : playerToggles.Length;
    private int ClanIndex(int row) => clanPage * PageSize + row;

    private readonly struct PlayerRole
    {
        public PlayerRole(string label, AiDifficultyTier? difficulty)
        {
            Label = label;
            Difficulty = difficulty;
        }

        public string Label { get; }
        public AiDifficultyTier? Difficulty { get; }
    }

    public void Start()
    {
        WismUiInputAdapter.ConfigureGameEventSystem();
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
        EnsureOptionToggles();
        EnsureInteractionContracts();
        CachePlayerLabels();
        if (pendingChoices != null)
        {
            foreach (var pair in pendingChoices) clanChoices[pair.Key] = pair.Value.Copy();
            GameObject.Find("ShowAiCombatToggle").GetComponent<Toggle>().SetIsOnWithoutNotify(pendingCombat);
            pendingChoices = null;
        }
        RefreshAvailableClans();
        EnsurePlayerRoleState();
        ConfigurePlayerRows(resetSelection: true);
        WirePlayerRowEvents();
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

        var selectedWorldName = GetWorldNameFromPanel();
        if (string.Equals(this.worldName, selectedWorldName, StringComparison.OrdinalIgnoreCase))
        {
            UpdateStartValidation();
            return;
        }

        this.worldName = selectedWorldName;
        if (UnityModKitRuntimeSelection.HasSelection &&
            !string.Equals(UnityModKitRuntimeSelection.CurrentSelection.World, this.worldName, StringComparison.OrdinalIgnoreCase))
        {
            UnityModKitRuntimeSelection.Clear();
        }

        RefreshAvailableClans();
        var startClans = LoadStartClanNames(this.worldName);
        foreach (var clan in availableClans)
            if (clanChoices.TryGetValue(clan.ShortName, out var choice))
                choice.Selected = startClans.Contains(clan.ShortName);
        ConfigurePlayerRows(resetSelection: true);
        UpdateStartValidation();
    }

    public void ModSettingsButton()
    {
        if (!UnityModKitRuntimeSelection.HasSelection)
        {
            var report = UnityModKitSelection.Inspect(ModularGameProfileCatalog.DefaultProfileId,
                Array.Empty<string>(), worldName, UnityModKitSelection.PluginModRoot);
            if (report.isLoadable) UnityModKitRuntimeSelection.Set(report);
        }
        pendingChoices = clanChoices.ToDictionary(pair => pair.Key, pair => pair.Value.Copy(), StringComparer.OrdinalIgnoreCase);
        pendingCombat = GetToggleValue("ShowAiCombatToggle", true);
        SceneManager.LoadScene(ModSettingsScene);
    }

    private void LoadGame()
    {
        var scene = ResolvePlayableScene(this.worldName, false) ?? ResolvePlayableScene("Illuria", false);
        if (scene == null)
        {
            UpdateValidationText(GameSetupValidation.Invalid("No game scene is available for loading."));
            return;
        }
        UnityNewGameEntity settings = new UnityNewGameEntity();
        settings.IsNewGame = false;
        settings.InteractiveUI = true;
        UnityManager.SetNewGameSettings(settings);
        SceneManager.LoadScene(scene);
    }

    private void StartNewGame(UnityNewGameEntity settings)
    {
        UnityManager.SetNewGameSettings(settings);

        LoadScene(settings.WorldName);
    }

    private void LoadScene(string worldName)
    {
        var scene = ResolvePlayableScene(worldName);
        if (scene == null) return;
        SceneManager.LoadScene(scene);
    }

    private static string ResolvePlayableScene(string world, bool useSelection = true)
    {
        var selected = useSelection ? ResolveSelectedUnityScene() : null;
        return UnityWorldSceneCatalog.Resolve(world, selected);
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
        if (!string.IsNullOrEmpty(rosterError)) return GameSetupValidation.Invalid(rosterError);
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

        if (ResolvePlayableScene(settings.WorldName) == null)
            return GameSetupValidation.Invalid("This world is not included in this build.");

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
        settings.ShowAiCombat = GetToggleValue("ShowAiCombatToggle", defaultValue: true);
        settings.IsNewGame = true;
        settings.RandomSeed = 0;
        UnityModKitRuntimeSelection.ApplyTo(settings);

        return settings;
    }

    private UnityPlayerEntity[] GetSelectedPlayersFromPanel()
    {
        var playerEntities = new List<UnityPlayerEntity>();
        for (int i = 0; i < availableClans.Length; i++)
        {
            if (clanChoices.TryGetValue(availableClans[i].ShortName, out var choice) && choice.Selected)
            {
                var playerEntity = new UnityPlayerEntity();
                var role = PlayerRoles[choice.Role];
                playerEntity.IsHuman = !role.Difficulty.HasValue;
                playerEntity.AiDifficulty = role.Difficulty;
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
                .Where(clan => clan.Playable && !string.Equals(clan.ShortName, "Neutral", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            rosterError = null;
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var clan in availableClans)
            {
                if (string.IsNullOrWhiteSpace(clan.ShortName) || !ids.Add(clan.ShortName))
                    throw new InvalidOperationException("Each clan needs a unique, nonempty ShortName.");
                if (string.IsNullOrWhiteSpace(clan.DisplayName))
                    throw new InvalidOperationException("Clan " + clan.ShortName + " needs a display name.");
                if (!TryClanColor(clan.PrimaryColor ?? clan.Color, out _) ||
                    !TryClanColor(clan.SecondaryColor ?? "(0, 0, 0)", out _))
                    throw new InvalidOperationException("Clan " + clan.ShortName + " needs RGB colors between 0 and 255.");
            }
            clanPage = Mathf.Clamp(clanPage, 0, Math.Max(0, (availableClans.Length - 1) / PageSize));
        }
        catch (Exception ex)
        {
            rosterError = "Could not load clans: " + ex.Message;
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

    private void ConfigurePlayerRows(bool resetSelection)
    {
        var startClans = LoadStartClanNames(this.worldName);
        foreach (var clan in availableClans)
            if (!clanChoices.ContainsKey(clan.ShortName))
                clanChoices[clan.ShortName] = new ClanChoice { Selected = startClans.Contains(clan.ShortName) };
        // World changes exclude clans without a capital, but do not erase their role.
        if (resetSelection)
            foreach (var clan in availableClans)
                if (!startClans.Contains(clan.ShortName)) clanChoices[clan.ShortName].Selected = false;
        for (int i = 0; i < this.playerToggles.Length; i++)
        {
            var toggle = this.playerToggles[i];
            var index = ClanIndex(i);
            var hasClan = i < PageSize && index < availableClans.Length;
            toggle.gameObject.SetActive(hasClan);
            toggle.interactable = hasClan;
            toggle.SetIsOnWithoutNotify(hasClan && clanChoices[availableClans[index].ShortName].Selected);
            var labels = clanLabels[i];
            foreach (var label in labels)
            {
                label.supportRichText = false;
                label.text = hasClan ? availableClans[index].DisplayName : string.Empty;
                if (hasClan)
                {
                    var clan = availableClans[index];
                    // The authored shadow precedes the foreground in canvas draw order.
                    TryClanColor(labels.Length > 1 && label == labels[0]
                        ? clan.SecondaryColor ?? "(0, 0, 0)" : clan.PrimaryColor ?? clan.Color, out var color);
                    label.color = color;
                }
            }
            FitClanLabels(labels, GetPlayerRoleIcon(i));
            playerRoleIndexes[i] = hasClan ? clanChoices[availableClans[index].ShortName].Role : 0;
            SetPlayerRoleLabel(i);
        }
        UpdateClanPager();
    }

    private void CachePlayerLabels()
    {
        roleLabels = playerToggles.Select(toggle => toggle.GetComponentsInChildren<Text>(true)
            .Where(text => PlayerRoleLabels.Contains(NormalizeRoleLabelText(text.text), StringComparer.OrdinalIgnoreCase)).ToArray()).ToArray();
        clanLabels = playerToggles.Select((toggle, row) => toggle.GetComponentsInChildren<Text>(true)
            .Except(roleLabels[row]).ToArray()).ToArray();
    }

    private static bool TryClanColor(string value, out Color color)
    {
        color = Color.black;
        if (!ClanInfo.TryParseRgb(value, out var r, out var g, out var b)) return false;
        color = new Color32(r, g, b, 255);
        return true;
    }

    private void UpdateClanPager()
    {
        bool paged = availableClans.Length > playerToggles.Length;
        if (paged && clanPager == null)
        {
            var last = playerToggles[playerToggles.Length - 1].GetComponent<RectTransform>();
            clanPager = new GameObject("ClanPager", typeof(RectTransform));
            var rect = clanPager.GetComponent<RectTransform>();
            rect.SetParent(last.parent, false);
            rect.anchorMin = last.anchorMin;
            rect.anchorMax = last.anchorMax;
            rect.pivot = last.pivot;
            rect.anchoredPosition = last.anchoredPosition;
            rect.sizeDelta = new Vector2(440f, last.rect.height);
            previousPage = CreatePageButton("PreviousClans", "<", -130f, -1);
            nextPage = CreatePageButton("NextClans", ">", 130f, 1);
            var label = new GameObject("ClanPageLabel", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(rect, false);
            pageLabel = label.GetComponent<Text>();
            pageLabel.font = clanLabels[0][0].font;
            pageLabel.fontSize = 24;
            pageLabel.color = Color.black;
            pageLabel.alignment = TextAnchor.MiddleCenter;
            pageLabel.raycastTarget = false;
            pageLabel.rectTransform.sizeDelta = new Vector2(180f, 44f);
        }
        if (clanPager == null) return;
        clanPager.SetActive(paged);
        if (!paged) return;
        previousPage.interactable = clanPage > 0;
        nextPage.interactable = (clanPage + 1) * PageSize < availableClans.Length;
        previousPage.GetComponentInChildren<Text>().color = previousPage.interactable ? Color.black : Color.gray;
        nextPage.GetComponentInChildren<Text>().color = nextPage.interactable ? Color.black : Color.gray;
        pageLabel.text = $"{clanPage * PageSize + 1}-{Math.Min((clanPage + 1) * PageSize, availableClans.Length)} / {availableClans.Length}";
    }

    private Button CreatePageButton(string name, string text, float x, int direction)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(clanPager.transform, false);
        rect.sizeDelta = new Vector2(60f, 44f);
        rect.anchoredPosition = new Vector2(x, 0f);
        var image = obj.GetComponent<Image>();
        var style = GameObject.Find("LoadButton").GetComponent<Image>();
        image.sprite = style.sprite;
        image.type = style.type;
        image.color = style.color;
        var button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            clanPage = Mathf.Clamp(clanPage + direction, 0, (availableClans.Length - 1) / PageSize);
            ConfigurePlayerRows(false);
        });
        var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(rect, false);
        label.rectTransform.sizeDelta = rect.sizeDelta;
        label.font = clanLabels[0][0].font;
        label.fontSize = 28;
        label.color = Color.black;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = text;
        label.raycastTarget = false;
        WismHitTargetPolicy.Apply(obj);
        WismUiControl.Ensure(obj, "game-setup." + name, WismUiControlRole.Navigation, "game-setup.page-clans", 30);
        return button;
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

        AlignShortcutLabelStack(buttonObject);
        foreach (var label in buttonObject.GetComponentsInChildren<Text>(true))
        {
            label.supportRichText = true;
            label.color = Color.black;
            label.text = $"<color=#b80000>{shortcut}</color>{rest}";
        }
    }

    private void EnsureModSettingsButton()
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
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(ModSettingsButton);

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
        WismHitTargetPolicy.Apply(buttonObject);
        WismUiControl.Ensure(
            buttonObject,
            "game-setup.mods",
            WismUiControlRole.Navigation,
            "game-setup.open-mods",
            20);
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

    private static void AlignShortcutLabelStack(GameObject buttonObject)
    {
        var labels = buttonObject.GetComponentsInChildren<Text>(true);
        if (labels.Length < 2)
        {
            return;
        }

        var reference = labels[0].rectTransform;
        for (int i = 1; i < labels.Length; i++)
        {
            var rect = labels[i].rectTransform;
            rect.anchorMin = reference.anchorMin;
            rect.anchorMax = reference.anchorMax;
            rect.pivot = reference.pivot;
            rect.anchoredPosition = reference.anchoredPosition;
            rect.sizeDelta = reference.sizeDelta;
            labels[i].alignment = labels[0].alignment;
        }
    }

    private static void EnsureOptionToggles()
    {
        var randomStartToggle = GameObject.Find("RandomStartToggle")?.GetComponent<Toggle>();
        if (randomStartToggle != null)
        {
            randomStartToggle.SetIsOnWithoutNotify(false);
            randomStartToggle.interactable = false;
        }

        var interactiveToggle = GameObject.Find("InteractiveToggle")?.GetComponent<Toggle>();
        if (interactiveToggle != null)
        {
            interactiveToggle.SetIsOnWithoutNotify(true);
            interactiveToggle.interactable = false;

            if (GameObject.Find("ShowAiCombatToggle") == null)
            {
                var combatToggle = Instantiate(interactiveToggle, interactiveToggle.transform.parent, false);
                combatToggle.name = "ShowAiCombatToggle";
                combatToggle.onValueChanged = new Toggle.ToggleEvent();
                combatToggle.group = null;
                combatToggle.interactable = true;
                combatToggle.SetIsOnWithoutNotify(true);
                var rect = combatToggle.GetComponent<RectTransform>();
                var reference = interactiveToggle.GetComponent<RectTransform>();
                rect.anchoredPosition = reference.anchoredPosition - new Vector2(0f, reference.rect.height + 8f);
                foreach (var label in combatToggle.GetComponentsInChildren<Text>(true))
                {
                    label.text = "Show AI combat";
                    label.color = Color.black;
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 20;
                    label.resizeTextMaxSize = label.fontSize;
                }
            }
        }
    }

    private static bool GetToggleValue(string objectName, bool defaultValue)
    {
        var toggle = GameObject.Find(objectName)?.GetComponent<Toggle>();
        return toggle == null ? defaultValue : toggle.isOn;
    }

    private void EnsurePlayerRoleState()
    {
        if (this.playerRoleIndexes.Length == this.playerToggles.Length)
        {
            return;
        }

        this.playerRoleIndexes = new int[this.playerToggles.Length];
        for (int i = 0; i < PlayerRoles.Length; i++)
        {
            this.playerRoleSprites[i] = Resources.Load<Sprite>("UI/PlayerRoles/" + PlayerRoles[i].Label);
            if (this.playerRoleSprites[i] == null)
                throw new InvalidOperationException("Missing player role portrait: " + PlayerRoles[i].Label);
        }
    }

    private void WirePlayerRowEvents()
    {
        for (int i = 0; i < this.playerToggles.Length; i++)
        {
            var rowIndex = i;
            this.playerToggles[i].onValueChanged.AddListener(value =>
            {
                var index = ClanIndex(rowIndex);
                if (!isInitializing && rowIndex < PageSize && index < availableClans.Length)
                    clanChoices[availableClans[index].ShortName].Selected = value;
                OnPlayerSelectionChange();
            });
            foreach (var text in GetRoleTexts(rowIndex))
            {
                // Preserve the label's left edge while removing its invisible
                // overlap with the settings column.
                var rect = text.rectTransform;
                var left = rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 160f);
                rect.anchoredPosition = new Vector2(left + 160f * rect.pivot.x, rect.anchoredPosition.y);
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 24;
                text.resizeTextMaxSize = text.fontSize;
                text.raycastTarget = true;
                var button = text.GetComponent<Button>() ?? text.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                button.targetGraphic = text;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => CyclePlayerRole(rowIndex));
                WismHitTargetPolicy.Apply(button.gameObject);
                WismUiControl.Ensure(
                    button.gameObject,
                    $"game-setup.player-{rowIndex + 1}.role",
                    WismUiControlRole.Selection,
                    "game-setup.cycle-player-role",
                    30);
            }
            var icon = GetPlayerRoleIcon(i);
            if (icon != null)
            {
                icon.raycastTarget = true;
                var button = icon.GetComponent<Button>() ?? icon.gameObject.AddComponent<Button>();
                button.targetGraphic = icon;
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => CyclePlayerRole(rowIndex));
                WismHitTargetPolicy.Apply(button.gameObject);
                WismUiControl.Ensure(button.gameObject, $"game-setup.player-{i + 1}.role-icon",
                    WismUiControlRole.Selection, "game-setup.cycle-player-role", 30);
            }
        }
    }

    private void EnsureInteractionContracts()
    {
        WismUiSurface.Ensure(
            this.gameObject,
            "game-setup",
            WismUiControlState.Normal,
            WismUiControlState.Selected,
            WismUiControlState.Disabled);

        EnsureNamedControl("StartButton", "game-setup.start", "game-setup.start", WismUiControlRole.Command, 40);
        EnsureNamedControl("WorldDropdown", "game-setup.world", "game-setup.select-world", WismUiControlRole.Selection, 20);
        EnsureNamedControl("LoadButton", "game-setup.load", "game-setup.load", WismUiControlRole.Navigation, 20);
        EnsureNamedControl(ModSettingsButtonName, "game-setup.mods", "game-setup.open-mods", WismUiControlRole.Navigation, 20);
        EnsureNamedControl("RandomStartToggle", "game-setup.random-start", "game-setup.toggle-random-start", WismUiControlRole.Toggle, 20);
        EnsureNamedControl("InteractiveToggle", "game-setup.interactive", "game-setup.toggle-interactive", WismUiControlRole.Toggle, 20);
        EnsureNamedControl("ShowAiCombatToggle", "game-setup.show-ai-combat", "game-setup.toggle-ai-combat", WismUiControlRole.Toggle, 20);

        for (var i = 0; i < this.playerToggles.Length; i++)
        {
            var toggle = this.playerToggles[i];
            WismHitTargetPolicy.Apply(toggle.gameObject);
            WismUiControl.Ensure(
                toggle.gameObject,
                $"game-setup.player-{i + 1}.enabled",
                WismUiControlRole.Toggle,
                "game-setup.toggle-player",
                20);
        }
    }

    private static void EnsureNamedControl(
        string objectName,
        string semanticId,
        string actionId,
        WismUiControlRole role,
        int priority)
    {
        var target = GameObject.Find(objectName);
        if (target == null)
        {
            return;
        }

        WismHitTargetPolicy.Apply(target);
        WismUiControl.Ensure(target, semanticId, role, actionId, priority);
    }

    private void OnPlayerSelectionChange()
    {
        if (isInitializing)
        {
            return;
        }

        UpdateStartValidation();
    }

    private void CyclePlayerRole(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= this.playerRoleIndexes.Length)
        {
            return;
        }

        this.playerRoleIndexes[playerIndex] = (this.playerRoleIndexes[playerIndex] + 1) % PlayerRoleLabels.Length;
        var index = ClanIndex(playerIndex);
        if (playerIndex >= PageSize || index >= availableClans.Length) return;
        clanChoices[availableClans[index].ShortName].Role = playerRoleIndexes[playerIndex];
        SetPlayerRoleLabel(playerIndex);
        UpdateStartValidation();
    }

    private int GetPlayerRoleIndex(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < this.playerRoleIndexes.Length
            ? this.playerRoleIndexes[playerIndex]
            : 0;
    }

    private void SetPlayerRoleLabel(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= this.playerToggles.Length)
        {
            return;
        }

        var roleIndex = GetPlayerRoleIndex(playerIndex);
        var role = PlayerRoles[roleIndex];
        foreach (var text in GetRoleTexts(playerIndex))
        {
            text.text = role.Label;
        }
        var icon = GetPlayerRoleIcon(playerIndex);
        if (icon != null)
        {
            icon.overrideSprite = null;
            icon.sprite = this.playerRoleSprites[roleIndex];
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
        }
    }

    private static void FitClanLabels(Text[] labels, Image icon)
    {
        if (labels.Length == 0 || icon == null) return;
        var iconLeft = icon.rectTransform.TransformPoint(new Vector3(icon.rectTransform.rect.xMin, 0, 0));
        var width = labels.Min(label =>
        {
            var rect = label.rectTransform;
            var left = rect.localPosition.x + rect.rect.xMin;
            return rect.parent.InverseTransformPoint(iconLeft).x - 8f - left;
        });
        width = Mathf.Max(1f, width);
        var maxFontSize = labels.Min(label => label.fontSize);
        // Keep foreground and shadow on identical text geometry and font sizing.
        foreach (var label in labels)
        {
            var rect = label.rectTransform;
            var position = rect.localPosition;
            var left = position.x + rect.rect.xMin;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.localPosition = new Vector3(left - rect.rect.xMin, position.y, position.z);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMaxSize = maxFontSize;
            label.resizeTextMinSize = Mathf.Min(20, maxFontSize);
        }
    }

    private Image GetPlayerRoleIcon(int playerIndex)
    {
        var row = this.playerToggles[playerIndex].transform;
        return row.GetComponentsInChildren<Image>(true)
            .FirstOrDefault(image => image.transform.parent == row && image.name.StartsWith("Image", StringComparison.Ordinal));
    }

    private IEnumerable<Text> GetRoleTexts(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= this.playerToggles.Length)
        {
            return Enumerable.Empty<Text>();
        }

        return roleLabels[playerIndex];
    }

    private static string NormalizeRoleLabelText(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Trim();
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



