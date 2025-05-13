using System.Drawing;
using System.Windows.Forms;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.CompanionApp.WinForms
{
    public class MapRenderer : Control
    {
        private const int TileSize = 32;
        private const int ViewportTilesWide = 25;
        private const int ViewportTilesHigh = 25;

        private Point cameraCenter = new(0, 0);  // This is the center of the viewport

        private MapSnapshot _currentMap;
        private ToolTip _toolTip = new();
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
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
            this.MouseMove += MapRenderer_MouseMove;
        }

        private void MapRenderer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentMap?.Tiles == null || _currentMap.Tiles.Count == 0)
                return;

            int tileSize = 32;
            int tileX = e.X / tileSize;
            int tileY = e.Y / tileSize;

            if (tileX < 0 || tileY < 0 || tileX >= _currentMap.Width || tileY >= _currentMap.Height)
                return;

            var hovered = new Point(tileX, tileY);

            if (_lastHoverTile == hovered)
                return; // skip redundant updates

            _lastHoverTile = hovered;

            var tile = _currentMap.Tiles.FirstOrDefault(t => t.X == tileX && t.Y == tileY);
            if (tile != null)
            {
                string tip = $"({tile.X},{tile.Y}) - {tile.TerrainType}" + (tile.HasCity ? " (City)" : "");
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
                var first = snapshot.Armies.First().Position;
                cameraCenter = new Point(first.X, first.Y);
            }

            tileCache.Clear();
            Invalidate();
        }




        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TranslateTransform(0, 0); // reset origin if needed

            base.OnPaint(e);

            if (_currentMap == null || _currentMap.Tiles == null)
            {
                Console.WriteLine("[MapRenderer] No map or tiles to draw.");
                return;
            }            

            int viewOriginX = cameraCenter.X - ViewportTilesWide / 2;
            int viewOriginY = cameraCenter.Y - ViewportTilesHigh / 2;

            if (offscreenBuffer == null || offscreenBuffer.Width != Width || offscreenBuffer.Height != Height)
            {
                offscreenBuffer?.Dispose();
                offscreenBuffer = new Bitmap(Width, Height);
            }

            using (var g = Graphics.FromImage(offscreenBuffer))
            {
                g.Clear(Color.Black);

                foreach (var tile in _currentMap.Tiles)
                {
                    if (tile.X < viewOriginX || tile.X >= viewOriginX + ViewportTilesWide ||
                        tile.Y < viewOriginY || tile.Y >= viewOriginY + ViewportTilesHigh)
                        continue;
                    
                    DrawTile(g, tile);
                }

                foreach (var army in _currentMap.Armies)
                {
                    if (army.Position == null) continue;

                    int flippedY = _currentMap.Height - 1 - army.Position.Y;
                    int drawX = (army.Position.X - viewOriginX) * TileSize + TileSize / 2;
                    int drawY = (flippedY - (_currentMap.Height - 1 - cameraCenter.Y - ViewportTilesHigh / 2)) * TileSize + TileSize / 2;

                    var color = ClanBrushes.TryGetValue(army.Owner ?? "", out var b) ? b : Brushes.DarkRed;

                    Point[] diamond =
                    {
                        new Point(drawX, drawY - 10),
                        new Point(drawX + 10, drawY),
                        new Point(drawX, drawY + 10),
                        new Point(drawX - 10, drawY)
                    };

                    g.FillPolygon(color, diamond);
                }

                // Draw selected army highlight
                if (_currentMap.SelectedArmy != null)
                {
                    var selected = _currentMap.SelectedArmy;
                    int flippedY = _currentMap.Height - 1 - selected.Position.Y;

                    int drawX = (selected.Position.X - viewOriginX) * TileSize + TileSize / 2;
                    int drawY = (flippedY - (_currentMap.Height - 1 - cameraCenter.Y - ViewportTilesHigh / 2)) * TileSize + TileSize / 2;

                    Point[] diamond =
                    {
                        new Point(drawX, drawY - 10),
                        new Point(drawX + 10, drawY),
                        new Point(drawX, drawY + 10),
                        new Point(drawX - 10, drawY)
                    };

                    g.DrawPolygon(Pens.Yellow, diamond);
                }
            }

            e.Graphics.DrawImageUnscaled(offscreenBuffer, 0, 0);
        }


        private void DrawTile(Graphics g, TileDto tile)
        {
            int halfW = ViewportTilesWide / 2;
            int halfH = ViewportTilesHigh / 2;

            // Viewport offset from center
            int viewOriginX = cameraCenter.X - halfW;
            int viewOriginY = cameraCenter.Y - halfH;

            // Flip Y to render bottom-left origin
            int flippedY = _currentMap.Height - 1 - tile.Y;

            // Compute draw positions relative to camera
            int drawX = (tile.X - viewOriginX) * TileSize;
            int drawY = (flippedY - (_currentMap.Height - 1 - cameraCenter.Y - halfH)) * TileSize;

            // Terrain color selection
            Brush brush = tile.TerrainType switch
            {
                "Forest" => Brushes.DarkGreen,
                "Mountain" => Brushes.Gray,
                "Water" => Brushes.Blue,
                "Hill" => Brushes.Olive,
                "Road" => Brushes.SandyBrown,
                "Bridge" => Brushes.SaddleBrown,
                "Castle" => Brushes.DarkSlateGray,
                _ => Brushes.LightGreen
            };

            g.FillRectangle(brush, drawX, drawY, TileSize, TileSize);
            g.DrawRectangle(Pens.Black, drawX, drawY, TileSize, TileSize);

            if (tile.HasCity)
            {
                g.FillEllipse(Brushes.SlateGray, drawX + 8, drawY + 8, 16, 16);
            }
        }

        public void TrackArmyAt(PositionDto position)
        {
            if (position != null)
            {
                cameraCenter = new Point(position.X, position.Y);
                Invalidate();
            }
        }

    }
}
