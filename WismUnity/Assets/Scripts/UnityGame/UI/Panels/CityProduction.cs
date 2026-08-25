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
        private Text modeText;
        private Text cityText;
        private Text statusText;
        private Text routeText;
        private Text deliveryText;
        private RectTransform minimapPanel;
        private Button destinationJumpButton;
        private Button[] sourceJumpButtons;

        public void LateUpdate()
        {
            if (this.armyButtons != null &&
                this.armySelectedIndex > 0 &&
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
            this.panelMode = ProductionPanelMode.Management;
            this.selectingDestination = false;
            this.productionCity = selectedCity ?? player.Capitol ?? player.GetCities()[0];

            InitializeProduction();
        }

        private void InitializeProduction()
        {
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
            string turnsRemainingString = "None";

            var barracks = this.productionCity.Barracks;
            if (barracks.ProducingArmy())
            {
                // Set image
                SetArmyImageOnGameObject(
                    Game.Current.GetCurrentPlayer().Clan,
                    barracks.ArmyInTraining.ArmyInfo,
                    "CurrentArmyKind");
                var currentArmyKind = this.transform.Find("CurrentArmyKind");
                if (currentArmyKind != null)
                {
                    currentArmyKind.gameObject.SetActive(true);
                }

                turnsRemainingString = barracks.ArmyInTraining.TurnsToProduce + "t";
            }
            else
            {
                var currentArmyKind = this.transform.Find("CurrentArmyKind");
                if (currentArmyKind != null)
                {
                    currentArmyKind.gameObject.SetActive(false);
                }
            }

            // Set turns remaining text
            var turnsRemaining = this.gameObject.transform.Find("TurnsRemainingText");
            if (turnsRemaining != null)
            {
                var turnsText = turnsRemaining.GetComponent<Text>();
                turnsText.text = turnsRemainingString;
            }
        }

        private void SetArmyImageOnGameObject(Clan clan, ArmyInfo info, string gameObjectName)
        {
            var imageTransform = this.gameObject.transform.Find(gameObjectName);
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
            StartProduction();

            if (this.panelMode == ProductionPanelMode.SingleCity)
            {
                OnExitClick();
                return;
            }

            RefreshAfterMutation();
        }

        private void StartProduction(City destinationCity = null)
        {
            var armyName = this.productionInfos[this.armySelectedIndex].ArmyInfoName;
            var armyInfo = ModFactory.FindArmyInfo(armyName);

            Debug.Log($"Starting production of {armyInfo.DisplayName}" +
                $" on {this.productionCity}" +
                $" to {(destinationCity == null ? this.productionCity : destinationCity)}");

            this.unityManager.GameManager
                .StartProduction(this.productionCity, armyInfo, destinationCity);
        }

        public void OnLocClick()
        {
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

            this.selectingDestination = false;
            StartProduction(destinationCity == this.productionCity ? null : destinationCity);
            this.unityManager.SetProductionMode(ProductionMode.CitySelected);
            this.unityManager.InputManager.SetInputMode(InputMode.UI);
            RefreshAfterMutation();
        }

        public void OnStopClick()
        {
            Debug.Log($"Stopping production on {this.productionCity}");
            this.unityManager.GameManager
                .StopProduction(this.productionCity);

            DisableProduction();
            RefreshAfterMutation();
        }

        public void OnExitClick()
        {
            this.armySelectedIndex = -1;
            this.selectingDestination = false;
            this.unityManager.InputManager.SetInputMode(InputMode.Game);
            this.unityManager.SetProductionMode(ProductionMode.None);
            this.gameObject.SetActive(false);
        }

        private void EnableProduction()
        {
            this.prodButton.interactable = true;

            if (Game.Current.GetCurrentPlayer()
                .GetCities().Count > 1)
            {
                this.locButton.interactable = true;
            }
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
            if (this.modeText != null)
            {
                return;
            }

            var panel = WismUiFactory.CreateVerticalPanel(this.transform, "WismProductionPanelSummary");
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 6f);
            panelRect.sizeDelta = new Vector2(0f, 264f);

            this.modeText = WismUiFactory.CreateText(panel, "ProductionModeText", string.Empty, 18, TextAnchor.MiddleCenter);
            this.cityText = WismUiFactory.CreateText(panel, "ProductionCityText", string.Empty, 16, TextAnchor.MiddleLeft);
            this.routeText = WismUiFactory.CreateText(panel, "ProductionRouteText", string.Empty, 14, TextAnchor.MiddleLeft);
            this.deliveryText = WismUiFactory.CreateText(panel, "ProductionDeliveryText", string.Empty, 14, TextAnchor.MiddleLeft);
            this.statusText = WismUiFactory.CreateText(panel, "ProductionStatusText", string.Empty, 14, TextAnchor.MiddleLeft);

            var row = WismUiFactory.CreateRow(panel, "ProductionNavigationRow");
            var previous = WismUiFactory.CreateButton(
                row,
                "PreviousProductionCityButton",
                "Prev",
                "owned-production.previous-city",
                "production.management.previous",
                WismUiControlRole.Navigation,
                10);
            previous.onClick.AddListener(OnPreviousCityClick);
            var next = WismUiFactory.CreateButton(
                row,
                "NextProductionCityButton",
                "Next",
                "owned-production.next-city",
                "production.management.next",
                WismUiControlRole.Navigation,
                10);
            next.onClick.AddListener(OnNextCityClick);

            var jumpRow = WismUiFactory.CreateRow(panel, "ProductionJumpRow");
            this.destinationJumpButton = WismUiFactory.CreateButton(
                jumpRow,
                "DestinationProductionCityButton",
                "->",
                "owned-production.destination",
                "production.management.destination",
                WismUiControlRole.Navigation,
                20);
            this.destinationJumpButton.onClick.AddListener(OnDestinationJumpClick);
            this.sourceJumpButtons = new Button[4];
            for (var i = 0; i < this.sourceJumpButtons.Length; i++)
            {
                var sourceIndex = i;
                this.sourceJumpButtons[i] = WismUiFactory.CreateButton(
                    jumpRow,
                    $"SourceProductionCityButton{i + 1}",
                    $"^{i + 1}",
                    $"owned-production.source-{i + 1}",
                    "production.management.source",
                    WismUiControlRole.Navigation,
                    20);
                this.sourceJumpButtons[i].onClick.AddListener(() => OnSourceJumpClick(sourceIndex));
            }

            this.minimapPanel = ProductionManagementUi.CreateMinimapPanel(panel, "ProductionMinimapOverlay");
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
            if (this.viewModel == null || this.modeText == null)
            {
                return;
            }

            var selected = this.viewModel.SelectedCity;
            this.modeText.text = this.panelMode == ProductionPanelMode.Management
                ? "Production Management"
                : "City Production";
            this.cityText.text = $"{selected.CityName}: {(selected.IsIdle ? "Idle" : selected.CurrentArmyName)}";
            this.routeText.text = BuildRouteText(selected);
            if (this.deliveryText != null)
            {
                this.deliveryText.text = BuildDeliveryText(selected);
            }

            RefreshJumpControls(selected);
            RefreshMinimapControls();
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

        private string BuildRouteText(ProductionCityViewModel selected)
        {
            var route = selected.IsIdle
                ? "No production routed."
                : $"Destination: {selected.DestinationCityName} ({selected.TurnsRemaining}t)";
            var incoming = selected.IncomingSources.Count == 0
                ? "Incoming: none"
                : $"Incoming: {string.Join(", ", ToIncomingLabels(selected.IncomingSources))}";
            var deliveries = selected.OutgoingDeliveries.Count == 0
                ? "Deliveries: none"
                : $"Deliveries: {string.Join(", ", ToIncomingLabels(selected.OutgoingDeliveries))}";
            return $"{route} | {incoming} | {deliveries}";
        }

        private string BuildDeliveryText(ProductionCityViewModel selected)
        {
            if (selected.IncomingSources.Count == 0 && selected.OutgoingDeliveries.Count == 0)
            {
                return "No routed production or delivery in transit.";
            }

            var incoming = selected.IncomingSources.Count == 0
                ? "Sources: none"
                : $"Sources: {string.Join(", ", ToIncomingLabels(selected.IncomingSources))}";
            var deliveries = selected.OutgoingDeliveries.Count == 0
                ? "Transit: none"
                : $"Transit: {string.Join(", ", ToIncomingLabels(selected.OutgoingDeliveries))}";
            return $"{incoming} | {deliveries}";
        }

        private void RefreshMinimapControls()
        {
            if (this.minimapPanel == null)
            {
                return;
            }

            var showMinimap = this.panelMode == ProductionPanelMode.Management;
            this.minimapPanel.gameObject.SetActive(showMinimap);
            if (showMinimap)
            {
                ProductionManagementUi.RebuildMinimapMarkers(this.minimapPanel, this.viewModel.MinimapMarkers);
            }
        }

        private void RefreshJumpControls(ProductionCityViewModel selected)
        {
            if (this.destinationJumpButton != null)
            {
                this.destinationJumpButton.interactable =
                    this.panelMode == ProductionPanelMode.Management &&
                    selected.CurrentDestinationCity != null &&
                    selected.CurrentDestinationCity != selected.City;
                SetButtonText(this.destinationJumpButton, "-> " + selected.DestinationCityName);
            }

            if (this.sourceJumpButtons == null)
            {
                return;
            }

            for (var i = 0; i < this.sourceJumpButtons.Length; i++)
            {
                var hasSource = i < selected.IncomingSources.Count;
                this.sourceJumpButtons[i].interactable = this.panelMode == ProductionPanelMode.Management && hasSource;
                SetButtonText(this.sourceJumpButtons[i], hasSource ? "^ " + selected.IncomingSources[i].SourceCityName : "^");
            }
        }

        private static void SetButtonText(Button button, string value)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = value;
            }
        }

        private static IEnumerable<string> ToIncomingLabels(IReadOnlyList<ProductionDeliveryViewModel> deliveries)
        {
            foreach (var delivery in deliveries)
            {
                yield return $"{delivery.SourceCityName}->{delivery.DestinationCityName} {delivery.ArmyDisplayName} {delivery.TurnsRemaining}t";
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
