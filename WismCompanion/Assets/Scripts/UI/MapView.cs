using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace WismCompanion.UI
{
    /// <summary>The entities found under a clicked map tile, surfaced to the inspector.</summary>
    public readonly struct MapSelection
    {
        public MapSelection(int x, int y, TileDto tile, ArmyDto army, CityDto city, LocationDto location)
        {
            X = x;
            Y = y;
            Tile = tile;
            Army = army;
            City = city;
            Location = location;
        }

        public int X { get; }

        public int Y { get; }

        public TileDto Tile { get; }

        public ArmyDto Army { get; }

        public CityDto City { get; }

        public LocationDto Location { get; }
    }

    /// <summary>
    /// UI Toolkit map + minimap, drawn with Painter2D. Geometry (tile viewport clamp, Y inversion,
    /// minimap aspect, castle/diamond glyphs) is ported from the WinForms companion MapRenderer.
    /// Repaints are event-driven (data change / pan / zoom), not per-frame, to keep overhead low.
    /// </summary>
    public sealed class MapView : VisualElement
    {
        private const float Padding = 8f;
        private const float MinTile = 10f;
        private const float MaxTile = 64f;

        private MapSnapshot map;
        private float tileSize = 28f;
        private Vector2 cameraCenter;
        private bool cameraInitialized;
        private Layout mapLayout;
        private bool dragging;
        private Vector2 lastPointer;

        public event Action<MapSelection> SelectionChanged;

        public MapView()
        {
            style.flexGrow = 1f;
            focusable = true;
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        }

        public void SetSnapshot(MapSnapshot snapshot)
        {
            map = snapshot;
            if (snapshot != null)
            {
                if (snapshot.SelectedArmy?.Position != null)
                {
                    cameraCenter = new Vector2(snapshot.SelectedArmy.Position.X, snapshot.SelectedArmy.Position.Y);
                    cameraInitialized = true;
                }
                else if (!cameraInitialized)
                {
                    if (snapshot.Armies != null && snapshot.Armies.Count > 0 && snapshot.Armies[0].Position != null)
                    {
                        cameraCenter = new Vector2(snapshot.Armies[0].Position.X, snapshot.Armies[0].Position.Y);
                    }
                    else
                    {
                        cameraCenter = new Vector2(snapshot.Width / 2f, snapshot.Height / 2f);
                    }

                    cameraInitialized = true;
                }
            }

            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            mapLayout = ComputeLayout();
            var p = mgc.painter2D;

            FillRect(p, contentRect, new Color(0.18f, 0.18f, 0.19f));
            if (!mapLayout.Valid)
            {
                return;
            }

            DrawViewport(p);
            DrawMinimap(p);
        }

        private Layout ComputeLayout()
        {
            var content = contentRect;
            var result = new Layout();
            if (map == null || map.Width <= 0 || map.Height <= 0 || content.width < 4f || content.height < 4f)
            {
                return result;
            }

            var minimapWidth = Mathf.Min(220f, content.width * 0.33f);
            var viewportWidth = Mathf.Max(MinTile, content.width - minimapWidth - Padding * 3f);
            var viewportHeight = Mathf.Max(MinTile, content.height - Padding * 2f);

            var tilesW = Mathf.Clamp(Mathf.FloorToInt(viewportWidth / tileSize), 1, map.Width);
            var tilesH = Mathf.Clamp(Mathf.FloorToInt(viewportHeight / tileSize), 1, map.Height);

            var maxOriginX = Mathf.Max(0, map.Width - tilesW);
            var maxOriginY = Mathf.Max(0, map.Height - tilesH);

            result.Tile = tileSize;
            result.TilesW = tilesW;
            result.TilesH = tilesH;
            result.OriginX = Mathf.Clamp(Mathf.RoundToInt(cameraCenter.x - tilesW / 2f), 0, maxOriginX);
            result.OriginY = Mathf.Clamp(Mathf.RoundToInt(cameraCenter.y - tilesH / 2f), 0, maxOriginY);
            result.Viewport = new Rect(content.x + Padding, content.y + Padding, tilesW * tileSize, tilesH * tileSize);

            var minimapX = result.Viewport.xMax + Padding;
            var mmW = Mathf.Max(1f, content.xMax - minimapX - Padding);
            var mmH = Mathf.Max(1f, content.height - Padding * 2f);
            var mapAspect = map.Width / (float)map.Height;
            var panelAspect = mmW / mmH;
            if (panelAspect > mapAspect)
            {
                mmW = mmH * mapAspect;
            }
            else
            {
                mmH = mmW / mapAspect;
            }

            result.Minimap = new Rect(minimapX, content.y + Padding, mmW, mmH);
            result.Valid = true;
            return result;
        }

        private void DrawViewport(Painter2D p)
        {
            FillRect(p, mapLayout.Viewport, Color.black);

            if (map.Tiles != null)
            {
                foreach (var tile in map.Tiles)
                {
                    if (tile.X < mapLayout.OriginX || tile.Y < mapLayout.OriginY ||
                        tile.X >= mapLayout.OriginX + mapLayout.TilesW || tile.Y >= mapLayout.OriginY + mapLayout.TilesH)
                    {
                        continue;
                    }

                    var pos = MapToViewport(tile.X, tile.Y);
                    var rect = new Rect(pos.x, pos.y, mapLayout.Tile, mapLayout.Tile);
                    FillRect(p, rect, MapColors.ColorForTerrain(tile.TerrainType));
                    StrokeRect(p, rect, new Color(0f, 0f, 0f, 0.18f), 1f);
                }
            }

            if (map.Cities != null)
            {
                foreach (var city in map.Cities)
                {
                    DrawCity(p, city);
                }
            }

            if (map.Armies != null)
            {
                foreach (var army in map.Armies)
                {
                    DrawArmy(p, army);
                }
            }

            if (map.SelectedArmy?.Position != null)
            {
                var pos = MapToViewport(map.SelectedArmy.Position.X, map.SelectedArmy.Position.Y);
                StrokeRect(p, new Rect(pos.x + 3f, pos.y + 3f, mapLayout.Tile - 6f, mapLayout.Tile - 6f), Color.yellow, 2f);
            }

            StrokeRect(p, mapLayout.Viewport, new Color(0f, 0f, 0f, 0.8f), 2f);
        }

        private void DrawMinimap(Painter2D p)
        {
            var mm = mapLayout.Minimap;
            FillRect(p, mm, new Color(0.22f, 0.22f, 0.22f));

            var tw = mm.width / map.Width;
            var th = mm.height / map.Height;

            if (map.Tiles != null)
            {
                foreach (var tile in map.Tiles)
                {
                    var x = mm.x + tile.X * tw;
                    var y = mm.y + (map.Height - 1 - tile.Y) * th;
                    FillRect(p, new Rect(x, y, Mathf.Max(1f, tw), Mathf.Max(1f, th)), MapColors.ColorForTerrain(tile.TerrainType));
                }
            }

            if (map.Cities != null)
            {
                foreach (var city in map.Cities)
                {
                    if (city.Position == null)
                    {
                        continue;
                    }

                    var x = mm.x + city.Position.X * tw;
                    var y = mm.y + (map.Height - 1 - city.Position.Y) * th;
                    FillRect(p, new Rect(x - 2f, y - 2f, 5f, 5f), MapColors.ClanColor(city.Owner));
                }
            }

            if (map.Armies != null)
            {
                foreach (var army in map.Armies)
                {
                    if (army.Position == null)
                    {
                        continue;
                    }

                    var x = mm.x + army.Position.X * tw;
                    var y = mm.y + (map.Height - 1 - army.Position.Y) * th;
                    FillRect(p, new Rect(x - 1f, y - 1f, 3f, 3f), MapColors.ClanColor(army.Owner));
                }
            }

            var vx = mm.x + mapLayout.OriginX * tw;
            var vy = mm.y + (map.Height - mapLayout.OriginY - mapLayout.TilesH) * th;
            StrokeRect(p, new Rect(vx, vy, mapLayout.TilesW * tw, mapLayout.TilesH * th), Color.white, 2f);
            StrokeRect(p, mm, new Color(0f, 0f, 0f, 0.8f), 2f);
        }

        private void DrawCity(Painter2D p, CityDto city)
        {
            if (city.Position == null)
            {
                return;
            }

            for (var dx = 0; dx < 2; dx++)
            {
                for (var dy = 0; dy < 2; dy++)
                {
                    var gx = city.Position.X + dx;
                    var gy = city.Position.Y - dy;
                    if (gx < mapLayout.OriginX || gy < mapLayout.OriginY ||
                        gx >= mapLayout.OriginX + mapLayout.TilesW || gy >= mapLayout.OriginY + mapLayout.TilesH)
                    {
                        continue;
                    }

                    var pos = MapToViewport(gx, gy);
                    var t = mapLayout.Tile;
                    var inner = new Rect(pos.x + t * 0.12f, pos.y + t * 0.12f, t * 0.76f, t * 0.76f);
                    FillRect(p, inner, new Color(0.82f, 0.82f, 0.82f));
                    StrokeRect(p, inner, MapColors.ClanColor(city.Owner), Mathf.Max(2f, t * 0.08f));
                    FillRect(p, new Rect(pos.x + t * 0.30f, pos.y + t * 0.18f, t * 0.12f, t * 0.30f), new Color(0.55f, 0.1f, 0.1f));
                    FillRect(p, new Rect(pos.x + t * 0.58f, pos.y + t * 0.18f, t * 0.12f, t * 0.30f), new Color(0.55f, 0.1f, 0.1f));
                }
            }
        }

        private void DrawArmy(Painter2D p, ArmyDto army)
        {
            if (army.Position == null)
            {
                return;
            }

            var gx = army.Position.X;
            var gy = army.Position.Y;
            if (gx < mapLayout.OriginX || gy < mapLayout.OriginY ||
                gx >= mapLayout.OriginX + mapLayout.TilesW || gy >= mapLayout.OriginY + mapLayout.TilesH)
            {
                return;
            }

            var pos = MapToViewport(gx, gy);
            var t = mapLayout.Tile;
            var cx = pos.x + t / 2f;
            var cy = pos.y + t / 2f;
            var r = t * 0.28f;

            p.fillColor = MapColors.ClanColor(army.Owner);
            p.BeginPath();
            p.MoveTo(new Vector2(cx, cy - r));
            p.LineTo(new Vector2(cx + r, cy));
            p.LineTo(new Vector2(cx, cy + r));
            p.LineTo(new Vector2(cx - r, cy));
            p.ClosePath();
            p.Fill();

            p.strokeColor = army.IsHero ? Color.yellow : Color.black;
            p.lineWidth = army.IsHero ? 2f : 1f;
            p.Stroke();
        }

        private Vector2 MapToViewport(int mapX, int mapY)
        {
            var relX = mapX - mapLayout.OriginX;
            var relY = mapY - mapLayout.OriginY;
            var screenRow = map.InvertYAxis ? mapLayout.TilesH - 1 - relY : relY;
            return new Vector2(mapLayout.Viewport.x + relX * mapLayout.Tile, mapLayout.Viewport.y + screenRow * mapLayout.Tile);
        }

        private void OnWheel(WheelEvent e)
        {
            tileSize = Mathf.Clamp(tileSize - e.delta.y * 2f, MinTile, MaxTile);
            MarkDirtyRepaint();
            e.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent e)
        {
            if (map == null)
            {
                return;
            }

            var local = new Vector2(e.localPosition.x, e.localPosition.y);

            if (mapLayout.Valid && mapLayout.Minimap.Contains(local))
            {
                RecenterFromMinimap(local);
                e.StopPropagation();
                return;
            }

            if (e.button == 0 && mapLayout.Valid && mapLayout.Viewport.Contains(local))
            {
                RaiseSelection(ViewportToMap(local));
            }

            dragging = true;
            lastPointer = local;
            this.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent e)
        {
            if (!dragging || map == null || tileSize <= 0f)
            {
                return;
            }

            var local = new Vector2(e.localPosition.x, e.localPosition.y);
            var delta = local - lastPointer;
            lastPointer = local;

            cameraCenter.x -= delta.x / tileSize;
            cameraCenter.y += (map.InvertYAxis ? delta.y : -delta.y) / tileSize;
            cameraCenter.x = Mathf.Clamp(cameraCenter.x, 0f, Mathf.Max(0f, map.Width - 1f));
            cameraCenter.y = Mathf.Clamp(cameraCenter.y, 0f, Mathf.Max(0f, map.Height - 1f));
            cameraInitialized = true;
            MarkDirtyRepaint();
        }

        private void OnPointerUp(PointerUpEvent e)
        {
            dragging = false;
            if (this.HasPointerCapture(e.pointerId))
            {
                this.ReleasePointer(e.pointerId);
            }
        }

        private Vector2Int ViewportToMap(Vector2 local)
        {
            var relX = Mathf.FloorToInt((local.x - mapLayout.Viewport.x) / mapLayout.Tile);
            var relY = Mathf.FloorToInt((local.y - mapLayout.Viewport.y) / mapLayout.Tile);
            var mapX = mapLayout.OriginX + relX;
            var mapY = map.InvertYAxis ? mapLayout.OriginY + mapLayout.TilesH - 1 - relY : mapLayout.OriginY + relY;
            return new Vector2Int(mapX, mapY);
        }

        private void RecenterFromMinimap(Vector2 local)
        {
            var mm = mapLayout.Minimap;
            var fx = (local.x - mm.x) / mm.width;
            var fyTop = (local.y - mm.y) / mm.height;
            var mx = Mathf.Clamp(Mathf.RoundToInt(fx * map.Width), 0, Mathf.Max(0, map.Width - 1));
            var myFromTop = Mathf.RoundToInt(fyTop * map.Height);
            var my = Mathf.Clamp(map.Height - 1 - myFromTop, 0, Mathf.Max(0, map.Height - 1));
            cameraCenter = new Vector2(mx, my);
            cameraInitialized = true;
            MarkDirtyRepaint();
        }

        private void RaiseSelection(Vector2Int coord)
        {
            if (SelectionChanged == null || map == null)
            {
                return;
            }

            TileDto tile = null;
            ArmyDto army = null;
            CityDto city = null;
            LocationDto location = null;

            if (map.Tiles != null)
            {
                foreach (var t in map.Tiles)
                {
                    if (t.X == coord.x && t.Y == coord.y)
                    {
                        tile = t;
                        break;
                    }
                }
            }

            if (map.Armies != null)
            {
                foreach (var a in map.Armies)
                {
                    if (a.Position != null && a.Position.X == coord.x && a.Position.Y == coord.y)
                    {
                        army = a;
                        break;
                    }
                }
            }

            if (map.Cities != null)
            {
                foreach (var c in map.Cities)
                {
                    if (c.Position != null && c.Position.X == coord.x && c.Position.Y == coord.y)
                    {
                        city = c;
                        break;
                    }
                }
            }

            if (map.Locations != null)
            {
                foreach (var l in map.Locations)
                {
                    if (l.Position != null && l.Position.X == coord.x && l.Position.Y == coord.y)
                    {
                        location = l;
                        break;
                    }
                }
            }

            SelectionChanged.Invoke(new MapSelection(coord.x, coord.y, tile, army, city, location));
        }

        private static void FillRect(Painter2D p, Rect r, Color color)
        {
            p.fillColor = color;
            p.BeginPath();
            p.MoveTo(new Vector2(r.xMin, r.yMin));
            p.LineTo(new Vector2(r.xMax, r.yMin));
            p.LineTo(new Vector2(r.xMax, r.yMax));
            p.LineTo(new Vector2(r.xMin, r.yMax));
            p.ClosePath();
            p.Fill();
        }

        private static void StrokeRect(Painter2D p, Rect r, Color color, float width)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.BeginPath();
            p.MoveTo(new Vector2(r.xMin, r.yMin));
            p.LineTo(new Vector2(r.xMax, r.yMin));
            p.LineTo(new Vector2(r.xMax, r.yMax));
            p.LineTo(new Vector2(r.xMin, r.yMax));
            p.ClosePath();
            p.Stroke();
        }

        private struct Layout
        {
            public bool Valid;
            public Rect Viewport;
            public Rect Minimap;
            public int OriginX;
            public int OriginY;
            public int TilesW;
            public int TilesH;
            public float Tile;
        }
    }
}
