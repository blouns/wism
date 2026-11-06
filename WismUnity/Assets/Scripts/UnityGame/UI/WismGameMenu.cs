using System;
using System.Linq;
using Assets.Scripts.Managers;
using Wism.Client.Commands.Armies;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Assets.Scripts.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class WismGameMenu : MonoBehaviour
    {
        private static readonly Color Stone = new Color32(128, 128, 128, 255);
        private UnityManager manager;
        private GameObject canvasRoot;
        private GameObject shield;
        private GameObject menu;
        private GameObject confirmation;
        private Button openButton;
        private Button reportsButton;
        private Button viewButton;
        private Button capitalButton;
        private Button inventoryButton;
        private Button heroesButton;
        public HeroControlsView HeroControls { get; private set; }
        private GameObject viewMenu;
        private GameObject ordersMenu;
        private Button ordersButton;
        private Button buildButton;
        private Button razeButton;
        private Button disbandButton;
        private GameObject disbandConfirmation;
        private Text disbandTitle;
        private Text disbandDetails;
        private ScrollRect disbandScroll;
        private Button disbandCancel;
        private DisbandArmiesCommand pendingDisband;
        private Toggle observeToggle;
        private Toggle combatToggle;
        public StrategicReportView Reports { get; private set; }
        private Button saveButton;
        private Button loadButton;
        private Button exitButton;
        private Button cancelButton;
        private InputMode previousMode;
        private GameObject previousSelection;
        private bool exitRequested;
        private Font font;
        private bool canIssueOrders;
        private bool canInspectHeroes;

        public bool IsOpen => shield != null && shield.activeSelf;
        public bool IsConfirmingExit => confirmation != null && confirmation.activeSelf;
        public bool IsConfirmingDisband => disbandConfirmation != null && disbandConfirmation.activeSelf;
        public Action ApplicationExit { get; set; } = ExitApplication;

        public void Initialize(UnityManager owner)
        {
            manager = owner ?? throw new ArgumentNullException(nameof(owner));
            if (canvasRoot != null) return;
            font = UnityUtilities.GameObjectHardFind("CityProductionPanel")
                .GetComponentsInChildren<Text>(true).FirstOrDefault(text => text.font != null)?.font
                ?? WismFontResolver.Resolve();
            canvasRoot = new GameObject("GameMenuCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 768);
            scaler.matchWidthOrHeight = 1;
            openButton = Button(canvasRoot.transform, "OpenGameMenu", "Menu", "game.menu", () => { if (IsOpen) Cancel(); else Open(); });
            Place((RectTransform)openButton.transform, new Vector2(0, 1), new Vector2(8, -8), new Vector2(100, 44));
            reportsButton = Button(canvasRoot.transform, "OpenReports", "Reports", "game.reports", () => OpenReports());
            Place((RectTransform)reportsButton.transform, new Vector2(0, 1), new Vector2(116, -8), new Vector2(128, 44));
            viewButton = Button(canvasRoot.transform, "OpenViewMenu", "View", "game.view", OpenView);
            Place((RectTransform)viewButton.transform, new Vector2(0, 1), new Vector2(252, -8), new Vector2(100, 44));
            ordersButton = Button(canvasRoot.transform, "OpenOrdersMenu", "Orders", "game.orders", OpenOrders);
            Place((RectTransform)ordersButton.transform, new Vector2(0, 1), new Vector2(360, -8), new Vector2(112, 44));
            shield = Panel(canvasRoot.transform, "MenuBackdrop", new Color(0, 0, 0, 0.25f));
            Stretch((RectTransform)shield.transform);
            shield.AddComponent<Button>().onClick.AddListener(Cancel);

            menu = Panel(shield.transform, "GameMenu", Stone);
            Place((RectTransform)menu.transform, new Vector2(0, 1), new Vector2(8, -56), new Vector2(304, 252));
            observeToggle = Preference("ObserveMovement", "Observe AI Moves", "game.observe", 8,
                value => manager.ObserveAiMovement = value);
            combatToggle = Preference("ShowAiCombat", "Show AI Combat", "game.show-ai-combat", 56,
                value => manager.ShowAiCombat = value);
            saveButton = Button(menu.transform, "SaveGame", "Save Game", "game.save", () => OpenPersistence(true));
            loadButton = Button(menu.transform, "LoadGame", "Load Game", "game.load", () => OpenPersistence(false));
            exitButton = Button(menu.transform, "ExitGame", "Exit Game...", "game.exit.request", RequestExit);
            PlaceRow(saveButton, 104);
            PlaceRow(loadButton, 152);
            PlaceRow(exitButton, 200);
            viewMenu = Panel(shield.transform, "ViewMenu", Stone);
            Place((RectTransform)viewMenu.transform, new Vector2(0, 1), new Vector2(252, -56), new Vector2(240, 156));
            capitalButton = Button(viewMenu.transform, "Capital", "Capital", "view.capital", NavigateToCapital);
            Place((RectTransform)capitalButton.transform, new Vector2(0, 1), new Vector2(8, -8), new Vector2(224, 44));
            inventoryButton = Button(viewMenu.transform, "Inventory", "Inventory", "heroes.inventory", () => OpenHeroes(false));
            heroesButton = Button(viewMenu.transform, "FindHeroes", "Find Heroes", "heroes.find", () => OpenHeroes(true));
            Place((RectTransform)inventoryButton.transform, new Vector2(0, 1), new Vector2(8, -56), new Vector2(224, 44));
            Place((RectTransform)heroesButton.transform, new Vector2(0, 1), new Vector2(8, -104), new Vector2(224, 44));
            viewMenu.SetActive(false);
            ordersMenu = Panel(shield.transform, "OrdersMenu", Stone);
            Place((RectTransform)ordersMenu.transform, new Vector2(0, 1), new Vector2(360, -56), new Vector2(240, 156));
            buildButton = Button(ordersMenu.transform, "Build", "Build", "orders.build", () => Order(true));
            razeButton = Button(ordersMenu.transform, "Raze", "Raze", "orders.raze", () => Order(false));
            disbandButton = Button(ordersMenu.transform, "Disband", "Disband...", "orders.disband", RequestDisband);
            Place((RectTransform)buildButton.transform, new Vector2(0, 1), new Vector2(8, -8), new Vector2(224, 44));
            Place((RectTransform)razeButton.transform, new Vector2(0, 1), new Vector2(8, -56), new Vector2(224, 44));
            Place((RectTransform)disbandButton.transform, new Vector2(0, 1), new Vector2(8, -104), new Vector2(224, 44));
            ordersMenu.SetActive(false);

            confirmation = Panel(shield.transform, "ExitConfirmation", Stone);
            var rect = (RectTransform)confirmation.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = rect.anchorMin;
            rect.sizeDelta = new Vector2(440, 200);
            var title = WismUiFactory.CreateText(rect, "Title", "Exit WISM?", 24, TextAnchor.MiddleCenter);
            title.color = Color.black;
            title.font = font;
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(16, -12), new Vector2(408, 44));
            var message = WismUiFactory.CreateText(rect, "Message", "Unsaved progress will be lost.", 18, TextAnchor.MiddleCenter);
            message.color = Color.black;
            message.font = font;
            Place(message.rectTransform, new Vector2(0, 1), new Vector2(16, -60), new Vector2(408, 52));
            cancelButton = Button(rect, "CancelExit", "Cancel", "game.exit.cancel", Cancel);
            var confirm = Button(rect, "ConfirmExit", "Exit Game", "game.exit.confirm", ConfirmExit);
            Place((RectTransform)cancelButton.transform, new Vector2(0, 1), new Vector2(16, -136), new Vector2(196, 44));
            Place((RectTransform)confirm.transform, new Vector2(0, 1), new Vector2(228, -136), new Vector2(196, 44));
            var reportRoot = new GameObject("StrategicReports", typeof(RectTransform), typeof(StrategicReportView));
            reportRoot.transform.SetParent(shield.transform, false);
            Stretch((RectTransform)reportRoot.transform);
            Reports = reportRoot.GetComponent<StrategicReportView>();
            Reports.Initialize(manager, font, Close);
            reportRoot.SetActive(false);
            var heroRoot = new GameObject("HeroControls", typeof(RectTransform), typeof(HeroControlsView));
            heroRoot.transform.SetParent(shield.transform, false);
            HeroControls = heroRoot.GetComponent<HeroControlsView>();
            HeroControls.Initialize(manager, font, Close);
            InitializeDisband();
            WismUiSurface.Ensure(canvasRoot, "game-menu", WismUiControlState.Normal, WismUiControlState.Disabled);
            shield.SetActive(false);
            confirmation.SetActive(false);
            SetNavigation(false);
        }

        private bool CanOpen => manager != null && manager.ExecutionMode == ExecutionMode.Running &&
            (manager.InputManager.InputMode == InputMode.Game || manager.InputManager.InputMode == InputMode.AITurn);

        private bool CanPersist => manager != null && !manager.InputManager.EndTurnPending &&
            (Wism.Client.Core.Game.Current.GameState == Wism.Client.Core.GameState.GameOver ||
             manager.InputManager.CanAcceptGameplayInput) &&
            manager.LastCommandId == (manager.GameManager.ControllerProvider.CommandController.GetLastCommand()?.Id ?? 0);

        private bool CanInspectState => Wism.Client.Core.Game.Current.GameState == Wism.Client.Core.GameState.Ready ||
            Wism.Client.Core.Game.Current.GameState == Wism.Client.Core.GameState.SelectedArmy ||
            Wism.Client.Core.Game.Current.GameState == Wism.Client.Core.GameState.GameOver;

        private void Update()
        {
            if (openButton == null) return;
            openButton.interactable = IsOpen || CanOpen;
            reportsButton.interactable = IsOpen || CanOpen;
            viewButton.interactable = IsOpen || CanOpen;
            ordersButton.interactable = IsOpen || CanOpen;
            if (!IsOpen && CanOpen && WismUiInputAdapter.MenuPressedThisFrame()) Open();
            if (!IsOpen && CanOpen && WismUiInputAdapter.ApplicationExitPressedThisFrame()) RequestExit();
            if (!IsOpen && CanOpen && manager.InteractiveUI)
            {
                if (WismUiInputAdapter.InventoryPressedThisFrame()) OpenHeroes(false);
                else if (WismUiInputAdapter.FindHeroesPressedThisFrame()) OpenHeroes(true);
            }
            if (!IsOpen) return;
            bool escape = Input.GetKeyDown(KeyCode.Escape);
#if ENABLE_INPUT_SYSTEM
            escape |= Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
            if (escape) Cancel();
        }

        public void Open()
        {
            if (IsOpen || !CanOpen) return;
            previousMode = manager.InputManager.InputMode;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            saveButton.interactable = loadButton.interactable = CanPersist;
            canIssueOrders = CanPersist && manager.InputManager.CanAcceptGameplayInput &&
                Wism.Client.Core.Game.Current.ArmiesSelected();
            canInspectHeroes = CanPersist && CanInspectState && manager.InteractiveUI &&
                Wism.Client.Core.Game.Current.GetCurrentPlayer()?.IsHuman == true;
            observeToggle.SetIsOnWithoutNotify(manager.ObserveAiMovement);
            combatToggle.SetIsOnWithoutNotify(manager.ShowAiCombat);
            capitalButton.interactable = manager.CanNavigateToCapital;
            capitalButton.GetComponentInChildren<Text>().text = capitalButton.interactable ? "Capital" : "No owned capital";
            inventoryButton.interactable = heroesButton.interactable = canInspectHeroes;
            exitRequested = false;
            menu.SetActive(true);
            viewMenu.SetActive(false);
            ordersMenu.SetActive(false);
            Reports.gameObject.SetActive(false);
            HeroControls.gameObject.SetActive(false);
            confirmation.SetActive(false);
            shield.SetActive(true);
            SetNavigation(true);
            manager.InputManager.SetInputMode(InputMode.UI);
            Select(exitButton.gameObject);
        }

        public void RequestExit()
        {
            if (IsConfirmingDisband) return;
            if (!IsOpen) Open();
            if (!IsOpen || exitRequested) return;
            menu.SetActive(false);
            viewMenu.SetActive(false);
            ordersMenu.SetActive(false);
            Reports.gameObject.SetActive(false);
            confirmation.SetActive(true);
            HeroControls.gameObject.SetActive(false);
            Select(cancelButton.gameObject);
        }

        public void ConfirmExit()
        {
            if (!IsConfirmingExit || exitRequested) return;
            exitRequested = true;
            ApplicationExit();
        }

        public void Cancel()
        {
            if (!IsOpen || exitRequested) return;
            if (IsConfirmingDisband) { Close(); return; }
            if (IsConfirmingExit)
            {
                confirmation.SetActive(false);
                menu.SetActive(true);
                Select(exitButton.gameObject);
                return;
            }
            Close();
        }

        public void Close()
        {
            if (!IsOpen || exitRequested) return;
            shield.SetActive(false);
            viewMenu.SetActive(false);
            ordersMenu.SetActive(false);
            Reports.gameObject.SetActive(false);
            confirmation.SetActive(false);
            disbandConfirmation.SetActive(false);
            pendingDisband = null;
            SetNavigation(false);
            manager.InputManager.SetInputMode(previousMode);
            HeroControls.gameObject.SetActive(false);
            manager.InputManager.SkipInput();
            Select(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        }

        public void ResetForGame()
        {
            exitRequested = false;
            if (shield != null) shield.SetActive(false);
            if (confirmation != null) confirmation.SetActive(false);
            if (Reports != null) Reports.gameObject.SetActive(false);
            if (viewMenu != null) viewMenu.SetActive(false);
            if (ordersMenu != null) ordersMenu.SetActive(false);
            if (HeroControls != null) HeroControls.gameObject.SetActive(false);
            if (disbandConfirmation != null) disbandConfirmation.SetActive(false);
            pendingDisband = null;
            SetNavigation(false);
        }

        public void OpenReports(StrategicReportKind kind = StrategicReportKind.Cities)
        {
            if (!IsOpen) Open();
            if (!IsOpen || IsConfirmingExit || IsConfirmingDisband || exitRequested) return;
            menu.SetActive(false);
            viewMenu.SetActive(false);
            ordersMenu.SetActive(false);
            HeroControls.gameObject.SetActive(false);
            Reports.Open(kind);
            Select(Reports.GetComponentsInChildren<Button>().First(button => button.name == "Report" + kind).gameObject);
        }

        private void OpenPersistence(bool saving)
        {
            if (!IsOpen || IsConfirmingExit || IsConfirmingDisband || !CanPersist) return;
            Close();
            manager.HandleSaveLoadPicker(saving);
        }

        public void OpenView()
        {
            if (!IsOpen) Open();
            if (!IsOpen || IsConfirmingExit || IsConfirmingDisband || exitRequested) return;
            menu.SetActive(false);
            Reports.gameObject.SetActive(false);
            viewMenu.SetActive(true);
            ordersMenu.SetActive(false);
            HeroControls.gameObject.SetActive(false);
            Select(capitalButton.interactable ? capitalButton.gameObject : null);
        }

        public void OpenOrders()
        {
            if (!IsOpen) Open();
            if (!IsOpen || IsConfirmingExit || IsConfirmingDisband || exitRequested) return;
            menu.SetActive(false);
            viewMenu.SetActive(false);
            Reports.gameObject.SetActive(false);
            ordersMenu.SetActive(true);
            HeroControls.gameObject.SetActive(false);
            bool canOrder = canIssueOrders && Wism.Client.Core.Game.Current.GameState != Wism.Client.Core.GameState.GameOver &&
                previousMode == InputMode.Game && Wism.Client.Core.Game.Current.ArmiesSelected();
            buildButton.interactable = razeButton.interactable = canOrder;
            buildButton.GetComponentInChildren<Text>().text = canOrder ? "Build" : "Build (unavailable)";
            razeButton.GetComponentInChildren<Text>().text = canOrder ? "Raze" : "Raze (unavailable)";
            var preview = canOrder ? SelectedDisband()?.Preview() : null;
            disbandButton.interactable = manager.InteractiveUI && preview?.CanExecute == true;
            disbandButton.GetComponentInChildren<Text>().text = preview?.RejectionReason == DisbandRejectionReason.HeroesOnly
                ? "Heroes protected" : "Disband...";
            Select(canOrder ? buildButton.gameObject : null);
        }

        private void Order(bool build)
        {
            if (!ordersMenu.activeSelf || !buildButton.interactable) return;
            Close();
            if (build) manager.GameManager.Build();
            else manager.GameManager.RazeCity();
        }

        public void OpenHeroes(bool find)
        {
            // Check before Open changes the input mode; a rejected shortcut opens no modal.
            if (!IsOpen && (!CanOpen || !CanPersist || !CanInspectState || !manager.InteractiveUI ||
                Wism.Client.Core.Game.Current.GetCurrentPlayer()?.IsHuman != true)) return;
            if (!IsOpen) Open();
            if (!IsOpen || !canInspectHeroes || !CanInspectState || IsConfirmingExit || IsConfirmingDisband || exitRequested ||
                manager.InputManager.EndTurnPending || Wism.Client.Core.Game.Current.GetCurrentPlayer()?.IsHuman != true) return;
            menu.SetActive(false);
            viewMenu.SetActive(false);
            ordersMenu.SetActive(false);
            Reports.gameObject.SetActive(false);
            HeroControls.Open(find);
        }

        private void NavigateToCapital()
        {
            if (!viewMenu.activeSelf || !manager.CanNavigateToCapital) return;
            Close();
            manager.GoToCapitol(Wism.Client.Core.Game.Current.GetCurrentPlayer());
        }

        private void SetNavigation(bool visible)
        {
            foreach (var button in new[] { reportsButton, viewButton, ordersButton })
            {
                if (button == null) continue;
                button.gameObject.SetActive(visible);
                if (visible) button.transform.SetAsLastSibling();
            }
            if (openButton != null) openButton.transform.SetAsLastSibling();
        }

        private DisbandArmiesCommand SelectedDisband()
        {
            var armies = Wism.Client.Core.Game.Current.GetSelectedArmies();
            return armies == null ? null : new DisbandArmiesCommand(manager.GameManager.ControllerProvider.ArmyController, armies);
        }

        public void RequestDisband()
        {
            if (IsConfirmingExit || IsConfirmingDisband || !manager.InteractiveUI) return;
            if (!IsOpen && (!CanOpen || !CanPersist || !manager.InputManager.CanAcceptGameplayInput)) return;
            var candidate = SelectedDisband();
            var preview = candidate?.Preview();
            if (preview?.CanExecute != true) return;
            if (!IsOpen) Open();
            if (!IsOpen || !canIssueOrders || previousMode != InputMode.Game) return;
            pendingDisband = candidate;
            menu.SetActive(false);
            ordersMenu.SetActive(false);
            viewMenu.SetActive(false);
            Reports.gameObject.SetActive(false);
            HeroControls.gameObject.SetActive(false);
            int count = preview.ArmiesToDisband.Count;
            disbandTitle.text = $"Disband {count} {(count == 1 ? "army" : "armies")}?";
            disbandDetails.text = "Remove permanently:\n" + string.Join("\n", preview.ArmiesToDisband.Select((army, index) => $"{index + 1}. {army.DisplayName}")) +
                "\n\nKeep heroes and artifacts:\n" + (preview.HeroesToPreserve.Count == 0 ? "None selected" :
                    string.Join("\n", preview.HeroesToPreserve.Select(hero => hero.DisplayName)));
            disbandConfirmation.SetActive(true);
            Canvas.ForceUpdateCanvases();
            disbandDetails.rectTransform.sizeDelta = new Vector2(-24, Mathf.Max(disbandScroll.viewport.rect.height, disbandDetails.preferredHeight + 20));
            disbandScroll.verticalNormalizedPosition = 1;
            Select(disbandCancel.gameObject);
        }

        public void ConfirmDisband()
        {
            if (!IsConfirmingDisband || pendingDisband == null) return;
            var command = pendingDisband;
            var selected = Wism.Client.Core.Game.Current.GetSelectedArmies();
            var valid = !manager.InputManager.EndTurnPending && previousMode == InputMode.Game &&
                Wism.Client.Core.Game.Current.GetCurrentPlayer()?.IsHuman == true &&
                manager.LastCommandId == (manager.GameManager.ControllerProvider.CommandController.GetLastCommand()?.Id ?? 0) &&
                selected != null && selected.SequenceEqual(command.Armies) && command.Preview().CanExecute;
            Close();
            if (valid) manager.GameManager.ControllerProvider.CommandController.AddCommand(command);
        }

        private void InitializeDisband()
        {
            disbandConfirmation = Panel(shield.transform, "DisbandConfirmation", Stone);
            var rect = (RectTransform)disbandConfirmation.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(700, 420);
            disbandTitle = WismUiFactory.CreateText(rect, "DisbandTitle", "Disband armies?", 26, TextAnchor.MiddleLeft);
            disbandTitle.font = font;
            disbandTitle.color = Color.black;
            disbandTitle.rectTransform.anchorMin = new Vector2(0, 1);
            disbandTitle.rectTransform.anchorMax = Vector2.one;
            disbandTitle.rectTransform.offsetMin = new Vector2(20, -64);
            disbandTitle.rectTransform.offsetMax = new Vector2(-20, -12);
            var scrollRoot = Panel(rect, "DisbandList", Color.white);
            var scrollRect = (RectTransform)scrollRoot.transform;
            Stretch(scrollRect);
            scrollRect.offsetMin = new Vector2(20, 84);
            scrollRect.offsetMax = new Vector2(-20, -76);
            scrollRoot.AddComponent<RectMask2D>();
            disbandScroll = scrollRoot.AddComponent<ScrollRect>();
            disbandScroll.viewport = scrollRect;
            disbandScroll.horizontal = false;
            disbandScroll.movementType = ScrollRect.MovementType.Clamped;
            disbandDetails = WismUiFactory.CreateText(scrollRect, "DisbandDetails", "", 22, TextAnchor.UpperLeft);
            disbandDetails.font = font;
            disbandDetails.color = Color.black;
            disbandDetails.supportRichText = false;
            disbandDetails.horizontalOverflow = HorizontalWrapMode.Wrap;
            disbandDetails.verticalOverflow = VerticalWrapMode.Overflow;
            disbandDetails.rectTransform.anchorMin = new Vector2(0, 1);
            disbandDetails.rectTransform.anchorMax = Vector2.one;
            disbandDetails.rectTransform.pivot = new Vector2(.5f, 1);
            disbandDetails.rectTransform.anchoredPosition = new Vector2(0, -10);
            disbandDetails.rectTransform.sizeDelta = new Vector2(-24, 0);
            disbandScroll.content = disbandDetails.rectTransform;
            disbandCancel = Button(rect, "CancelDisband", "Cancel", "orders.disband.cancel", Close);
            var confirm = Button(rect, "ConfirmDisband", "Disband", "orders.disband.confirm", ConfirmDisband);
            Place((RectTransform)disbandCancel.transform, Vector2.zero, new Vector2(20, 20), new Vector2(196, 44));
            Place((RectTransform)confirm.transform, Vector2.right, new Vector2(-20, 20), new Vector2(196, 44));
            disbandConfirmation.SetActive(false);
        }

        private Toggle Preference(string name, string label, string id, float y, UnityEngine.Events.UnityAction<bool> change)
        {
            var root = Panel(menu.transform, name, new Color32(151, 151, 151, 255));
            Place((RectTransform)root.transform, new Vector2(0, 1), new Vector2(8, -y), new Vector2(288, 44));
            var toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = root.GetComponent<Image>();
            var box = Panel(root.transform, "Checkbox", Color.white);
            Place((RectTransform)box.transform, new Vector2(0, 1), new Vector2(10, -10), new Vector2(24, 24));
            box.GetComponent<Image>().raycastTarget = false;
            var check = WismUiFactory.CreateText(box.transform, "Check", "X", 20, TextAnchor.MiddleCenter);
            check.font = font;
            check.color = Color.black;
            Stretch(check.rectTransform);
            toggle.graphic = check;
            var text = WismUiFactory.CreateText(root.transform, "Label", label, 22, TextAnchor.MiddleLeft);
            text.font = font;
            text.color = Color.black;
            Place(text.rectTransform, new Vector2(0, 1), new Vector2(44, 0), new Vector2(236, 44));
            WismUiControl.Ensure(root, id, WismUiControlRole.Toggle, id, 100);
            toggle.onValueChanged.AddListener(change);
            return toggle;
        }

        private void OnDestroy()
        {
            if (canvasRoot != null) Destroy(canvasRoot);
        }

        private static void ExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Select(GameObject target)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
        }

        private Button Button(Transform parent, string name, string label, string action, UnityEngine.Events.UnityAction click)
        {
            var button = WismUiFactory.CreateButton(parent, name, label, action, action, overlapPriority: 100);
            button.image.color = new Color32(151, 151, 151, 255);
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(230, 215, 160, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color32(190, 178, 136, 255);
            button.colors = colors;
            var text = button.GetComponentInChildren<Text>();
            text.color = Color.black;
            text.font = font;
            text.fontSize = 22;
            Bevel((RectTransform)button.transform);
            button.onClick.AddListener(click);
            return button;
        }

        private static GameObject Panel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            if (name != "MenuBackdrop") Bevel((RectTransform)panel.transform);
            return panel;
        }

        private static void Bevel(RectTransform parent)
        {
            for (int i = 0; i < 4; i++)
            {
                var edge = new GameObject("Border" + i, typeof(RectTransform), typeof(Image));
                edge.transform.SetParent(parent, false);
                var graphic = edge.GetComponent<Image>();
                graphic.color = i < 2 ? new Color32(190, 190, 190, 255) : new Color32(56, 56, 56, 255);
                graphic.raycastTarget = false;
                var rect = (RectTransform)edge.transform;
                Stretch(rect);
                if (i == 0) { rect.anchorMin = Vector2.up; rect.offsetMin = new Vector2(0, -2); }
                else if (i == 1) { rect.anchorMax = Vector2.up; rect.offsetMax = new Vector2(2, 0); }
                else if (i == 2) { rect.anchorMax = Vector2.right; rect.offsetMax = new Vector2(0, 2); }
                else { rect.anchorMin = Vector2.right; rect.offsetMin = new Vector2(-2, 0); }
            }
        }

        private static void PlaceRow(Button button, float y) =>
            Place((RectTransform)button.transform, new Vector2(0, 1), new Vector2(8, -y), new Vector2(288, 44));

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
