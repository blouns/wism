using System.Drawing;
using System.Windows.Forms;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.CompanionApp.WinForms
{
    public class MapRenderer : Control
    {
        private MapSnapshot _currentMap;
        private ToolTip _toolTip = new();
        private Point _lastHoverTile = Point.Empty;

        public MapRenderer()
        {
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
            Invalidate(); // triggers redraw
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_currentMap == null)
            {
                Console.WriteLine("[MapRenderer] No map assigned.");
                return;
            }

            if (_currentMap.Tiles == null)
            {
                Console.WriteLine("[MapRenderer] Map tiles are null.");
                return;
            }

            Console.WriteLine($"[MapRenderer] Painting map {_currentMap.Width}x{_currentMap.Height}");

            int tileSize = 32;

            foreach (var tile in _currentMap.Tiles)
            {
                Brush brush = tile.TerrainType == "Forest" ? Brushes.DarkGreen : Brushes.LightGreen;
                e.Graphics.FillRectangle(brush, tile.X * tileSize, tile.Y * tileSize, tileSize, tileSize);
                e.Graphics.DrawRectangle(Pens.Black, tile.X * tileSize, tile.Y * tileSize, tileSize, tileSize);

                if (tile.HasCity)
                {
                    e.Graphics.FillEllipse(Brushes.SlateGray, tile.X * tileSize + 8, tile.Y * tileSize + 8, 16, 16);
                }
            }


            foreach (var hero in _currentMap.Heroes)
            {
                e.Graphics.FillEllipse(Brushes.Red,
                    hero.Position.X * tileSize + 6,
                    hero.Position.Y * tileSize + 6,
                    20, 20);
            }
        }
    }
}
