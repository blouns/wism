using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public class ItemPicker : MonoBehaviour
    {
        private readonly List<MapObject> items = new List<MapObject>();
        private readonly List<Button> rows = new List<Button>();
        private int selectedIndex = -1;
        private int openedFrame;
        private RectTransform panel;
        private RectTransform content;
        private ScrollRect scroll;
        private Text heading;
        private Text context;
        private Button confirm;
        private Font font;
        private const float RowHeight = 48f;
        private static readonly Color Stone = new Color32(118, 118, 118, 255);
        private static readonly Color Selected = new Color32(196, 184, 112, 255);
        public OkCancel OkCancelResult { get; private set; }

        public void Initialize(UnityManager unityGame, List<MapObject> choices,
            string title = "Go to location", string action = "Go", string owner = "")
        {
            if (unityGame == null) throw new ArgumentNullException(nameof(unityGame));
            if (choices == null) throw new ArgumentNullException(nameof(choices));
            EnsureLayout();
            Clear();
            this.items.AddRange(choices);
            this.heading.text = title;
            this.context.text = string.IsNullOrEmpty(owner) ? choices.Count + " locations"
                : owner + " - " + choices.Count + (choices.Count == 1 ? " item" : " items");
            this.confirm.GetComponentInChildren<Text>().text = action;
            gameObject.SetActive(true);
            FitPanel();
            for (int i = 0; i < this.items.Count; i++)
            {
                int index = i;
                var row = CreateButton(this.content, "ItemRow" + i, this.items[i].DisplayName, () => SetCurrentItem(index));
                WismUiControl.Ensure(row.gameObject, "item-picker.row-" + i,
                    WismUiControlRole.Selection, "item-picker.select", 30);
                var rect = (RectTransform)row.transform;
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, RowHeight - 2);
                rect.anchoredPosition = new Vector2(0, -i * RowHeight);
                var label = row.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(16, 0);
                label.rectTransform.offsetMax = new Vector2(-16, 0);
                this.rows.Add(row);
            }
            this.content.sizeDelta = new Vector2(0, this.items.Count * RowHeight);
            this.scroll.verticalNormalizedPosition = 1;
            this.OkCancelResult = OkCancel.Picking;
            this.openedFrame = Time.frameCount;
            SetCurrentItem(this.items.Count > 0 ? 0 : -1);
        }

        public bool IsInitialized() => this.OkCancelResult == OkCancel.Picking;

        public void SetCurrentItem(int index)
        {
            this.selectedIndex = index >= 0 && index < this.items.Count ? index : -1;
            for (int i = 0; i < this.rows.Count; i++)
                this.rows[i].GetComponent<Image>().color = i == this.selectedIndex ? Selected : Stone;
            this.confirm.interactable = this.selectedIndex >= 0;
        }

        public void Ok()
        {
            if (!IsInitialized() || this.selectedIndex < 0) return;
            this.OkCancelResult = OkCancel.Ok;
            gameObject.SetActive(false);
        }

        public void Cancel()
        {
            this.selectedIndex = -1;
            this.OkCancelResult = OkCancel.Cancel;
            gameObject.SetActive(false);
        }

        public MapObject GetSelectedItem() => this.selectedIndex >= 0 && this.selectedIndex < this.items.Count
            ? this.items[this.selectedIndex] : null;

        public void Clear(Transform unused = null)
        {
            foreach (var row in this.rows)
            {
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
            this.rows.Clear();
            this.items.Clear();
            this.selectedIndex = -1;
            this.OkCancelResult = OkCancel.None;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsInitialized() || Time.frameCount <= this.openedFrame) return;
            FitPanel();
            if (WismUiInputAdapter.ItemPickerCancelPressed()) Cancel();
            else if (WismUiInputAdapter.ItemPickerConfirmPressed()) Ok();
            else
            {
                int direction = WismUiInputAdapter.ItemPickerDirection();
                if (direction != 0 && this.items.Count > 0)
                {
                    SetCurrentItem(Mathf.Clamp(this.selectedIndex + direction, 0, this.items.Count - 1));
                    float overflow = Mathf.Max(0, this.content.rect.height - this.scroll.viewport.rect.height);
                    float top = this.content.anchoredPosition.y;
                    float rowTop = this.selectedIndex * RowHeight;
                    if (rowTop < top) top = rowTop;
                    else if (rowTop + RowHeight > top + this.scroll.viewport.rect.height)
                        top = rowTop + RowHeight - this.scroll.viewport.rect.height;
                    this.content.anchoredPosition = new Vector2(0, Mathf.Clamp(top, 0, overflow));
                }
            }
        }

        private void EnsureLayout()
        {
            if (this.panel != null) return;
            // Match the shipped production panel, not the prototype's system-font list.
            this.font = UnityUtilities.GameObjectHardFind("CityProductionPanel")
                .GetComponentsInChildren<Text>(true).First(text => text.font != null).font;
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            Stretch((RectTransform)transform, Vector2.zero, Vector2.zero);
            transform.localScale = Vector3.one;
            var backdrop = GetComponent<Image>();
            backdrop.sprite = null;
            backdrop.color = new Color(0, 0, 0, 0.3f);
            backdrop.raycastTarget = true;
            this.panel = ImageRect(transform, "ClassicItemPicker", Stone);
            this.panel.anchorMin = this.panel.anchorMax = new Vector2(0.5f, 0.5f);
            Bevel(this.panel);
            this.heading = CreateText(this.panel, "Heading", "", 26);
            Stretch(this.heading.rectTransform, new Vector2(24, -56), new Vector2(-24, -8), true);
            this.context = CreateText(this.panel, "HeroName", "", 18);
            Stretch(this.context.rectTransform, new Vector2(24, -88), new Vector2(-24, -54), true);
            var viewport = ImageRect(this.panel, "ItemViewport", new Color32(104, 104, 104, 255));
            Stretch(viewport, new Vector2(20, 78), new Vector2(-46, -100));
            viewport.gameObject.AddComponent<RectMask2D>();
            this.scroll = viewport.gameObject.AddComponent<ScrollRect>();
            this.content = new GameObject("ItemRows", typeof(RectTransform)).GetComponent<RectTransform>();
            this.content.SetParent(viewport, false);
            this.content.anchorMin = new Vector2(0, 1);
            this.content.anchorMax = Vector2.one;
            this.content.pivot = new Vector2(0.5f, 1);
            this.scroll.content = this.content;
            this.scroll.viewport = viewport;
            this.scroll.horizontal = false;
            this.scroll.movementType = ScrollRect.MovementType.Clamped;
            this.scroll.scrollSensitivity = RowHeight;
            var track = ImageRect(this.panel, "ItemScrollTrack", new Color32(83, 83, 83, 255));
            track.anchorMin = new Vector2(1, 0);
            track.anchorMax = Vector2.one;
            track.offsetMin = new Vector2(-38, 78);
            track.offsetMax = new Vector2(-20, -100);
            var thumb = ImageRect(track, "ItemScrollThumb", new Color32(174, 174, 174, 255));
            Stretch(thumb, Vector2.zero, Vector2.zero);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = thumb;
            scrollbar.targetGraphic = thumb.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            this.scroll.verticalScrollbar = scrollbar;
            this.scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            WismHitTargetPolicy.Apply(track.gameObject);
            WismUiControl.Ensure(track.gameObject, "item-picker.scroll", WismUiControlRole.Navigation, "item-picker.scroll", 40);
            this.confirm = CreateButton(this.panel, "ConfirmItem", "Take", Ok);
            var exit = CreateButton(this.panel, "ExitItems", "Exit", Cancel);
            PlaceFooter((RectTransform)this.confirm.transform, -88);
            PlaceFooter((RectTransform)exit.transform, 88);
            WismUiSurface.Ensure(gameObject, "item-picker", WismUiControlState.Normal,
                WismUiControlState.Selected, WismUiControlState.Disabled);
        }

        private void FitPanel()
        {
            var bounds = ((RectTransform)transform).rect;
            float height = 178 + Mathf.Clamp(this.items.Count, 1, 6) * RowHeight;
            this.panel.sizeDelta = new Vector2(Mathf.Min(620, bounds.width - 32), Mathf.Min(height, bounds.height - 32));
        }

        private Text CreateText(Transform parent, string name, string value, int size)
        {
            var text = WismUiFactory.CreateText(parent, name, value, size, TextAnchor.MiddleCenter);
            text.font = this.font;
            text.color = Color.black;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = size;
            return text;
        }

        private Button CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction click)
        {
            var rect = ImageRect(parent, name, Stone);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(click);
            Bevel(rect);
            var text = CreateText(rect, "Label", label, 22);
            Stretch(text.rectTransform, new Vector2(8, 0), new Vector2(-8, 0));
            WismHitTargetPolicy.Apply(rect.gameObject);
            WismUiControl.Ensure(rect.gameObject, "item-picker." + name,
                WismUiControlRole.Command, "item-picker." + name, 30);
            return button;
        }

        private static RectTransform ImageRect(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        private static void Bevel(RectTransform rect)
        {
            var top = ImageRect(rect, "LightEdge", new Color32(174, 174, 174, 255));
            Stretch(top, Vector2.zero, Vector2.zero);
            top.anchorMin = new Vector2(0, 1);
            top.offsetMin = new Vector2(0, -3);
            var bottom = ImageRect(rect, "DarkEdge", new Color32(45, 45, 45, 255));
            Stretch(bottom, Vector2.zero, Vector2.zero);
            bottom.anchorMax = new Vector2(1, 0);
            bottom.offsetMax = new Vector2(0, 3);
            top.GetComponent<Image>().raycastTarget = bottom.GetComponent<Image>().raycastTarget = false;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, bool top = false)
        {
            rect.anchorMin = top ? Vector2.up : Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
        }

        private static void PlaceFooter(RectTransform rect, float x)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(156, 44);
            rect.anchoredPosition = new Vector2(x, 38);
        }
    }
}
