using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public static class ProductionManagementUi
    {
        public const float MinimapWidth = 180f;
        public const float MinimapHeight = 76f;
        public const float MarkerSize = 10f;

        static readonly Regex InvalidNameCharacters = new Regex("[^A-Za-z0-9_-]+", RegexOptions.Compiled);

        public static RectTransform CreateMinimapPanel(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);

            var rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(MinimapWidth, MinimapHeight);

            var image = gameObject.GetComponent<Image>();
            image.color = WismUiTheme.Classic.MinimapBackground;
            image.raycastTarget = false;

            var layout = gameObject.GetComponent<LayoutElement>();
            layout.minWidth = MinimapWidth;
            layout.preferredWidth = MinimapWidth;
            layout.minHeight = MinimapHeight;
            layout.preferredHeight = MinimapHeight;

            return rect;
        }

        public static IReadOnlyList<GameObject> RebuildMinimapMarkers(
            RectTransform minimapPanel,
            IReadOnlyList<ProductionMinimapMarkerViewModel> markers)
        {
            if (minimapPanel == null)
            {
                return new GameObject[0];
            }

            for (var i = minimapPanel.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(minimapPanel.GetChild(i).gameObject);
            }

            if (markers == null || markers.Count == 0)
            {
                return new GameObject[0];
            }

            var bounds = ProductionMarkerBounds.FromMarkers(markers);
            var created = new List<GameObject>();
            for (var i = 0; i < markers.Count; i++)
            {
                created.Add(CreateMarker(minimapPanel, markers[i], bounds, i));
            }

            return created;
        }

        public static Color MarkerColor(string kind)
        {
            switch (kind)
            {
                case "selected":
                    return WismUiTheme.Classic.ButtonSelected;
                case "producing":
                    return new Color32(84, 168, 84, 255);
                case "idle":
                    return WismUiTheme.Classic.MutedText;
                case "redirect-target":
                    return WismUiTheme.Classic.Danger;
                case "delivery-target":
                    return new Color32(190, 92, 186, 255);
                case "receiver":
                    return new Color32(92, 148, 204, 255);
                default:
                    return WismUiTheme.Classic.Text;
            }
        }

        public static Vector2 MarkerPosition(
            ProductionMinimapMarkerViewModel marker,
            IReadOnlyList<ProductionMinimapMarkerViewModel> markers)
        {
            return MarkerPosition(marker, ProductionMarkerBounds.FromMarkers(markers));
        }

        static GameObject CreateMarker(
            RectTransform parent,
            ProductionMinimapMarkerViewModel marker,
            ProductionMarkerBounds bounds,
            int index)
        {
            var gameObject = new GameObject(MarkerObjectName(marker, index), typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);

            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(MarkerSize, MarkerSize);
            rect.anchoredPosition = MarkerPosition(marker, bounds);

            var image = gameObject.GetComponent<Image>();
            image.color = MarkerColor(marker.Kind);
            image.raycastTarget = false;

            return gameObject;
        }

        static Vector2 MarkerPosition(ProductionMinimapMarkerViewModel marker, ProductionMarkerBounds bounds)
        {
            var x = marker?.City?.Tile?.X ?? 0;
            var y = marker?.City?.Tile?.Y ?? 0;
            var normalizedX = bounds.NormalizeX(x);
            var normalizedY = bounds.NormalizeY(y);
            return new Vector2(
                MarkerSize + normalizedX * (MinimapWidth - MarkerSize * 2f),
                MarkerSize + normalizedY * (MinimapHeight - MarkerSize * 2f));
        }

        static string MarkerObjectName(ProductionMinimapMarkerViewModel marker, int index)
        {
            var cityName = marker?.City?.DisplayName ?? marker?.City?.ShortName ?? "Unknown";
            return $"ProductionMinimapMarker_{index}_{Sanitize(marker?.Kind)}_{Sanitize(cityName)}";
        }

        static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            return InvalidNameCharacters.Replace(value, "-").Trim('-');
        }

        readonly struct ProductionMarkerBounds
        {
            readonly int minX;
            readonly int maxX;
            readonly int minY;
            readonly int maxY;

            ProductionMarkerBounds(int minX, int maxX, int minY, int maxY)
            {
                this.minX = minX;
                this.maxX = maxX;
                this.minY = minY;
                this.maxY = maxY;
            }

            public static ProductionMarkerBounds FromMarkers(IReadOnlyList<ProductionMinimapMarkerViewModel> markers)
            {
                var positioned = markers?
                    .Where(marker => marker?.City?.Tile != null)
                    .Select(marker => marker.City.Tile)
                    .ToArray() ?? new Wism.Client.Core.Tile[0];

                if (positioned.Length == 0)
                {
                    return new ProductionMarkerBounds(0, 1, 0, 1);
                }

                return new ProductionMarkerBounds(
                    positioned.Min(tile => tile.X),
                    positioned.Max(tile => tile.X),
                    positioned.Min(tile => tile.Y),
                    positioned.Max(tile => tile.Y));
            }

            public float NormalizeX(int x)
            {
                return this.maxX == this.minX ? 0.5f : Mathf.InverseLerp(this.minX, this.maxX, x);
            }

            public float NormalizeY(int y)
            {
                return this.maxY == this.minY ? 0.5f : Mathf.InverseLerp(this.minY, this.maxY, y);
            }
        }
    }
}
