using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.CompanionApp.WinForms
{
    public class MapRenderer : Control
    {
        private const int TileSize = 32;
        private const int MaxViewportTilesWide = 25;
        private const int MaxViewportTilesHigh = 25;

        /// <summary>
        /// When true, inverts Y-axis (for ASCII origin). Set to false for Unity origin.
        /// </summary>
        public bool InvertYAxis { get; set; } = true;

        private Point cameraCenter = new Point(0, 0);
        private MapSnapshot _currentMap;
        private readonly ToolTip _toolTip = new ToolTip();
        private Point _lastHoverTile = Point.Empty;
        private Bitmap offscreenBuffer;
        private readonly Dictionary<(int x, int y), TileRenderCacheEntry> tileCache = new();

        private static readonly Dictionary<string, Brush> ClanBrushes = new()
        {
            ["Sirians"] = Brushes.White,
            ["StormGiants"] = Brushes.Gold,
            ["GreyDwarves"] = Brushes.SaddleBrown,
            ["OrcsOfKor"] = Brushes.Red,
            ["Elvallie"] = Brushes.Green,
            ["Selentines"] = Brushes.DarkBlue,
            ["HorseLords"] = Brushes.LightSkyBlue,
            ["LordBane"] = Brushes.Black
        };

        public MapRenderer()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
            MouseMove += MapRenderer_MouseMove;
        }

        //BUGBUG: This is not rendinering the correct terrain--need to investigate.
        private void MapRenderer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentMap?.Tiles == null || !_currentMap.Tiles.Any())
                return;

            var hovered = new Point(e.X / TileSize, e.Y / TileSize);
            if (_lastHoverTile == hovered) return;
            _lastHoverTile = hovered;

            var tile = _currentMap.Tiles.FirstOrDefault(t => t.X == hovered.X && t.Y == hovered.Y);
            if (tile != null)
            {
                // Strip any numeric suffix like "(0)" so we get just the ShortName
                var key = tile.TerrainType;
                var m = System.Text.RegularExpressions.Regex.Match(key, @"^(.*?)\(\d+\)$");
                if (m.Success) key = m.Groups[1].Value;

                string tip = $"({tile.X},{tile.Y}) – {key}" + (tile.HasCity ? " (City)" : "");
                _toolTip.SetToolTip(this, tip);
            }
            else
            {
                _toolTip.SetToolTip(this, null);
            }
        }


        public void UpdateMap(MapSnapshot snapshot)
        {
            _currentMap = snapshot;
            if (snapshot.SelectedArmy != null)
            {
                cameraCenter = new Point(snapshot.SelectedArmy.Position.X, snapshot.SelectedArmy.Position.Y);
            }
            else if (cameraCenter == Point.Empty && snapshot.Armies?.Any() == true)
            {
                var firstPos = snapshot.Armies.First().Position;
                cameraCenter = new Point(firstPos.X, firstPos.Y);
            }

            tileCache.Clear();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_currentMap?.Tiles == null) return;

            int rawW = Width / TileSize;
            int rawH = Height / TileSize;
            int viewW = Math.Min(Math.Min(rawW, MaxViewportTilesWide), _currentMap.Width);
            int viewH = Math.Min(Math.Min(rawH, MaxViewportTilesHigh), _currentMap.Height);

            int maxOriginX = Math.Max(0, _currentMap.Width - viewW);
            int maxOriginY = Math.Max(0, _currentMap.Height - viewH);
            int originX = Math.Clamp(cameraCenter.X - viewW / 2, 0, maxOriginX);
            int originY = Math.Clamp(cameraCenter.Y - viewH / 2, 0, maxOriginY);

            int bufW = viewW * TileSize;
            int bufH = viewH * TileSize;
            if (offscreenBuffer == null || offscreenBuffer.Width != bufW || offscreenBuffer.Height != bufH)
            {
                offscreenBuffer?.Dispose();
                offscreenBuffer = new Bitmap(bufW, bufH);
            }

            // helper to convert map coords to screen within buffer, with optional invert
            Point ToScreen(int mx, int my)
            {
                int relX = mx - originX;
                int relY = my - originY;
                int screenRow = InvertYAxis
                    ? (viewH - 1 - relY)
                    : relY;
                return new Point(relX * TileSize, screenRow * TileSize);
            }

            using var g = Graphics.FromImage(offscreenBuffer);
            g.Clear(Color.Black);

            // Draw terrain
            foreach (var tile in _currentMap.Tiles)
            {
                var pt = ToScreen(tile.X, tile.Y);
                if (pt.X < 0 || pt.X >= bufW || pt.Y < 0 || pt.Y >= bufH)
                    continue;
                DrawTile(g, tile, pt.X, pt.Y);
            }

            // Draw each city as a single castle spanning its 2×2 footprint
            foreach (var city in _currentMap.Cities)
            {
                var clanName = string.IsNullOrWhiteSpace(city.Owner) ? "Neutral" : city.Owner.Trim();
                Brush? borderBrush = ClanBrushes.TryGetValue(clanName, out var b) ? b : null;

                // Anchor from correct top-left city tile (draw down and right)
                for (int dx = 0; dx < 2; dx++)
                {
                    for (int dy = 0; dy < 2; dy++)
                    {
                        var pt = ToScreen(city.Position.X + dx, city.Position.Y - dy);
                        var ellipseRect = new Rectangle(pt.X + 4, pt.Y + 4, TileSize - 8, TileSize - 8);
                        g.FillEllipse(Brushes.SlateGray, ellipseRect);
                        if (borderBrush != null)
                            using (var borderPen = new Pen(borderBrush, 2))
                                g.DrawEllipse(borderPen, ellipseRect);
                    }
                }
            }

            // Draw armies
            foreach (var army in _currentMap.Armies)
            {
                var pt = ToScreen(army.Position.X, army.Position.Y);
                int cx = pt.X + TileSize / 2;
                int cy = pt.Y + TileSize / 2;
                var color = ClanBrushes.TryGetValue(army.Owner ?? string.Empty, out var cb2) ? cb2 : Brushes.DarkRed;
                var diamond = new[]
                {
                    new Point(cx, cy - 10),
                    new Point(cx + 10, cy),
                    new Point(cx, cy + 10),
                    new Point(cx - 10, cy)
                };
                g.FillPolygon(color, diamond);
            }

            // Highlight selected army
            if (_currentMap.SelectedArmy != null)
            {
                var sel = _currentMap.SelectedArmy;
                var pt = ToScreen(sel.Position.X, sel.Position.Y);
                int cx = pt.X + TileSize / 2;
                int cy = pt.Y + TileSize / 2;
                var diamond = new[]
                {
                    new Point(cx, cy - 10),
                    new Point(cx + 10, cy),
                    new Point(cx, cy + 10),
                    new Point(cx - 10, cy)
                };
                g.DrawPolygon(Pens.Yellow, diamond);
            }

            // Center buffer in control
            int offsetX = (Width - offscreenBuffer.Width) / 2;
            int offsetY = (Height - offscreenBuffer.Height) / 2;
            e.Graphics.DrawImageUnscaled(offscreenBuffer, new Point(offsetX, offsetY));
        }

        private void DrawTile(Graphics g, TileDto tile, int x, int y)
        {
            // Strip any numeric suffix in parentheses (e.g. "Grass(0)")
            var key = tile.TerrainType;
            var match = System.Text.RegularExpressions.Regex.Match(key, @"^(.*?)\(\d+\)$");
            if (match.Success)
                key = match.Groups[1].Value;

            Brush brush = key switch
            {
                "Forest" => Brushes.DarkGreen,
                "Mountain" => Brushes.Gray,
                "Grass" => Brushes.LightGreen,
                "Water" => Brushes.LightBlue,
                "Hill" => Brushes.Olive,
                "Marsh" => Brushes.DarkOliveGreen,
                "Road" => Brushes.SandyBrown,
                "Bridge" => Brushes.SaddleBrown,
                "Castle" => Brushes.LightGreen,
                "Library" => Brushes.LightSteelBlue,
                "Ruins" => Brushes.DimGray,
                "Sage" => Brushes.MediumPurple,
                "Temple" => Brushes.LightYellow,
                "Tomb" => Brushes.SaddleBrown,
                "Tower" => Brushes.LightSlateGray,
                "Void" => Brushes.Black,
                _ => Brushes.LightGreen
            };
            g.FillRectangle(brush, x, y, TileSize, TileSize);
            g.DrawRectangle(Pens.Black, x, y, TileSize, TileSize);
        }

        public void TrackArmyAt(PositionDto position)
        {
            if (position is not null)
            {
                cameraCenter = new Point(position.X, position.Y);
                Invalidate();
            }
        }
    }
}
