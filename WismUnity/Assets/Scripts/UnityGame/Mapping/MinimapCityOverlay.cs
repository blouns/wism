using System.Collections.Generic;
using Assets.Scripts.Tilemaps;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

// A visual layer only: the existing minimap owns all pointer handling.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MinimapCityOverlay : MaskableGraphic
{
    private readonly List<Marker> markers = new List<Marker>();
    private Camera mapCamera;
    private WorldTilemap worldTilemap;
    private readonly Vector3[] corners = new Vector3[4];
    private Vector2 pixelsPerUnit;
    public int MarkerCount => markers.Count;
    public System.Func<City, Color32> ColorOverride { get; set; }

    private sealed class Marker
    {
        public City City;
        public Vector2 Center;
        public Color32 Color;
        public bool Razed;
        public string ColorSource;
    }

    public void Bind(World world, Camera camera, WorldTilemap tilemap)
    {
        raycastTarget = false;
        mapCamera = camera;
        worldTilemap = tilemap;
        markers.Clear();
        if (world != null)
            foreach (var city in world.GetCities())
                if (city.Tile != null) markers.Add(new Marker { City = city });
        Refresh();
        SetVerticesDirty();
    }

    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (mapCamera == null || worldTilemap == null) return;
        bool changed = false;
        foreach (var marker in markers)
        {
            var city = marker.City;
            if (city.Tile == null) continue;
            // City.Tile is the upper-left cell of a two-by-two footprint.
            var upperLeft = worldTilemap.ConvertGameToUnityVector(city.X, city.Y);
            var lowerRight = worldTilemap.ConvertGameToUnityVector(city.X + 1, city.Y - 1);
            var projected = mapCamera.WorldToViewportPoint((upperLeft + lowerRight) * .5f);
            var center = new Vector2(projected.x, projected.y);
            bool razed = !ReferenceEquals(city.Tile.City, city);
            string source = razed ? null : city.Player?.Clan.Info.PrimaryColor ?? city.Player?.Clan.Info.Color;
            var color = ColorOverride != null ? ColorOverride(city) : ResolveColor(source);
            if (marker.Center != center || marker.Razed != razed || !marker.Color.Equals(color))
                changed = true;
            marker.Color = color;
            marker.Center = center;
            marker.Razed = razed;
            marker.ColorSource = source;
        }
        var scale = ScreenPixelsPerUnit();
        if (scale != pixelsPerUnit) { pixelsPerUnit = scale; changed = true; }
        if (changed) SetVerticesDirty();
    }

    public City HitTestCity(Vector2 screenPoint, Camera eventCamera)
    {
        Refresh();
        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, eventCamera)) return null;
        City closest = null;
        float distance = 8f * 8f;
        foreach (var marker in markers)
        {
            if (marker.Razed) continue;
            var local = rectTransform.rect.min + Vector2.Scale(rectTransform.rect.size, marker.Center);
            var point = RectTransformUtility.WorldToScreenPoint(eventCamera, rectTransform.TransformPoint(local));
            float squared = (point - screenPoint).sqrMagnitude;
            if (squared < distance) { closest = marker.City; distance = squared; }
        }
        return closest;
    }

    public static Color32 ResolveColor(string value)
    {
        return ClanInfo.TryParseRgb(value, out var r, out var g, out var b)
            ? new Color32(r, g, b, 255) : new Color32(160, 160, 160, 255);
    }

    private Vector2 ScreenPixelsPerUnit()
    {
        rectTransform.GetWorldCorners(corners);
        var root = canvas != null ? canvas.rootCanvas : null;
        var camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
        var lower = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        var upper = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        var size = rectTransform.rect.size;
        return new Vector2(size.x > 0 ? Mathf.Abs(upper.x - lower.x) / size.x : 1f,
            size.y > 0 ? Mathf.Abs(upper.y - lower.y) / size.y : 1f);
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return;
        var unit = new Vector2(1f / Mathf.Max(.001f, pixelsPerUnit.x),
            1f / Mathf.Max(.001f, pixelsPerUnit.y));
        foreach (var marker in markers)
        {
            if (marker.Center.x < 0 || marker.Center.x > 1 || marker.Center.y < 0 || marker.Center.y > 1) continue;
            var center = rect.min + Vector2.Scale(rect.size, marker.Center);
            var outer = new Rect(center - unit * 3.5f, unit * 7f);
            if (marker.Razed)
            {
                AddQuad(mesh, new Rect(outer.xMin, outer.yMin, outer.width, unit.y), rect, marker.Color);
                AddQuad(mesh, new Rect(outer.xMin, outer.yMax - unit.y, outer.width, unit.y), rect, marker.Color);
                AddQuad(mesh, new Rect(outer.xMin, outer.yMin, unit.x, outer.height), rect, marker.Color);
                AddQuad(mesh, new Rect(outer.xMax - unit.x, outer.yMin, unit.x, outer.height), rect, marker.Color);
            }
            else
            {
                AddQuad(mesh, outer, rect, Color.black);
                AddQuad(mesh, new Rect(center - unit * 2.5f, unit * 5f), rect, Color.white);
                AddQuad(mesh, new Rect(center - unit * 1.5f, unit * 3f), rect, marker.Color);
            }
        }
    }

    private static void AddQuad(VertexHelper mesh, Rect area, Rect clip, Color32 color)
    {
        var min = Vector2.Max(area.min, clip.min);
        var max = Vector2.Min(area.max, clip.max);
        if (min.x >= max.x || min.y >= max.y) return;
        int start = mesh.currentVertCount;
        mesh.AddVert(new Vector3(min.x, min.y), color, Vector2.zero);
        mesh.AddVert(new Vector3(min.x, max.y), color, Vector2.zero);
        mesh.AddVert(new Vector3(max.x, max.y), color, Vector2.zero);
        mesh.AddVert(new Vector3(max.x, min.y), color, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start + 2, start + 3, start);
    }
}
