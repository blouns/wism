using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.CompanionApp.WinForms
{
    public class MapRenderer : Control
    {
        private const int TileSize = 40;
        private const int MinimapPanelWidth = 220;
        private const int LayoutPadding = 8;
        private const int MaxViewportTilesWide = 14;
        private const int MaxViewportTilesHigh = 10;

        public bool InvertYAxis { get; set; } = true;

        private Point cameraCenter = new Point(0, 0);
        private Point viewportOrigin = new Point(0, 0);
        private Size viewportTiles = new Size(1, 1);
        private Rectangle viewportBounds = Rectangle.Empty;
        private Rectangle minimapBounds = Rectangle.Empty;
        private MapSnapshot _currentMap = new MapSnapshot();
        private readonly ToolTip _toolTip = new ToolTip();
        private Point _lastHoverTile = Point.Empty;
        private Bitmap offscreenBuffer = new Bitmap(1, 1);

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
            MouseClick += MapRenderer_MouseClick;
        }

        private void MapRenderer_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_currentMap.Tiles == null || !_currentMap.Tiles.Any())
            {
                return;
            }

            var hovered = ScreenToMap(e.Location);
            if (hovered == Point.Empty || _lastHoverTile == hovered)
            {
                return;
            }

            _lastHoverTile = hovered;
            var tile = _currentMap.Tiles.FirstOrDefault(t => t.X == hovered.X && t.Y == hovered.Y);
            if (tile == null)
            {
                _toolTip.SetToolTip(this, null);
                return;
            }

            var key = CleanTerrainName(tile.TerrainType);
            var tip = $"({tile.X},{tile.Y}) - {key}" + (tile.HasCity ? " (City)" : "");
            _toolTip.SetToolTip(this, tip);
        }

        private void MapRenderer_MouseClick(object? sender, MouseEventArgs e)
        {
            if (!minimapBounds.Contains(e.Location))
            {
                return;
            }

            cameraCenter = ScreenToMap(e.Location);
            Invalidate();
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

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_currentMap.Tiles == null || !_currentMap.Tiles.Any())
            {
                return;
            }

            CalculateLayout();
            EnsureBuffer();

            using var g = Graphics.FromImage(offscreenBuffer);
            g.Clear(Color.FromArgb(96, 96, 96));
            DrawZoomedViewport(g);
            DrawMinimap(g);

            e.Graphics.DrawImageUnscaled(offscreenBuffer, Point.Empty);
        }

        private void CalculateLayout()
        {
            var rightPanelWidth = Width >= 520 ? MinimapPanelWidth : Math.Max(120, Width / 3);
            var leftWidth = Math.Max(TileSize, Width - rightPanelWidth - LayoutPadding * 3);
            var leftHeight = Math.Max(TileSize, Height - LayoutPadding * 2);

            var viewW = Math.Min(Math.Min(leftWidth / TileSize, MaxViewportTilesWide), _currentMap.Width);
            var viewH = Math.Min(Math.Min(leftHeight / TileSize, MaxViewportTilesHigh), _currentMap.Height);
            viewportTiles = new Size(Math.Max(1, viewW), Math.Max(1, viewH));

            var maxOriginX = Math.Max(0, _currentMap.Width - viewportTiles.Width);
            var maxOriginY = Math.Max(0, _currentMap.Height - viewportTiles.Height);
            viewportOrigin = new Point(
                Math.Clamp(cameraCenter.X - viewportTiles.Width / 2, 0, maxOriginX),
                Math.Clamp(cameraCenter.Y - viewportTiles.Height / 2, 0, maxOriginY));

            viewportBounds = new Rectangle(
                LayoutPadding,
                LayoutPadding,
                viewportTiles.Width * TileSize,
                viewportTiles.Height * TileSize);

            var minimapX = viewportBounds.Right + LayoutPadding;
            var minimapW = Math.Max(1, Width - minimapX - LayoutPadding);
            var minimapH = Math.Max(1, Height - LayoutPadding * 2);
            var mapAspect = _currentMap.Width / (double)_currentMap.Height;
            var panelAspect = minimapW / (double)minimapH;
            if (panelAspect > mapAspect)
            {
                minimapW = (int)Math.Round(minimapH * mapAspect);
            }
            else
            {
                minimapH = (int)Math.Round(minimapW / mapAspect);
            }

            minimapBounds = new Rectangle(minimapX, LayoutPadding, minimapW, minimapH);
        }

        private void EnsureBuffer()
        {
            if (offscreenBuffer == null || offscreenBuffer.Width != Width || offscreenBuffer.Height != Height)
            {
                offscreenBuffer?.Dispose();
                offscreenBuffer = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
            }
        }

        private void DrawZoomedViewport(Graphics g)
        {
            using var borderPen = new Pen(Color.Black, 2);
            g.FillRectangle(Brushes.Black, viewportBounds);
            g.DrawRectangle(borderPen, viewportBounds);

            foreach (var tile in _currentMap.Tiles)
            {
                if (tile.X < viewportOrigin.X || tile.Y < viewportOrigin.Y ||
                    tile.X >= viewportOrigin.X + viewportTiles.Width ||
                    tile.Y >= viewportOrigin.Y + viewportTiles.Height)
                {
                    continue;
                }

                var pt = MapToViewport(tile.X, tile.Y);
                DrawTile(g, tile, pt.X, pt.Y, TileSize, drawGrid: true);
            }

            foreach (var city in _currentMap.Cities)
            {
                DrawCity(g, city);
            }

            foreach (var army in _currentMap.Armies)
            {
                DrawArmy(g, army);
            }

            if (_currentMap.SelectedArmy != null)
            {
                var pt = MapToViewport(_currentMap.SelectedArmy.Position.X, _currentMap.SelectedArmy.Position.Y);
                g.DrawRectangle(Pens.Yellow, pt.X + 3, pt.Y + 3, TileSize - 6, TileSize - 6);
            }
        }

        private void DrawMinimap(Graphics g)
        {
            using var panelBrush = new SolidBrush(Color.FromArgb(56, 56, 56));
            using var framePen = new Pen(Color.Black, 2);
            g.FillRectangle(panelBrush, minimapBounds);
            g.DrawRectangle(framePen, minimapBounds);

            var tileW = minimapBounds.Width / (float)_currentMap.Width;
            var tileH = minimapBounds.Height / (float)_currentMap.Height;
            foreach (var tile in _currentMap.Tiles)
            {
                using var brush = new SolidBrush(ColorForTerrain(tile.TerrainType));
                var x = minimapBounds.Left + tile.X * tileW;
                var y = minimapBounds.Top + (_currentMap.Height - 1 - tile.Y) * tileH;
                g.FillRectangle(brush, x, y, Math.Max(1, tileW + 0.5f), Math.Max(1, tileH + 0.5f));
            }

            foreach (var city in _currentMap.Cities)
            {
                var brush = ClanBrushes.TryGetValue(city.Owner ?? string.Empty, out var clanBrush)
                    ? clanBrush
                    : Brushes.White;
                var x = minimapBounds.Left + city.Position.X * tileW;
                var y = minimapBounds.Top + (_currentMap.Height - 1 - city.Position.Y) * tileH;
                g.FillRectangle(brush, x - 2, y - 2, 5, 5);
            }

            foreach (var army in _currentMap.Armies)
            {
                var brush = ClanBrushes.TryGetValue(army.Owner ?? string.Empty, out var clanBrush)
                    ? clanBrush
                    : Brushes.DarkRed;
                var x = minimapBounds.Left + army.Position.X * tileW;
                var y = minimapBounds.Top + (_currentMap.Height - 1 - army.Position.Y) * tileH;
                g.FillRectangle(brush, x - 1, y - 1, 3, 3);
            }

            var viewX = minimapBounds.Left + viewportOrigin.X * tileW;
            var viewY = minimapBounds.Top + (_currentMap.Height - viewportOrigin.Y - viewportTiles.Height) * tileH;
            var viewRect = new RectangleF(viewX, viewY, viewportTiles.Width * tileW, viewportTiles.Height * tileH);
            using var viewPen = new Pen(Color.White, 2);
            g.DrawRectangle(viewPen, viewRect.X, viewRect.Y, viewRect.Width, viewRect.Height);
        }

        private Point MapToViewport(int mapX, int mapY)
        {
            var relX = mapX - viewportOrigin.X;
            var relY = mapY - viewportOrigin.Y;
            var screenRow = InvertYAxis ? viewportTiles.Height - 1 - relY : relY;
            return new Point(viewportBounds.Left + relX * TileSize, viewportBounds.Top + screenRow * TileSize);
        }

        private Point ScreenToMap(Point point)
        {
            if (viewportBounds.Contains(point))
            {
                var relX = (point.X - viewportBounds.Left) / TileSize;
                var relY = (point.Y - viewportBounds.Top) / TileSize;
                var mapY = InvertYAxis
                    ? viewportOrigin.Y + viewportTiles.Height - 1 - relY
                    : viewportOrigin.Y + relY;
                return new Point(viewportOrigin.X + relX, mapY);
            }

            if (minimapBounds.Contains(point))
            {
                var x = (int)((point.X - minimapBounds.Left) / (float)minimapBounds.Width * _currentMap.Width);
                var yFromTop = (int)((point.Y - minimapBounds.Top) / (float)minimapBounds.Height * _currentMap.Height);
                return new Point(
                    Math.Clamp(x, 0, _currentMap.Width - 1),
                    Math.Clamp(_currentMap.Height - 1 - yFromTop, 0, _currentMap.Height - 1));
            }

            return Point.Empty;
        }

        private void DrawTile(Graphics g, TileDto tile, int x, int y, int size, bool drawGrid)
        {
            using var brush = new SolidBrush(ColorForTerrain(tile.TerrainType));
            g.FillRectangle(brush, x, y, size, size);
            if (drawGrid)
            {
                using var gridPen = new Pen(Color.FromArgb(45, 0, 0, 0));
                g.DrawRectangle(gridPen, x, y, size, size);
            }
        }

        private void DrawCity(Graphics g, CityDto city)
        {
            for (var dx = 0; dx < 2; dx++)
            {
                for (var dy = 0; dy < 2; dy++)
                {
                    var pt = MapToViewport(city.Position.X + dx, city.Position.Y - dy);
                    if (!viewportBounds.IntersectsWith(new Rectangle(pt, new Size(TileSize, TileSize))))
                    {
                        continue;
                    }

                    var clanName = string.IsNullOrWhiteSpace(city.Owner) ? "Neutral" : city.Owner.Trim();
                    var borderBrush = ClanBrushes.TryGetValue(clanName, out var b) ? b : Brushes.White;
                    var castleRect = new Rectangle(pt.X + 5, pt.Y + 5, TileSize - 10, TileSize - 10);
                    g.FillRectangle(Brushes.LightGray, castleRect);
                    using var borderPen = new Pen(borderBrush, 3);
                    g.DrawRectangle(borderPen, castleRect);
                    g.FillRectangle(Brushes.DarkRed, pt.X + 13, pt.Y + 7, 5, 12);
                    g.FillRectangle(Brushes.DarkRed, pt.X + 24, pt.Y + 7, 5, 12);
                }
            }
        }

        private void DrawArmy(Graphics g, ArmyDto army)
        {
            var pt = MapToViewport(army.Position.X, army.Position.Y);
            if (!viewportBounds.Contains(pt))
            {
                return;
            }

            var color = ClanBrushes.TryGetValue(army.Owner ?? string.Empty, out var brush) ? brush : Brushes.DarkRed;
            var cx = pt.X + TileSize / 2;
            var cy = pt.Y + TileSize / 2;
            var diamond = new[]
            {
                new Point(cx, cy - 11),
                new Point(cx + 11, cy),
                new Point(cx, cy + 11),
                new Point(cx - 11, cy)
            };
            g.FillPolygon(color, diamond);
            g.DrawPolygon(Pens.Black, diamond);
        }

        private static Color ColorForTerrain(string terrainType)
        {
            var key = CleanTerrainName(terrainType);
            return key switch
            {
                "Forest" => Color.FromArgb(21, 118, 34),
                "Mountain" => Color.FromArgb(128, 128, 128),
                "Grass" => Color.FromArgb(86, 172, 84),
                "Water" => Color.FromArgb(0, 108, 213),
                "Hill" => Color.FromArgb(70, 143, 61),
                "Marsh" => Color.FromArgb(54, 111, 45),
                "Road" => Color.FromArgb(134, 134, 134),
                "Bridge" => Color.FromArgb(140, 103, 53),
                "Castle" => Color.FromArgb(86, 172, 84),
                "Library" => Color.LightSteelBlue,
                "Ruins" => Color.DimGray,
                "Sage" => Color.MediumPurple,
                "Temple" => Color.LightYellow,
                "Tomb" => Color.SaddleBrown,
                "Tower" => Color.LightSlateGray,
                "Void" => Color.Black,
                _ => Color.FromArgb(86, 172, 84)
            };
        }

        private static string CleanTerrainName(string terrainType)
        {
            var match = System.Text.RegularExpressions.Regex.Match(terrainType ?? string.Empty, @"^(.*?)\(\d+\)$");
            return match.Success ? match.Groups[1].Value : terrainType ?? string.Empty;
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
