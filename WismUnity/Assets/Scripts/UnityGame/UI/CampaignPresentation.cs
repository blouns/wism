using System;
using System.Linq;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wism.Client.Core;

namespace Assets.Scripts.UI
{
    public enum CampaignPage { None, Arrival, Scroll, Surrender, Rejection, Victory, Dominion, Inspection }

    [DisallowMultipleComponent]
    public sealed class CampaignPresentation : MonoBehaviour
    {
        private UnityManager manager;
        private Game game;
        private GameObject canvasRoot;
        private Text heading;
        private Text message;
        private Button proceed;
        private Button accept;
        private Button reject;
        private Font font;
        private bool decisionTaken;
        private InputMode previousMode;

        public CampaignPage Page { get; private set; }
        public bool IsOpen => Page != CampaignPage.None;

        public void Initialize(UnityManager owner) => manager = owner ?? throw new ArgumentNullException(nameof(owner));

        public bool PresentPendingOffer()
        {
            if (!ReferenceEquals(game, Game.Current)) ResetForGame();
            if (IsOpen) return true;
            var outcome = Game.Current.VictoryOutcome;
            if (Game.Current.GameState == GameState.GameOver || outcome == null ||
                outcome.OutcomeKind != VictoryOutcomeKind.SurrenderOffered || !outcome.SurrenderEligible)
                return false;

            if (!manager.InteractiveUI)
            {
                VictoryEvaluator.AcceptSurrender(Game.Current, World.Current, outcome);
                return false;
            }

            Begin();
            Show(CampaignPage.Arrival);
            return true;
        }

        public void PresentVictory()
        {
            // Dead human players still make this a human campaign, not an AI-only match.
            if (!manager.InteractiveUI || !Game.Current.Players.Any(player => player.IsHuman)) return;
            if (string.IsNullOrEmpty(Game.Current.VictoryOutcome?.WinnerClanShortName)) return;
            if (!ReferenceEquals(game, Game.Current)) ResetForGame();
            if (IsOpen) return;
            Begin();
            Show(CampaignPage.Victory);
        }

        private void Begin()
        {
            EnsureCanvas();
            game = Game.Current;
            previousMode = manager.InputManager.InputMode;
            decisionTaken = false;
            canvasRoot.SetActive(true);
            manager.InputManager.SetInputMode(InputMode.UI);
        }

        public void Continue()
        {
            if (!ReferenceEquals(game, Game.Current)) { ResetForGame(); return; }
            switch (Page)
            {
                case CampaignPage.Arrival: Show(CampaignPage.Scroll); break;
                case CampaignPage.Scroll: Show(CampaignPage.Surrender); break;
                case CampaignPage.Victory: Show(CampaignPage.Dominion); break;
                case CampaignPage.Dominion: Show(CampaignPage.Inspection); break;
                case CampaignPage.Inspection:
                case CampaignPage.Rejection: Close(); break;
            }
        }

        public void Decide(bool accepted)
        {
            if (Page != CampaignPage.Surrender || decisionTaken || !ReferenceEquals(game, Game.Current)) return;
            var offer = game.VictoryOutcome;
            if (offer == null || offer.OutcomeKind != VictoryOutcomeKind.SurrenderOffered || !offer.SurrenderEligible) return;
            decisionTaken = true;
            if (accepted)
            {
                VictoryEvaluator.AcceptSurrender(game, World.Current, offer);
                // Let the manager publish the durable result and begin the victory pages.
                Close();
            }
            else
            {
                VictoryEvaluator.RejectSurrender(game, offer);
                Show(CampaignPage.Rejection);
            }
        }

        private void Close()
        {
            Page = CampaignPage.None;
            canvasRoot.SetActive(false);
            manager.InputManager.SetInputMode(Game.Current.GameState == GameState.GameOver ? InputMode.Game : previousMode);
            manager.InputManager.SkipInput();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void ResetForGame()
        {
            Page = CampaignPage.None;
            game = Game.IsInitialized() ? Game.Current : null;
            decisionTaken = false;
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        private void Show(CampaignPage page)
        {
            Page = page;
            bool choosing = page == CampaignPage.Surrender;
            proceed.gameObject.SetActive(!choosing);
            accept.gameObject.SetActive(choosing);
            reject.gameObject.SetActive(choosing);
            var winner = game.VictoryOutcome?.WinnerClanDisplayName ?? "The victor";
            bool humanWinner = game.Players.Any(player => player.IsHuman &&
                player.Clan.ShortName == game.VictoryOutcome?.WinnerClanShortName);
            heading.text = page == CampaignPage.Rejection ? "Peace is not an option" :
                page == CampaignPage.Arrival || page == CampaignPage.Scroll || page == CampaignPage.Surrender ? "An offer of peace" : "The war is over";
            switch (page)
            {
                case CampaignPage.Arrival: message.text = "Your seneschal reports strangers at the gate!"; break;
                case CampaignPage.Scroll: message.text = "The remaining lords send ambassadors. They humbly present a scroll."; break;
                case CampaignPage.Surrender:
                    message.text = "Mighty Warlord!\nThe remaining lords offer you all their lands if you will spare their lives."; break;
                case CampaignPage.Rejection: message.text = "No quarter!\nThe war continues."; break;
                case CampaignPage.Victory: message.text = humanWinner ? $"{winner}: You have won!" : $"{winner} have won the war."; break;
                case CampaignPage.Dominion:
                    message.text = humanWinner ? $"You now rule {World.Current.Name}." : $"{winner} now rule {World.Current.Name}."; break;
                case CampaignPage.Inspection: message.text = "You may now inspect the realm."; break;
            }
            proceed.GetComponentInChildren<Text>().text = page == CampaignPage.Inspection ? "Inspect Realm" :
                page == CampaignPage.Rejection ? "Continue War" : "Continue";
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(choosing ? null : proceed.gameObject);
        }

        private void EnsureCanvas()
        {
            if (canvasRoot != null) return;
            font = UnityUtilities.GameObjectHardFind("CityProductionPanel")
                .GetComponentsInChildren<Text>(true).FirstOrDefault(text => text.font != null)?.font ?? WismFontResolver.Resolve();
            canvasRoot = new GameObject("CampaignCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 768);
            scaler.matchWidthOrHeight = 1;
            var backdrop = new GameObject("CampaignBackdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(canvasRoot.transform, false);
            backdrop.GetComponent<Image>().color = new Color(0, 0, 0, 0.4f);
            var rect = (RectTransform)backdrop.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var panel = new GameObject("CampaignPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(backdrop.transform, false);
            panel.GetComponent<Image>().color = new Color32(128, 128, 128, 255);
            rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(640, 356);
            heading = Label(rect, "Heading", 28, 24, 52);
            message = Label(rect, "Message", 24, 92, 140);
            proceed = MakeButton(rect, "CampaignContinue", "Continue", "campaign.continue", Continue, 190, 260, 260);
            accept = MakeButton(rect, "AcceptSurrender", "Accept Surrender", "campaign.accept", () => Decide(true), 24, 260, 284);
            reject = MakeButton(rect, "RejectSurrender", "Reject Surrender", "campaign.reject", () => Decide(false), 332, 260, 284);
            WismUiSurface.Ensure(canvasRoot, "campaign-ending", WismUiControlState.Normal);
        }

        private Text Label(Transform parent, string name, int size, float y, float height)
        {
            var text = WismUiFactory.CreateText(parent, name, "", size, TextAnchor.MiddleCenter);
            text.font = font;
            text.color = Color.black;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = size;
            Place(text.rectTransform, 24, y, 592, height);
            return text;
        }

        private Button MakeButton(Transform parent, string name, string label, string action, UnityEngine.Events.UnityAction click, float x, float y, float width)
        {
            var button = WismUiFactory.CreateButton(parent, name, label, action, action, overlapPriority: 100);
            button.image.color = new Color32(185, 185, 185, 255);
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(235, 225, 170, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color32(190, 180, 140, 255);
            button.colors = colors;
            var text = button.GetComponentInChildren<Text>();
            text.font = font;
            text.color = Color.black;
            text.fontSize = 22;
            button.onClick.AddListener(click);
            Place((RectTransform)button.transform, x, y, width, 60);
            return button;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
