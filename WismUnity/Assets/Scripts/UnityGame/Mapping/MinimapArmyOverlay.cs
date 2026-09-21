using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Core;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class MinimapArmyOverlay : MaskableGraphic
{
    private readonly List<Vector2> centers = new List<Vector2>();
    private readonly List<Color32> colors = new List<Color32>();
    public int MarkerCount => centers.Count;

    public void Bind(UnityManager manager, Camera camera)
    {
        raycastTarget = false;
        centers.Clear();
        colors.Clear();
        foreach (var player in Game.Current.Players)
        {
            var color = MinimapCityOverlay.ResolveColor(player.Clan.Info.PrimaryColor ?? player.Clan.Info.Color);
            foreach (var stack in player.GetArmies().Where(army => !army.IsDead && army.Tile != null).GroupBy(army => army.Tile))
            {
                var position = manager.WorldTilemap.ConvertGameToUnityVector(stack.Key.X, stack.Key.Y);
                var point = camera.WorldToViewportPoint(position);
                centers.Add(new Vector2(point.x, point.y));
                colors.Add(color);
            }
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
            Quad(mesh, center, 3, Color.white, rect);
            Quad(mesh, center, 2, colors[i], rect);
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
