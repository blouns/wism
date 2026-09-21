using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Assets.Scripts.UI
{
    public class CityProduction : MonoBehaviour
    {
        [SerializeField]
        private Button[] armyButtons;

        [SerializeField]
        private Button prodButton;
        [SerializeField]
        private Button locButton;
        [SerializeField]
        private Button stopButton;
        [SerializeField]
        private Button exitButton;


        private UnityManager unityManager;
        private ArmyManager armyManager;
        private int armySelectedIndex;
        private City productionCity;
        private ProductionInfo[] productionInfos;
        private Player managementPlayer;
        private ProductionPanelMode panelMode;
        private ProductionManagementViewModel viewModel;
        private bool selectingDestination;
        private Text cityText;
        private Text statusText;
        private RectTransform managementSummary;
        private RectTransform pickerContent;
        private float pickerHeight;
        private RectTransform jumpRow;
        private Button destinationJumpButton;
        private Button[] sourceJumpButtons;
        private MinimapCityOverlay destinationOverlay;
        private Player productionPlayer;
        private Wism.Client.Core.Armies.ArmyInTraining displayedTraining;

        public void LateUpdate()
        {
            if (this.productionCity != null && !this.selectingDestination &&
                this.unityManager.ProductionMode == ProductionMode.CitySelected &&
                this.productionCity.Barracks.ArmyInTraining != this.displayedTraining)
                InitializeProduction();
            FitPickerToPanel();
            if (this.armyButtons != null &&
                this.armySelectedIndex >= 0 &&
                this.armySelectedIndex < this.armyButtons.Length)
            {
                this.armyButtons[this.armySelectedIndex].Select();
            }
        }

        public void Initialize(UnityManager unityManager, City city)
        {
            if (unityManager is null)
            {
                throw new ArgumentNullException(nameof(unityManager));
            }

            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            this.productionCity = city;
            this.productionPlayer = city.Player;
            this.unityManager = unityManager;
            this.armyManager = unityManager.GetComponent<ArmyManager>();
            this.armySelectedIndex = -1;
            this.managementPlayer = null;
            this.panelMode = ProductionPanelMode.SingleCity;
            this.selectingDestination = false;

            InitializeProduction();
        }

        public void InitializeManagement(UnityManager unityManager, Player player, City selectedCity = null)
        {
            if (unityManager is null)
            {
                throw new ArgumentNullException(nameof(unityManager));
            }

            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            this.unityManager = unityManager;
            this.armyManager = unityManager.GetComponent<ArmyManager>();
            this.armySelectedIndex = -1;
            this.managementPlayer = player;
            this.productionPlayer = player;
            this.panelMode = ProductionPanelMode.Management;
            this.selectingDestination = false;
            this.productionCity = selectedCity ?? player.Capitol ?? player.GetCities()[0];

            InitializeProduction();
        }

        private void InitializeProduction()
        {
            CancelDestination();
            EnsurePickerLayout();
            this.transform.SetAsLastSibling();
            EnsureInteractionContracts();
            SetInitialButtonState();

            this.viewModel = this.panelMode == ProductionPanelMode.Management
                ? ProductionPanelViewModelBuilder.BuildManagement(this.managementPlayer, this.productionCity)
                : ProductionPanelViewModelBuilder.BuildSingleCity(this.productionCity);
            this.productionCity = this.viewModel.SelectedCity.City;
            EnsureDynamicControls();
            RefreshDynamicControls();

            var barracks = this.productionCity.Barracks;

            // Unpack the army infos for each production slot
            this.productionInfos = barracks.GetProductionKinds().ToArray();
            for (int i = 0; i < this.productionInfos.Length; i++)
            {
                InitializeProductionSlot(i);
            }

            InitializeCurrentProduction();
            this.unityManager.InputManager.SetInputMode(InputMode.UI);
        }

        private void InitializeCurrentProduction()
        {
            this.displayedTraining = this.productionCity.Barracks.ArmyInTraining;
            string turnsRemainingString = "None";

            var barracks = this.productionCity.Barracks;
            if (barracks.ProducingArmy())
            {
                // Set image
                SetArmyImageOnGameObject(
                    Game.Current.GetCurrentPlayer().Clan,
                    barracks.ArmyInTraining.ArmyInfo,
                    "CurrentArmyKind");
                var currentArmyKind = this.pickerContent.Find("CurrentArmyKind");
                if (currentArmyKind != null)
                {
                    currentArmyKind.gameObject.SetActive(true);
                }

                turnsRemainingString = barracks.ArmyInTraining.TurnsToProduce + "t";
            }
            else
            {
                var currentArmyKind = this.pickerContent.Find("CurrentArmyKind");
                if (currentArmyKind != null)
                {
                    currentArmyKind.gameObject.SetActive(false);
                }
            }

            // Set turns remaining text
            var turnsRemaining = this.pickerContent.Find("TurnsRemainingText");
            if (turnsRemaining != null)
            {
                var turnsText = turnsRemaining.GetComponent<Text>();
                turnsText.text = turnsRemainingString;
            }
        }

        private void SetArmyImageOnGameObject(Clan clan, ArmyInfo info, string gameObjectName)
        {
            var imageTransform = this.pickerContent.Find(gameObjectName);
            if (imageTransform == null)
            {
                return;
            }

            var armyPrefab = this.armyManager.FindGameObjectKind(clan, info);
            SpriteRenderer spriteRenderer = armyPrefab.GetComponent<SpriteRenderer>();
            var image = imageTransform.GetComponent<Image>();
            image.sprite = spriteRenderer.sprite;
        }

        private void SetInitialButtonState()
        {
            this.prodButton.interactable = false;
            this.locButton.interactable = false;
            this.stopButton.interactable = this.productionCity != null && this.productionCity.Barracks.ProducingArmy();
            this.exitButton.interactable = true;
            ClearProduction();
        }

        private void InitializeProductionSlot(int index)
        {
            if (this.armyButtons == null || index >= this.armyButtons.Length)
            {
                return;
            }

            ArmyInfo armyInfo = ModFactory.FindArmyInfo(this.productionInfos[index].ArmyInfoName);

            // Set image
            var clan = Game.Current.GetCurrentPlayer().Clan;
            var armyPrefab = this.armyManager.FindGameObjectKind(clan, armyInfo);
            SpriteRenderer spriteRenderer = armyPrefab.GetComponent<SpriteRenderer>();
            var image = this.armyButtons[index].gameObject.transform.Find("ArmyKind")
                .GetComponent<Image>();
            image.sprite = spriteRenderer.sprite;

            // Set production info
            Text productionText = this.armyButtons[index].gameObject.transform.Find("ArmyInfo")
                .GetComponent<Text>();
            productionText.text = $"{this.productionInfos[index].TurnsToProduce}t / {this.productionInfos[index].Upkeep}gp";

            this.armyButtons[index].gameObject.SetActive(true);
        }

        private void ClearProduction()
        {
            if (this.armyButtons == null)
            {
                return;
            }

            for (int i = 0; i < this.armyButtons.Length; i++)
            {
                this.armyButtons[i].gameObject.SetActive(false);
            }
        }

        public void OnArmy1Click()
        {
            this.armySelectedIndex = 0;
            EnableProduction();
        }

        public void OnArmy2Click()
        {
            this.armySelectedIndex = 1;
            EnableProduction();
        }

        public void OnArmy3Click()
        {
            this.armySelectedIndex = 2;
            EnableProduction();
        }

        public void OnArmy4Click()
        {
            this.armySelectedIndex = 3;
            EnableProduction();
        }

        public void OnProdClick()
        {
            if (this.armySelectedIndex < 0 || this.productionInfos == null ||
                this.armySelectedIndex >= this.productionInfos.Length || !this.prodButton.interactable)
            {
                return;
            }
            if (StartProduction()) OnExitClick();
        }

        private ArmyInfo SelectedArmy => this.productionInfos != null && this.armySelectedIndex >= 0 &&
            this.armySelectedIndex < this.productionInfos.Length
            ? ModFactory.FindArmyInfo(this.productionInfos[this.armySelectedIndex].ArmyInfoName) : null;

        private bool StartProduction(City destinationCity = null)
        {
            var armyInfo = SelectedArmy;
            if (this.productionCity.Player != this.productionPlayer ||
                this.productionPlayer != Game.Current.GetCurrentPlayer() ||
                Game.Current.GameState == GameState.GameOver ||
                !this.productionCity.Barracks.CanStartProduction(armyInfo, destinationCity))
            {
                SetStatus("Production unavailable at this destination.");
                return false;
            }

            Debug.Log($"Starting production of {armyInfo.DisplayName}" +
                $" on {this.productionCity}" +
                $" to {(destinationCity == null ? this.productionCity : destinationCity)}");

            this.unityManager.GameManager
                .StartProduction(this.productionCity, armyInfo, destinationCity);
            return true;
        }

        public void OnLocClick()
        {
            if (this.selectingDestination) { CancelDestination(); return; }
            if (!Barracks.CanVectorArmy(SelectedArmy)) return;
            if (this.armySelectedIndex < 0)
            {
                SetStatus("Choose an army first.");
                return;
            }

            if (Game.Current.GetCurrentPlayer().GetCities().Count <= 1)
            {
                SetStatus("No other owned city can receive production.");
                return;
            }

            this.selectingDestination = true;
            this.destinationOverlay = UnityUtilities.GameObjectHardFind("Minimap")?.GetComponentInChildren<MinimapCityOverlay>();
            if (this.destinationOverlay != null)
            {
                this.destinationOverlay.ColorOverride = DestinationColor;
                this.destinationOverlay.Refresh();
            }
            this.locButton.GetComponentInChildren<Text>().text = "Back";
            this.unityManager.SetProductionMode(ProductionMode.SelectDestination);
            this.unityManager.InputManager.SetInputMode(InputMode.Game);
            SetStatus("Choose a destination city.");
        }

        public void SelectDestination(City destinationCity)
        {
            if (!this.selectingDestination)
            {
                return;
            }

            if (!ProductionPanelEntryPolicy.IsOwnedCity(destinationCity, Game.Current.GetCurrentPlayer()?.Clan))
            {
                SetStatus("Choose an owned destination city.");
                return;
            }

            if (!StartProduction(destinationCity == this.productionCity ? null : destinationCity)) return;
            CancelDestination();
            RefreshAfterMutation();
        }

        public Color32 DestinationColor(City city)
        {
            if (city == null || city.Player != this.productionPlayer || city.Tile?.City != city)
                return new Color32(128, 128, 128, 255);
            if (city == this.productionCity) return Color.yellow;
            return this.productionCity.Barracks.CanRouteProductionTo(SelectedArmy, city) ? Color.white : Color.red;
        }

        public void CancelDestination()
        {
            ClearDestinationOverlay();
            if (!this.selectingDestination) return;
            this.selectingDestination = false;
            this.locButton.GetComponentInChildren<Text>().text = "Loc";
            this.unityManager.SetProductionMode(ProductionMode.CitySelected);
            this.unityManager.InputManager.SetInputMode(InputMode.UI);
            SetStatus(string.Empty);
        }

        private void ClearDestinationOverlay()
        {
            if (this.destinationOverlay == null) return;
            if (this.destinationOverlay.ColorOverride == DestinationColor)
                this.destinationOverlay.ColorOverride = null;
            this.destinationOverlay.Refresh();
            this.destinationOverlay = null;
        }

        private void OnDisable() => CancelDestination();

        public void OnStopClick()
        {
            CancelDestination();
            Debug.Log($"Stopping production on {this.productionCity}");
            this.unityManager.GameManager
                .StopProduction(this.productionCity);

            DisableProduction();
            RefreshAfterMutation();
        }

        public void OnExitClick()
        {
            CancelDestination();
            this.armySelectedIndex = -1;
            this.selectingDestination = false;
            this.unityManager.InputManager.SetInputMode(InputMode.Game);
            this.unityManager.SetProductionMode(ProductionMode.None);
            this.gameObject.SetActive(false);
        }

        private void EnableProduction()
        {
            CancelDestination();
            this.prodButton.interactable = true;
            this.locButton.interactable = Barracks.CanVectorArmy(SelectedArmy) &&
                Game.Current.GetCurrentPlayer().GetCities().Count > 1;
        }

        private void DisableProduction()
        {
            this.prodButton.interactable = false;
            this.locButton.interactable = false;
        }

        public void OnNextCityClick()
        {
            MoveManagementSelection(1);
        }

        public void OnPreviousCityClick()
        {
            MoveManagementSelection(-1);
        }

        public ProductionPanelMode GetPanelMode()
        {
            return this.panelMode;
        }

        public ProductionManagementViewModel GetViewModel()
        {
            return this.viewModel;
        }

        public bool IsSelectingDestination()
        {
            return this.selectingDestination;
        }

        private void MoveManagementSelection(int delta)
        {
            if (this.panelMode != ProductionPanelMode.Management || this.viewModel == null || this.viewModel.Cities.Count == 0)
            {
                return;
            }

            var count = this.viewModel.Cities.Count;
            var nextIndex = (this.viewModel.SelectedCityIndex + delta + count) % count;
            this.productionCity = this.viewModel.Cities[nextIndex].City;
            this.armySelectedIndex = -1;
            InitializeProduction();
        }

        private void RefreshAfterMutation()
        {
            InitializeProduction();
        }

        private void EnsureDynamicControls()
        {
            // Both entry points reuse this component, but management chrome must never cover the picker.
            bool management = this.panelMode == ProductionPanelMode.Management;
            if (this.managementSummary != null)
            {
                this.managementSummary.gameObject.SetActive(management);
            }

            if (!management)
            {
                return;
            }

            if (this.managementSummary != null)
            {
                return;
            }

            var panel = WismUiFactory.CreateVerticalPanel(this.transform, "WismProductionPanelSummary");
            this.managementSummary = panel;
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(0f, 44f);
            var background = panel.GetComponent<Image>();
            background.sprite = GetComponent<Image>().sprite;
            background.color = Color.white;
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 4f;
            layout.childControlHeight = true;

            var row = WismUiFactory.CreateRow(panel, "ProductionNavigationRow");
            ConfigureManagementRow(row);
            var previous = CreateManagementButton(
                row,
                "PreviousProductionCityButton",
                "Prev",
                "owned-production.previous-city",
                "production.management.previous",
                WismUiControlRole.Navigation,
                10);
            previous.onClick.AddListener(OnPreviousCityClick);
            this.cityText = CreateManagementText(row, "ProductionCityText", flexible: true);
            this.statusText = CreateManagementText(row, "ProductionStatusText", flexible: false);
            var next = CreateManagementButton(
                row,
                "NextProductionCityButton",
                "Next",
                "owned-production.next-city",
                "production.management.next",
                WismUiControlRole.Navigation,
                10);
            next.onClick.AddListener(OnNextCityClick);

            this.jumpRow = WismUiFactory.CreateRow(panel, "ProductionJumpRow");
            ConfigureManagementRow(this.jumpRow);
            this.destinationJumpButton = CreateManagementButton(
                this.jumpRow,
                "DestinationProductionCityButton",
                "To",
                "owned-production.destination",
                "production.management.destination",
                WismUiControlRole.Navigation,
                20);
            this.destinationJumpButton.onClick.AddListener(OnDestinationJumpClick);
            this.sourceJumpButtons = new Button[4];
            for (var i = 0; i < this.sourceJumpButtons.Length; i++)
            {
                var sourceIndex = i;
                this.sourceJumpButtons[i] = CreateManagementButton(
                    this.jumpRow,
                    $"SourceProductionCityButton{i + 1}",
                    "From",
                    $"owned-production.source-{i + 1}",
                    "production.management.source",
                    WismUiControlRole.Navigation,
                    20);
                this.sourceJumpButtons[i].onClick.AddListener(() => OnSourceJumpClick(sourceIndex));
            }

        }

        private static void ConfigureManagementRow(RectTransform row)
        {
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            var size = row.gameObject.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = 36f;
        }

        private Text CreateManagementText(Transform parent, string name, bool flexible)
        {
            var text = WismUiFactory.CreateText(parent, name, string.Empty, 22);
            var original = this.exitButton.GetComponentInChildren<Text>(true);
            text.font = original.font;
            text.color = original.color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 22;
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 120f;
            layout.preferredWidth = flexible ? 320f : 180f;
            layout.flexibleWidth = flexible ? 1f : 0f;
            return text;
        }

        private Button CreateManagementButton(Transform parent, string name, string label,
            string semanticId, string actionId, WismUiControlRole role, int priority)
        {
            var button = Instantiate(this.exitButton, parent, false);
            button.name = name;
            // Cloning the classic control preserves its sprites/font, but never its Exit callback.
            button.onClick = new Button.ButtonClickedEvent();
            var text = button.GetComponentInChildren<Text>(true);
            text.text = label;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 22;
            EnsureButtonContract(button, semanticId, actionId, role, priority);
            var layout = button.GetComponent<LayoutElement>();
            layout.minWidth = 80f;
            layout.preferredWidth = parent == this.jumpRow ? 180f : 96f;
            layout.flexibleWidth = parent == this.jumpRow ? 1f : 0f;
            return button;
        }

        private void EnsurePickerLayout()
        {
            if (this.pickerContent != null)
            {
                return;
            }

            var panel = (RectTransform)this.transform;
            this.pickerHeight = panel.rect.height;
            var children = new List<Transform>();
            foreach (Transform child in panel) children.Add(child);
            this.pickerContent = new GameObject("ClassicProductionPicker", typeof(RectTransform)).GetComponent<RectTransform>();
            this.pickerContent.SetParent(panel, false);
            this.pickerContent.anchorMin = this.pickerContent.anchorMax = new Vector2(0.5f, 0.5f);
            this.pickerContent.sizeDelta = new Vector2(1360f, this.pickerHeight);
            foreach (var child in children) child.SetParent(this.pickerContent, false);
            FitPickerToPanel();
        }

        private void FitPickerToPanel()
        {
            if (this.pickerContent == null) return;
            var panel = (RectTransform)this.transform;
            // Preserve the authored composition on wide windows; fit the complete strip on narrow ones.
            float scale = Mathf.Clamp(panel.rect.width / this.pickerContent.sizeDelta.x, 0.01f, 1f);
            this.pickerContent.localScale = Vector3.one * scale;
            float height = this.pickerHeight * scale;
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, height * panel.pivot.y);
        }

        private void EnsureInteractionContracts()
        {
            var surfaceId = this.panelMode == ProductionPanelMode.Management
                ? "owned-cities-production"
                : "single-city-production";
            WismUiSurface.Ensure(
                this.gameObject,
                surfaceId,
                WismUiControlState.Normal,
                WismUiControlState.Selected,
                WismUiControlState.Disabled,
                WismUiControlState.Busy);

            EnsureButtonContract(this.prodButton, surfaceId + ".produce", "production.start", WismUiControlRole.Command, 30);
            EnsureButtonContract(this.locButton, surfaceId + ".destination", "production.choose-destination", WismUiControlRole.Command, 30);
            EnsureButtonContract(this.stopButton, surfaceId + ".stop", "production.stop", WismUiControlRole.Command, 30);
            EnsureButtonContract(this.exitButton, surfaceId + ".exit", "production.close", WismUiControlRole.Navigation, 40);

            if (this.armyButtons == null)
            {
                return;
            }

            for (var i = 0; i < this.armyButtons.Length; i++)
            {
                EnsureButtonContract(
                    this.armyButtons[i],
                    $"{surfaceId}.army-{i + 1}",
                    "production.select-army",
                    WismUiControlRole.Selection,
                    20);
            }
        }

        private static void EnsureButtonContract(
            Button button,
            string semanticId,
            string actionId,
            WismUiControlRole role,
            int priority)
        {
            if (button == null)
            {
                return;
            }

            WismHitTargetPolicy.Apply(button.gameObject);
            WismUiControl.Ensure(button.gameObject, semanticId, role, actionId, priority);
        }

        private void RefreshDynamicControls()
        {
            if (this.viewModel == null || this.cityText == null)
            {
                return;
            }

            var selected = this.viewModel.SelectedCity;
            this.cityText.text = $"{selected.CityName}: {(selected.IsIdle ? "Idle" : selected.CurrentArmyName)}";

            RefreshJumpControls(selected);
            SetStatus(this.selectingDestination ? "Choose a destination city." : BuildStatusText(selected));
        }

        private void OnDestinationJumpClick()
        {
            var destination = this.viewModel?.SelectedCity?.CurrentDestinationCity;
            if (destination == null)
            {
                return;
            }

            SelectManagementCity(destination);
        }

        private void OnSourceJumpClick(int index)
        {
            var incoming = this.viewModel?.SelectedCity?.IncomingSources;
            if (incoming == null || index < 0 || index >= incoming.Count)
            {
                return;
            }

            SelectManagementCity(incoming[index].SourceCity);
        }

        private void SelectManagementCity(City city)
        {
            if (city == null)
            {
                return;
            }

            this.panelMode = ProductionPanelMode.Management;
            this.productionCity = city;
            this.armySelectedIndex = -1;
            InitializeProduction();
        }

        private void RefreshJumpControls(ProductionCityViewModel selected)
        {
            if (this.destinationJumpButton != null)
            {
                this.destinationJumpButton.interactable =
                    this.panelMode == ProductionPanelMode.Management &&
                    selected.CurrentDestinationCity != null &&
                    selected.CurrentDestinationCity != selected.City;
                this.destinationJumpButton.gameObject.SetActive(this.destinationJumpButton.interactable);
                SetButtonText(this.destinationJumpButton, "To " + selected.DestinationCityName);
            }

            if (this.sourceJumpButtons == null)
            {
                return;
            }

            for (var i = 0; i < this.sourceJumpButtons.Length; i++)
            {
                var hasSource = i < selected.IncomingSources.Count;
                this.sourceJumpButtons[i].interactable = this.panelMode == ProductionPanelMode.Management && hasSource;
                this.sourceJumpButtons[i].gameObject.SetActive(this.sourceJumpButtons[i].interactable);
                SetButtonText(this.sourceJumpButtons[i], hasSource ? "From " + selected.IncomingSources[i].SourceCityName : string.Empty);
            }
            bool hasRoutes = this.destinationJumpButton.gameObject.activeSelf ||
                Array.Exists(this.sourceJumpButtons, button => button.gameObject.activeSelf);
            this.jumpRow.gameObject.SetActive(hasRoutes);
            this.managementSummary.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, hasRoutes ? 84f : 44f);
        }

        private static void SetButtonText(Button button, string value)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = value;
            }
        }

        private string BuildStatusText(ProductionCityViewModel selected)
        {
            return this.panelMode == ProductionPanelMode.Management
                ? $"{this.viewModel.SelectedCityIndex + 1}/{this.viewModel.Cities.Count} owned cities"
                : "Single-city production";
        }

        private void SetStatus(string value)
        {
            if (this.statusText != null)
            {
                this.statusText.text = value;
            }
        }
    }
}
