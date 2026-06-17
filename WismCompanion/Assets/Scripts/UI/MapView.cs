using System;
using System.Collections.Generic;
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
            X = x; Y = y; Tile = tile; Army = army; City = city; Location = location;
        }

        public int X { get; }
        public int Y { get; }
        public TileDto Tile { get; }
        public ArmyDto Army { get; }
        public CityDto City { get; }
        public LocationDto Location { get; }
    }

    /// <summary>
    /// UI Toolkit map + minimap. Terrain is rendered with Warlords Classic sprites using the same
    /// 14-way adjacency algorithm as WismUnity's TileUtility. Cities use per-clan 4-quadrant castle
    /// sprites; armies show the unit sprite with a flag overlay indicating stack depth.
    /// Repaints are event-driven, not per-frame.
    /// </summary>
    public sealed class MapView : VisualElement
    {
        private const float Padding = 8f;
        private const float MinTile = 10f;
        private const float MaxTile = 64f;

        private MapSnapshot map;
        private float tileSize = 28f;
        private bool tileInitialized;
        private int lastMapW, lastMapH;
        private Vector2 cameraCenter;
        private bool cameraInitialized;
        private Layout mapLayout;
        private bool dragging;
        private Vector2 lastPointer;
        private bool follow;
        private string lastFollowPlayer;
        private Button followBtn;

        // Influence overlay (V1 Aurora): animated spatial heat layer + its driving ticker.
        private readonly InfluenceOverlay influence = new InfluenceOverlay();
        private IVisualElementScheduledItem overlayTicker;
        private Toggle masterToggle;
        private Slider opacitySlider;
        private Button paletteButton;
        private readonly Dictionary<InfluenceChannel, Button> channelButtons = new();

        // Tile lookup for adjacency: (x,y) → cleaned terrain name
        private readonly Dictionary<(int, int), string> tileTypes = new();

        // Per-position army stacks: (x,y) → (representative army, count)
        private readonly Dictionary<(int, int), (ArmyDto army, int count)> armyStacks = new();

        // Explicit locations win over terrain labels for overlays.
        private readonly Dictionary<(int, int), LocationDto> locationsByPosition = new();

        public event Action<MapSelection> SelectionChanged;

        public MapView()
        {
            style.flexGrow = 1f;
            style.position = Position.Relative;
            focusable = true;
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
            RegisterCallback<DetachFromPanelEvent>(_ => influence.Dispose());

            // Follow button: overlaid in the bottom-left corner of the map.
            followBtn = new Button(ToggleFollow) { text = "Follow" };
            followBtn.AddToClassList("map-btn");
            followBtn.style.position = Position.Absolute;
            followBtn.style.bottom = 10;
            followBtn.style.left = 10;
            Add(followBtn);

            BuildInfluenceToolbar();
        }

        // ---- Influence overlay toolbar + lifecycle -----------------------------------

        private void BuildInfluenceToolbar()
        {
            var bar = new VisualElement();
            bar.AddToClassList("influence-bar");
            bar.style.position = Position.Absolute;
            bar.style.top = 10;
            bar.style.left = 10;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;

            masterToggle = new Toggle("Influence") { value = false };
            masterToggle.AddToClassList("influence-toggle");
            masterToggle.RegisterValueChangedCallback(e => { influence.Enabled = e.newValue; RefreshOverlayState(); });
            bar.Add(masterToggle);

            foreach (InfluenceChannel ch in Enum.GetValues(typeof(InfluenceChannel)))
            {
                var captured = ch;
                var btn = new Button(() => { influence.Channel = captured; UpdateChannelButtons(); RefreshOverlayState(); }) { text = ch.ToString() };
                btn.AddToClassList("map-btn");
                channelButtons[ch] = btn;
                bar.Add(btn);
            }

            var front = new Toggle("Front") { value = influence.ShowFront };
            front.RegisterValueChangedCallback(e => { influence.ShowFront = e.newValue; RefreshOverlayState(); });
            bar.Add(front);

            var sparkle = new Toggle("Sparkle") { value = influence.ShowSparkle };
            sparkle.RegisterValueChangedCallback(e => { influence.ShowSparkle = e.newValue; RefreshOverlayState(); });
            bar.Add(sparkle);

            var plasma = new Toggle("Plasma") { value = influence.UseGpu };
            plasma.RegisterValueChangedCallback(e => { influence.UseGpu = e.newValue; RefreshOverlayState(); });
            bar.Add(plasma);

            var flow = new Toggle("Flow") { value = influence.ShowGradient };
            flow.RegisterValueChangedCallback(e => { influence.ShowGradient = e.newValue; RefreshOverlayState(); });
            bar.Add(flow);

            var embers = new Toggle("Embers") { value = influence.ShowEmbers };
            embers.RegisterValueChangedCallback(e => { influence.ShowEmbers = e.newValue; RefreshOverlayState(); });
            bar.Add(embers);

            paletteButton = new Button(CyclePalette) { text = influence.Palette.ToString() };
            paletteButton.AddToClassList("map-btn");
            bar.Add(paletteButton);

            opacitySlider = new Slider(0.1f, 1f) { value = influence.Opacity };
            opacitySlider.style.width = 90;
            opacitySlider.RegisterValueChangedCallback(e => { influence.Opacity = e.newValue; MarkDirtyRepaint(); });
            bar.Add(opacitySlider);

            Add(bar);
            UpdateChannelButtons();
        }

        private void UpdateChannelButtons()
        {
            foreach (var kvp in channelButtons)
                kvp.Value.EnableInClassList("map-btn--active", kvp.Key == influence.Channel);
        }

        private void CyclePalette()
        {
            var values = (InfluencePalette[])Enum.GetValues(typeof(InfluencePalette));
            var next = (Array.IndexOf(values, influence.Palette) + 1) % values.Length;
            influence.Palette = values[next];
            if (paletteButton != null) paletteButton.text = influence.Palette.ToString();
            RefreshOverlayState();
        }

        private void RefreshOverlayState()
        {
            if (influence.Animating)
            {
                if (overlayTicker == null) overlayTicker = schedule.Execute(OverlayTick).Every(33);
                else overlayTicker.Resume();
            }
            else
            {
                overlayTicker?.Pause();
            }
            MarkDirtyRepaint();
        }

        private void OverlayTick()
        {
            influence.Tick(Time.realtimeSinceStartup);
            MarkDirtyRepaint();
        }

        public void SetSnapshot(MapSnapshot snapshot)
        {
            map = snapshot;
            tileTypes.Clear();
            armyStacks.Clear();
            locationsByPosition.Clear();

            if (snapshot != null)
            {
                // When the map dimensions change, re-fit the tile size on the next layout pass.
                if (snapshot.Width != lastMapW || snapshot.Height != lastMapH)
                {
                    lastMapW = snapshot.Width;
                    lastMapH = snapshot.Height;
                    tileInitialized = false;
                    cameraInitialized = false;
                }

                if (snapshot.Tiles != null)
                {
                    foreach (var t in snapshot.Tiles)
                    {
                        tileTypes[(t.X, t.Y)] = MapColors.CleanTerrainName(t.TerrainType);
                    }
                }

                if (snapshot.Locations != null)
                {
                    foreach (var location in snapshot.Locations)
                    {
                        if (location?.Position == null) continue;
                        locationsByPosition[(location.Position.X, location.Position.Y)] = location;
                    }
                }

                if (snapshot.Armies != null)
                {
                    foreach (var a in snapshot.Armies)
                    {
                        if (a.Position == null) continue;
                        var key = (a.Position.X, a.Position.Y);
                        if (armyStacks.TryGetValue(key, out var existing))
                        {
                            var rep = ViewingOrderPick(a, existing.army);
                            armyStacks[key] = (rep, existing.count + 1);
                        }
                        else
                        {
                            armyStacks[key] = (a, 1);
                        }
                    }
                }

                var shouldFocusCapital = follow &&
                    snapshot.CurrentCapital?.Position != null &&
                    !string.Equals(snapshot.CurrentPlayer, lastFollowPlayer, StringComparison.OrdinalIgnoreCase);
                if (shouldFocusCapital)
                {
                    cameraCenter = new Vector2(snapshot.CurrentCapital.Position.X, snapshot.CurrentCapital.Position.Y);
                    cameraInitialized = true;
                    ZoomToFollow();
                }
                else if (snapshot.SelectedArmy?.Position != null)
                {
                    cameraCenter = new Vector2(snapshot.SelectedArmy.Position.X, snapshot.SelectedArmy.Position.Y);
                    cameraInitialized = true;
                    if (follow) ZoomToFollow();
                }
                else if (!cameraInitialized)
                {
                    cameraCenter = snapshot.Armies != null && snapshot.Armies.Count > 0 && snapshot.Armies[0].Position != null
                        ? new Vector2(snapshot.Armies[0].Position.X, snapshot.Armies[0].Position.Y)
                        : new Vector2(snapshot.Width / 2f, snapshot.Height / 2f);
                    cameraInitialized = true;
                }

                lastFollowPlayer = snapshot.CurrentPlayer;
            }

            influence.SetField(snapshot?.Influence);
            RefreshOverlayState();

            MarkDirtyRepaint();
        }

        // ---- Rendering ---------------------------------------------------------------

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            mapLayout = ComputeLayout();
            var p = mgc.painter2D;

            FillRect(p, contentRect, new Color(0.18f, 0.18f, 0.19f));
            if (!mapLayout.Valid) return;

            DrawViewport(mgc, p);
            DrawMinimap(p);
        }

        private void DrawViewport(MeshGenerationContext mgc, Painter2D p)
        {
            FillRect(p, mapLayout.Viewport, Color.black);

            if (map.Tiles != null)
            {
                foreach (var tile in map.Tiles)
                {
                    if (!InViewport(tile.X, tile.Y)) continue;

                    var pos = MapToViewport(tile.X, tile.Y);
                    var rect = new Rect(pos.x, pos.y, mapLayout.Tile, mapLayout.Tile);
                    var clean = MapColors.CleanTerrainName(tile.TerrainType);

                    Texture2D terrainTex;
                    if (clean == "Road")
                    {
                        terrainTex = SpriteRegistry.GetRoad(ComputeRoadAdjacency(tile.X, tile.Y));
                    }
                    else if (clean == "Bridge")
                    {
                        var (bm, rm) = ComputeBridgeAdjacency(tile.X, tile.Y);
                        terrainTex = SpriteRegistry.GetBridge(bm, rm);
                    }
                    else if (clean == "Hill")
                    {
                        terrainTex = SpriteRegistry.GetTerrain(clean, ComputeHillSprite(tile.X, tile.Y));
                    }
                    else
                    {
                        terrainTex = SpriteRegistry.GetTerrain(clean, ComputeAdjacency(tile.X, tile.Y, clean));
                    }
                    if (terrainTex != null)
                    {
                        DrawQuad(mgc, terrainTex, rect);
                    }
                    else
                    {
                        FillRect(p, rect, MapColors.ColorForTerrain(tile.TerrainType));
                    }

                    // Location sprite overlay (Ruins, Temple, etc.)
                    var locTex = SpriteRegistry.GetLocation(GetLocationType(tile.X, tile.Y, clean));
                    if (locTex != null)
                    {
                        DrawQuad(mgc, locTex, rect);
                    }

                    // Subtle grid line
                    StrokeRect(p, rect, new Color(0f, 0f, 0f, 0.10f), 1f);
                }
            }

            if (map.Cities != null)
            {
                foreach (var city in map.Cities)
                {
                    DrawCity(mgc, p, city);
                }
            }

            // Influence heat: above terrain and city tiles, below units so stacks stay readable.
            var tileRegion = new Rect(mapLayout.Viewport.x, mapLayout.Viewport.y,
                mapLayout.TilesW * mapLayout.Tile, mapLayout.TilesH * mapLayout.Tile);
            influence.DrawHeat(mgc, p, tileRegion, mapLayout.OriginX, mapLayout.OriginY,
                mapLayout.TilesW, mapLayout.TilesH, map.InvertYAxis, MapToViewport, mapLayout.Tile);

            foreach (var kvp in armyStacks)
            {
                DrawArmy(mgc, p, kvp.Value.army, kvp.Value.count);
            }

            // Flow chevrons, front-line seam, ripples, sparkle, and embers: on top of everything.
            influence.DrawEffects(p, mapLayout.OriginX, mapLayout.OriginY, mapLayout.TilesW, mapLayout.TilesH, map.InvertYAxis, MapToViewport, MapToViewportF, mapLayout.Tile);

            if (map.SelectedArmy?.Position != null)
            {
                var pos = MapToViewport(map.SelectedArmy.Position.X, map.SelectedArmy.Position.Y);
                StrokeRect(p, new Rect(pos.x + 3f, pos.y + 3f, mapLayout.Tile - 6f, mapLayout.Tile - 6f), Color.yellow, 2f);
            }

            StrokeRect(p, mapLayout.Viewport, new Color(0f, 0f, 0f, 0.8f), 2f);
        }

        private void DrawCity(MeshGenerationContext mgc, Painter2D p, CityDto city)
        {
            if (city.Position == null) return;
            var invertY = map.InvertYAxis;

            for (var dx = 0; dx < 2; dx++)
            {
                for (var dy = 0; dy < 2; dy++)
                {
                    var gx = city.Position.X + dx;
                    var gy = city.Position.Y - dy;
                    if (!InViewport(gx, gy)) continue;

                    var pos = MapToViewport(gx, gy);
                    var t = mapLayout.Tile;
                    var rect = new Rect(pos.x, pos.y, t, t);

                    // When invertY=true: dy=0→top on screen; when false: dy=0→bottom on screen
                    var screenTop = invertY ? dy == 0 : dy == 1;
                    int quadrant = (screenTop ? 0 : 2) + dx; // 0=TL,1=TR,2=BL,3=BR

                    var tex = SpriteRegistry.GetCity(city.Owner, quadrant);
                    if (tex != null)
                    {
                        DrawQuad(mgc, tex, rect);
                    }
                    else
                    {
                        var inner = new Rect(pos.x + t * 0.12f, pos.y + t * 0.12f, t * 0.76f, t * 0.76f);
                        FillRect(p, inner, new Color(0.82f, 0.82f, 0.82f));
                        StrokeRect(p, inner, MapColors.ClanColor(city.Owner), Mathf.Max(2f, t * 0.08f));
                        FillRect(p, new Rect(pos.x + t * 0.30f, pos.y + t * 0.18f, t * 0.12f, t * 0.30f), new Color(0.55f, 0.1f, 0.1f));
                        FillRect(p, new Rect(pos.x + t * 0.58f, pos.y + t * 0.18f, t * 0.12f, t * 0.30f), new Color(0.55f, 0.1f, 0.1f));
                    }
                }
            }
        }

        private void DrawArmy(MeshGenerationContext mgc, Painter2D p, ArmyDto army, int stackCount)
        {
            if (army.Position == null) return;
            if (!InViewport(army.Position.X, army.Position.Y)) return;

            var pos = MapToViewport(army.Position.X, army.Position.Y);
            var t = mapLayout.Tile;
            var rect = new Rect(pos.x, pos.y, t, t);

            var unitTex = SpriteRegistry.GetArmy(army.Owner, army.UnitType ?? army.Name, army.IsHero);
            if (unitTex != null)
            {
                // Flag sprite and unit sprite are 40×40 companions: flag content sits in the left
                // portion of the canvas, unit content in the right portion. Draw flag first so the
                // unit renders on top where they overlap.
                var flagTex = SpriteRegistry.GetFlag(army.Owner, stackCount);
                if (flagTex != null) DrawQuad(mgc, flagTex, rect);
                DrawQuad(mgc, unitTex, rect);
            }
            else
            {
                var flagTex = SpriteRegistry.GetFlag(army.Owner, stackCount);
                if (flagTex != null) DrawQuad(mgc, flagTex, rect);

                var cx = pos.x + t / 2f;
                var cy = pos.y + t / 2f;
                var r = t * 0.28f;
                p.fillColor = MapColors.ClanColor(army.Owner);
                p.BeginPath();
                p.MoveTo(new Vector2(cx, cy - r));
                p.LineTo(new Vector2(cx + r, cy));
                p.LineTo(new Vector2(cx + r * 0.35f, cy + r));
                p.LineTo(new Vector2(cx - r * 0.35f, cy + r));
                p.LineTo(new Vector2(cx - r, cy));
                p.ClosePath();
                p.Fill();
                p.strokeColor = army.CanFly ? new Color(0.45f, 0.85f, 1f) : army.IsSpecial ? new Color(1f, 0.75f, 0.2f) : Color.black;
                p.lineWidth = army.CanFly || army.IsSpecial ? 2f : 1f;
                p.Stroke();
            }
        }

        private string GetLocationType(int x, int y, string terrainFallback)
        {
            return locationsByPosition.TryGetValue((x, y), out var location) && !string.IsNullOrWhiteSpace(location.Type)
                ? location.Type
                : terrainFallback;
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
                    if (city.Position == null) continue;
                    var x = mm.x + city.Position.X * tw;
                    var y = mm.y + (map.Height - 1 - city.Position.Y) * th;
                    FillRect(p, new Rect(x - 2f, y - 2f, 5f, 5f), MapColors.ClanColor(city.Owner));
                }
            }

            if (map.Armies != null)
            {
                foreach (var army in map.Armies)
                {
                    if (army.Position == null) continue;
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

        // ---- Adjacency (TileUtility 14-way algorithm, ported from WismUnity) ----------

        /// <summary>
        /// Computes the 0-13 sprite index using the same 4-corner adjacency map as TileUtility.
        /// TopLeft/Right/BottomLeft/Right are each true when the three tiles forming that quadrant
        /// corner are all the same terrain type as the current tile.
        /// </summary>
        private int ComputeAdjacency(int x, int y, string cleanType)
        {
            // Respect the map's Y convention so sprites orient correctly on screen.
            var up   = map.InvertYAxis ? 1 : -1;
            var down = map.InvertYAxis ? -1 : 1;

            bool Has(int nx, int ny)
            {
                if (!tileTypes.TryGetValue((nx, ny), out var t)) return false;
                if (string.Equals(t, cleanType, StringComparison.OrdinalIgnoreCase)) return true;
                // Cross-type adjacency (mirrors WismUnity tile rules)
                if (cleanType == "Water"    && t == "Bridge")   return true; // WaterTile.HasTile includes Bridge
                if (cleanType == "Hill"     && t == "Mountain") return true; // HillTile.HasHill includes Mountain
                if (cleanType == "Mountain" && (t == "SnowPeak" || t == "Volcano")) return true;
                return false;
            }

            bool left = Has(x - 1, y);
            bool right = Has(x + 1, y);
            bool top   = Has(x, y + up);
            bool bot   = Has(x, y + down);
            bool tl    = Has(x - 1, y + up);
            bool tr    = Has(x + 1, y + up);
            bool bl    = Has(x - 1, y + down);
            bool br    = Has(x + 1, y + down);

            // Corner quadrants: true when the full 3-tile corner cluster is the same type
            bool adjTL = left && tl && top;
            bool adjTR = top  && tr && right;
            bool adjBL = bl   && left && bot;
            bool adjBR = bot  && br   && right;

            return (adjTL, adjTR, adjBL, adjBR) switch
            {
                (false, true,  false, false) => 0,   // bottom-left
                (true,  true,  false, true)  => 1,   // bottom-left inside corner
                (true,  true,  false, false) => 2,   // bottom-middle
                (true,  false, false, false) => 3,   // bottom-right
                (true,  true,  true,  false) => 4,   // bottom-right inside corner
                (true,  true,  true,  true)  => 5,   // middle (fully surrounded)
                (true,  false, true,  false) => 6,   // right-middle
                (false, false, false, true)  => 7,   // top-left
                (false, true,  true,  true)  => 8,   // top-left inside corner
                (false, false, true,  true)  => 9,   // top-middle
                (false, false, true,  false) => 10,  // top-right
                (true,  false, true,  true)  => 11,  // top-right inside corner
                (false, true,  false, true)  => 12,  // left-middle
                _                            => 13,  // single / isolated
            };
        }

        // ---- Sprite quad drawing via MeshGenerationContext ---------------------------

        /// <summary>
        /// Draws a textured quad. UV maps screen top→texture top (Y flipped to match Unity
        /// texture convention where UV (0,0) is bottom-left of the texture).
        /// </summary>
        private static void DrawQuad(MeshGenerationContext mgc, Texture2D tex, Rect rect)
        {
            if (tex == null) return;
            var mesh = mgc.Allocate(4, 6, tex);
            var w = (Color32)Color.white;
            var z = Vertex.nearZ;
            mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMin, rect.yMin, z), tint = w, uv = new Vector2(0f, 1f) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMax, rect.yMin, z), tint = w, uv = new Vector2(1f, 1f) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMax, rect.yMax, z), tint = w, uv = new Vector2(1f, 0f) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMin, rect.yMax, z), tint = w, uv = new Vector2(0f, 0f) });
            mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
            mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
        }

        // ---- Layout and coordinate math -----------------------------------------------

        private Layout ComputeLayout()
        {
            var content = contentRect;
            var result = new Layout();
            if (map == null || map.Width <= 0 || map.Height <= 0 || content.width < 4f || content.height < 4f)
                return result;

            var vw = Mathf.Max(MinTile, content.width  - Padding * 2f);
            var vh = Mathf.Max(MinTile, content.height - Padding * 2f);

            // Auto-fit: make all tiles visible on first render for this map size.
            if (!tileInitialized)
            {
                tileSize = Mathf.Clamp(Mathf.Min(vw / map.Width, vh / map.Height), MinTile, MaxTile);
                tileInitialized = true;
            }
            var tile = Mathf.Clamp(tileSize, MinTile, MaxTile);

            // Number of tiles visible at the current zoom level (may be < map size when zoomed in).
            var tilesW = Mathf.Clamp(Mathf.FloorToInt(vw / tile), 1, map.Width);
            var tilesH = Mathf.Clamp(Mathf.FloorToInt(vh / tile), 1, map.Height);
            var maxOriginX = Mathf.Max(0, map.Width  - tilesW);
            var maxOriginY = Mathf.Max(0, map.Height - tilesH);

            result.Tile   = tile;
            result.TilesW = tilesW;
            result.TilesH = tilesH;
            result.OriginX = Mathf.Clamp(Mathf.RoundToInt(cameraCenter.x - tilesW / 2f), 0, maxOriginX);
            result.OriginY = Mathf.Clamp(Mathf.RoundToInt(cameraCenter.y - tilesH / 2f), 0, maxOriginY);

            // Viewport fills the full content area; tiles render at top-left, unused space is background.
            result.Viewport = new Rect(content.x + Padding, content.y + Padding, vw, vh);

            // Minimap: small corner overlay (top-right).
            var mmBase = Mathf.Min(180f, vw * 0.22f, vh * 0.22f);
            var mapAspect = (float)map.Width / map.Height;
            float mmW, mmH;
            if (mapAspect >= 1f) { mmW = mmBase; mmH = mmBase / mapAspect; }
            else                 { mmH = mmBase; mmW = mmBase * mapAspect; }
            const float mmPad = 8f;
            result.Minimap = new Rect(result.Viewport.xMax - mmW - mmPad, result.Viewport.yMin + mmPad, mmW, mmH);

            result.Valid = true;
            return result;
        }

        private bool InViewport(int mapX, int mapY) =>
            mapX >= mapLayout.OriginX && mapY >= mapLayout.OriginY &&
            mapX < mapLayout.OriginX + mapLayout.TilesW &&
            mapY < mapLayout.OriginY + mapLayout.TilesH;

        private Vector2 MapToViewport(int mapX, int mapY)
        {
            var relX = mapX - mapLayout.OriginX;
            var relY = mapY - mapLayout.OriginY;
            var screenRow = map.InvertYAxis ? mapLayout.TilesH - 1 - relY : relY;
            return new Vector2(mapLayout.Viewport.x + relX * mapLayout.Tile, mapLayout.Viewport.y + screenRow * mapLayout.Tile);
        }

        // Continuous map→screen for sub-tile positions (ember particles). The within-cell Y offset
        // flips under InvertYAxis, so this can't reuse the integer tile mapper.
        private Vector2 MapToViewportF(float mapX, float mapY)
        {
            var x = mapLayout.Viewport.x + (mapX - mapLayout.OriginX) * mapLayout.Tile;
            var y = map.InvertYAxis
                ? mapLayout.Viewport.y + (mapLayout.OriginY + mapLayout.TilesH - mapY) * mapLayout.Tile
                : mapLayout.Viewport.y + (mapY - mapLayout.OriginY) * mapLayout.Tile;
            return new Vector2(x, y);
        }

        // ---- Input -------------------------------------------------------------------

        private void OnWheel(WheelEvent e)
        {
            tileSize = Mathf.Clamp(tileSize - e.delta.y * 2f, MinTile, MaxTile);
            MarkDirtyRepaint();
            e.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent e)
        {
            if (map == null) return;
            var local = new Vector2(e.localPosition.x, e.localPosition.y);

            if (mapLayout.Valid && mapLayout.Minimap.Contains(local))
            {
                RecenterFromMinimap(local);
                e.StopPropagation();
                return;
            }

            if (e.button == 0 && mapLayout.Valid && mapLayout.Viewport.Contains(local))
                RaiseSelection(ViewportToMap(local));

            dragging = true;
            lastPointer = local;
            this.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent e)
        {
            if (!dragging || map == null || tileSize <= 0f) return;
            var local = new Vector2(e.localPosition.x, e.localPosition.y);
            var delta = local - lastPointer;
            lastPointer = local;
            // User is panning manually — cancel follow mode.
            if (follow && (Mathf.Abs(delta.x) > 2f || Mathf.Abs(delta.y) > 2f))
                SetFollow(false);
            cameraCenter.x -= delta.x / tileSize;
            cameraCenter.y += (map.InvertYAxis ? delta.y : -delta.y) / tileSize;
            cameraCenter.x = Mathf.Clamp(cameraCenter.x, 0f, Mathf.Max(0f, map.Width  - 1f));
            cameraCenter.y = Mathf.Clamp(cameraCenter.y, 0f, Mathf.Max(0f, map.Height - 1f));
            cameraInitialized = true;
            MarkDirtyRepaint();
        }

        private void OnPointerUp(PointerUpEvent e)
        {
            dragging = false;
            if (this.HasPointerCapture(e.pointerId))
                this.ReleasePointer(e.pointerId);
        }

        private Vector2Int ViewportToMap(Vector2 local)
        {
            var relX = Mathf.FloorToInt((local.x - mapLayout.Viewport.x) / mapLayout.Tile);
            var relY = Mathf.FloorToInt((local.y - mapLayout.Viewport.y) / mapLayout.Tile);
            var mapX = mapLayout.OriginX + relX;
            var mapY = map.InvertYAxis
                ? mapLayout.OriginY + mapLayout.TilesH - 1 - relY
                : mapLayout.OriginY + relY;
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
            if (SelectionChanged == null || map == null) return;

            TileDto tile = null;
            ArmyDto army = null;
            CityDto city = null;
            LocationDto location = null;

            if (map.Tiles != null)
                foreach (var t in map.Tiles)
                    if (t.X == coord.x && t.Y == coord.y) { tile = t; break; }

            if (map.Armies != null)
                foreach (var a in map.Armies)
                    if (a.Position != null && a.Position.X == coord.x && a.Position.Y == coord.y) { army = a; break; }

            if (map.Cities != null)
                foreach (var c in map.Cities)
                    if (c.Position != null && c.Position.X == coord.x && c.Position.Y == coord.y) { city = c; break; }

            if (map.Locations != null)
                foreach (var l in map.Locations)
                    if (l.Position != null && l.Position.X == coord.x && l.Position.Y == coord.y) { location = l; break; }

            SelectionChanged.Invoke(new MapSelection(coord.x, coord.y, tile, army, city, location));
        }

        // ---- Follow mode ------------------------------------------------------------

        private void ToggleFollow() => SetFollow(!follow);

        private void SetFollow(bool value)
        {
            follow = value;
            followBtn?.EnableInClassList("map-btn--active", follow);
            if (follow && map?.SelectedArmy?.Position != null)
            {
                cameraCenter = new Vector2(map.SelectedArmy.Position.X, map.SelectedArmy.Position.Y);
                cameraInitialized = true;
                ZoomToFollow();
            }
        }

        private void ZoomToFollow()
        {
            tileSize = Mathf.Clamp(48f, MinTile, MaxTile);
            tileInitialized = true;
            MarkDirtyRepaint();
        }

        // ---- Road adjacency (4-bit NESW mask) ---------------------------------------

        private bool IsRoadOrBridge(int x, int y) =>
            tileTypes.TryGetValue((x, y), out var t) &&
            (t.Equals("Road", StringComparison.OrdinalIgnoreCase) ||
             t.Equals("Bridge", StringComparison.OrdinalIgnoreCase));

        private int ComputeRoadAdjacency(int x, int y)
        {
            var up   = map.InvertYAxis ?  1 : -1;
            var down = map.InvertYAxis ? -1 :  1;
            int mask = 0;
            if (IsRoadOrBridge(x, y + up))   mask |= 8; // N
            if (IsRoadOrBridge(x + 1, y))    mask |= 4; // E
            if (IsRoadOrBridge(x, y + down)) mask |= 2; // S
            if (IsRoadOrBridge(x - 1, y))    mask |= 1; // W
            return mask;
        }

        // Returns separate bridge-neighbor and road-neighbor NESW masks for sprite selection.
        private (int bridgeMask, int roadMask) ComputeBridgeAdjacency(int x, int y)
        {
            var up   = map.InvertYAxis ?  1 : -1;
            var down = map.InvertYAxis ? -1 :  1;
            int bm = 0, rm = 0;
            void Check(int nx, int ny, int bit)
            {
                if (!tileTypes.TryGetValue((nx, ny), out var t)) return;
                if (t == "Bridge") bm |= bit;
                else if (t == "Road") rm |= bit;
            }
            Check(x,     y + up,   8); // N
            Check(x + 1, y,        4); // E
            Check(x,     y + down, 2); // S
            Check(x - 1, y,        1); // W
            return (bm, rm);
        }

        // Returns 0-4 hill sprite index matching WismUnity HillTile.GetTileData logic.
        private int ComputeHillSprite(int x, int y)
        {
            var up   = map.InvertYAxis ?  1 : -1;
            var down = map.InvertYAxis ? -1 :  1;
            bool HasHill(int nx, int ny) =>
                tileTypes.TryGetValue((nx, ny), out var t) && (t == "Hill" || t == "Mountain");
            bool n = HasHill(x, y + up), s = HasHill(x, y + down), w = HasHill(x - 1, y);
            if (!s &&  n) return 0;
            if ( s && !n) return 1;
            if ( s &&  n) return 2;
            if ( w &&  n) return 3;
            if ( s &&  w) return 4;
            return 4; // isolated or east-only neighbor
        }

        // ---- Army viewing order (mirrors ByArmyViewingOrder comparer) ---------------

        private static ArmyDto ViewingOrderPick(ArmyDto incoming, ArmyDto existing)
        {
            if (incoming.IsHero   != existing.IsHero)   return incoming.IsHero   ? incoming : existing;
            if (incoming.IsSpecial != existing.IsSpecial) return incoming.IsSpecial ? incoming : existing;
            if (incoming.CanFly   != existing.CanFly)   return incoming.CanFly   ? incoming : existing;
            if (incoming.Strength != existing.Strength) return incoming.Strength > existing.Strength ? incoming : existing;
            if (incoming.Moves    != existing.Moves)    return incoming.Moves    > existing.Moves    ? incoming : existing;
            return existing;
        }

        // ---- Painter2D helpers -------------------------------------------------------

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
            public int OriginX, OriginY, TilesW, TilesH;
            public float Tile;
        }
    }
}
