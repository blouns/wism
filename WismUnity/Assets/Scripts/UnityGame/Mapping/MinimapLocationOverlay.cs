using System.Collections.Generic;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.MapObjects;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class MinimapLocationOverlay : MaskableGraphic
{
    private readonly List<Vector2> centers = new List<Vector2>();
    private readonly List<bool> searched = new List<bool>();
    public int MarkerCount => centers.Count;

    public void Bind(UnityManager manager, Camera camera, IEnumerable<Location> locations)
    {
        raycastTarget = false;
        centers.Clear();
        searched.Clear();
        foreach (var location in locations)
        {
            var position = manager.WorldTilemap.ConvertGameToUnityVector(location.X, location.Y);
            var point = camera.WorldToViewportPoint(position);
            centers.Add(new Vector2(point.x, point.y));
            searched.Add(location.Searched);
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return;
        for (int i = 0; i < centers.Count; i++)
        {
            var point = centers[i];
            if (point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1) continue;
            var center = rect.min + Vector2.Scale(point, rect.size);
            Quad(mesh, center, 4, Color.black, rect);
            Quad(mesh, center, 3, searched[i] ? new Color32(185, 185, 185, 255) : new Color32(245, 208, 72, 255), rect);
            if (searched[i]) Quad(mesh, center, 1.5f, Color.black, rect);
        }
    }

    private static void Quad(VertexHelper mesh, Vector2 center, float radius, Color32 color, Rect clip)
    {
        var min = Vector2.Max(center - Vector2.one * radius, clip.min);
        var max = Vector2.Min(center + Vector2.one * radius, clip.max);
        int start = mesh.currentVertCount;
        mesh.AddVert(new Vector3(min.x, min.y), color, Vector2.zero);
        mesh.AddVert(new Vector3(min.x, max.y), color, Vector2.zero);
        mesh.AddVert(new Vector3(max.x, max.y), color, Vector2.zero);
        mesh.AddVert(new Vector3(max.x, min.y), color, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start + 2, start + 3, start);
    }
}
