using System;
using System.Globalization;
using System.Linq;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wism.Client.Core;

namespace Assets.Scripts.UI
{
    public sealed class StrategicReportView : MonoBehaviour, IPointerClickHandler
    {
        private UnityManager manager;
        private Font font;
        private RectTransform content;
        private ScrollRect scroll;
        private Text heading;
        private Text metric;
        private Button[] tabs;
        private RawImage map;
        private MinimapCityOverlay cities;
        private MinimapArmyOverlay armies;
        public ClanReportRow[] Rows { get; private set; }
        public double[] Values { get; private set; }
        public StrategicReportKind Kind { get; private set; }

        public void Initialize(UnityManager owner, Font face, Action close)
        {
            manager = owner;
            font = face;
            gameObject.AddComponent<Image>().color = new Color32(128, 128, 128, 255);
            heading = Label(transform, "ReportHeading", "Reports", 28);
            Place(heading.rectTransform, 20, 16, 420, 42);
            var exit = MakeButton(transform, "CloseReport", "Close", close);
            var exitRect = (RectTransform)exit.transform;
            exitRect.anchorMin = exitRect.anchorMax = exitRect.pivot = Vector2.one;
            exitRect.anchoredPosition = new Vector2(-20, -16);
            exitRect.sizeDelta = new Vector2(100, 44);
            tabs = new Button[4];
            for (int i = 0; i < tabs.Length; i++)
            {
                var kind = (StrategicReportKind)i;
                tabs[i] = MakeButton(transform, "Report" + kind, kind.ToString(), () => Show(kind));
                Place((RectTransform)tabs[i].transform, 20 + i * 120, 72, 112, 44);
            }
            metric = Label(transform, "ReportMetric", "", 16);
            var metricRect = metric.rectTransform;
            metricRect.anchorMin = new Vector2(0, 1);
            metricRect.anchorMax = Vector2.one;
            metricRect.offsetMin = new Vector2(20, -164);
            metricRect.offsetMax = new Vector2(-20, -122);

            var viewport = Rect(transform, "ReportRows");
            viewport.gameObject.AddComponent<Image>().color = new Color32(117, 117, 117, 255);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = new Vector2(.62f, 1);
            viewport.offsetMin = new Vector2(20, 20);
            viewport.offsetMax = new Vector2(-22, -176);
            content = Rect(viewport, "ReportContent");
            content.anchorMin = Vector2.up;
            content.anchorMax = Vector2.one;
            content.pivot = Vector2.up;
            content.offsetMin = content.offsetMax = Vector2.zero;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            var barRect = Rect(transform, "ReportScrollbar");
            barRect.anchorMin = new Vector2(.62f, 0);
            barRect.anchorMax = new Vector2(.62f, 1);
            barRect.offsetMin = new Vector2(-12, 20);
            barRect.offsetMax = new Vector2(2, -176);
            barRect.gameObject.AddComponent<Image>().color = new Color32(65, 65, 65, 255);
            var handle = Rect(barRect, "Handle");
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color32(200, 200, 200, 255);
            var scrollbar = barRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            var mapArea = Rect(transform, "ReportMapArea");
            mapArea.anchorMin = new Vector2(.62f, 0);
            mapArea.anchorMax = Vector2.one;
            mapArea.offsetMin = new Vector2(8, 20);
            mapArea.offsetMax = new Vector2(-20, -176);
            var mapRect = Rect(mapArea, "ReportMap");
            map = mapRect.gameObject.AddComponent<RawImage>();
            map.raycastTarget = false;
            var ratio = mapRect.gameObject.AddComponent<AspectRatioFitter>();
            ratio.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            cities = Rect(mapRect, "ReportCities").gameObject.AddComponent<MinimapCityOverlay>();
            armies = Rect(mapRect, "ReportArmies").gameObject.AddComponent<MinimapArmyOverlay>();
        }

        public void Open(StrategicReportKind kind)
        {
            gameObject.SetActive(true);
            map.texture = UnityUtilities.GameObjectHardFind("Minimap").GetComponent<RawImage>().texture;
            map.GetComponent<AspectRatioFitter>().aspectRatio = (float)World.Current.Map.GetLength(0) / World.Current.Map.GetLength(1);
            var camera = GameObject.FindGameObjectWithTag("MinimapCamera").GetComponent<Camera>();
            cities.Bind(World.Current, camera, manager.WorldTilemap);
            armies.Bind(manager, camera);
            Render(StrategicReportData.Capture(Game.Current), kind);
        }

        public void Render(ClanReportRow[] rows, StrategicReportKind kind)
        {
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            Show(kind);
        }

        public void Show(StrategicReportKind kind)
        {
            if (Rows == null) return;
            Kind = kind;
            Values = StrategicReportData.Values(Rows, kind);
            heading.text = kind == StrategicReportKind.Winning ? "Winning (estimate)" : kind.ToString();
            metric.text = kind == StrategicReportKind.Winning ? "City share 50%  /  Strength share 35%  /  Gold share 15%" :
                kind == StrategicReportKind.Armies ? "Living units by clan; map markers show occupied army tiles" :
                kind == StrategicReportKind.Cities ? "Owned cities by clan" : "Treasury by clan (gp)";
            for (int i = 0; i < tabs.Length; i++) tabs[i].image.color = i == (int)kind ? new Color32(226, 211, 152, 255) : new Color32(185, 185, 185, 255);
            cities.gameObject.SetActive(kind != StrategicReportKind.Armies);
            armies.gameObject.SetActive(kind == StrategicReportKind.Armies);
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            content.sizeDelta = new Vector2(0, Rows.Length * 52);
            double max = kind == StrategicReportKind.Winning ? 100 : Values.DefaultIfEmpty(0).Max();
            for (int i = 0; i < Rows.Length; i++)
            {
                var row = Rect(content, "ClanRow" + i);
                row.anchorMin = Vector2.up;
                row.anchorMax = Vector2.one;
                row.pivot = Vector2.up;
                row.sizeDelta = new Vector2(0, 52);
                row.anchoredPosition = new Vector2(0, -i * 52);
                var label = Label(row, "Clan", Rows[i].Name + (Rows[i].Eliminated ? " (out)" : ""), 18);
                Place(label.rectTransform, 8, 2, 154, 48);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 10;
                label.resizeTextMaxSize = 18;
                var value = Label(row, "Value", Values[i].ToString(kind == StrategicReportKind.Winning ? "F1" : "N0", CultureInfo.InvariantCulture), 18);
                value.alignment = TextAnchor.MiddleRight;
                value.rectTransform.anchorMin = value.rectTransform.anchorMax = value.rectTransform.pivot = Vector2.one;
                value.rectTransform.anchoredPosition = new Vector2(-8, -2);
                value.rectTransform.sizeDelta = new Vector2(120, 48);
                var track = Rect(row, "BarTrack");
                track.anchorMin = Vector2.zero;
                track.anchorMax = Vector2.one;
                track.offsetMin = new Vector2(170, 14);
                track.offsetMax = new Vector2(-136, -14);
                track.gameObject.AddComponent<Image>().color = new Color32(65, 65, 65, 255);
                var bar = Rect(track, "ClanBar");
                bar.anchorMax = new Vector2(max > 0 ? (float)(Values[i] / max) : 0, 1);
                bar.gameObject.AddComponent<Image>().color = MinimapCityOverlay.ResolveColor(Rows[i].Color);
            }
            scroll.verticalNormalizedPosition = 1;
        }

        public void OnPointerClick(PointerEventData eventData) { }

        private Text Label(Transform parent, string name, string value, int size)
        {
            var text = WismUiFactory.CreateText(parent, name, value, size);
            text.font = font;
            text.color = Color.black;
            return text;
        }

        private Button MakeButton(Transform parent, string name, string label, Action action)
        {
            var button = WismUiFactory.CreateButton(parent, name, label, "reports." + name, "reports." + name);
            button.image.color = new Color32(185, 185, 185, 255);
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = colors.selectedColor = new Color32(235, 225, 190, 255);
            button.colors = colors;
            var text = button.GetComponentInChildren<Text>();
            text.font = font;
            text.fontSize = 20;
            text.color = Color.black;
            button.onClick.AddListener(() => action());
            return button;
        }

        private static RectTransform Rect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
