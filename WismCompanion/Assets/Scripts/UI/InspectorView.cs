using UnityEngine.UIElements;
using Wism.Companion.Shared.Events;

namespace WismCompanion.UI
{
    /// <summary>
    /// Renders the entities under the clicked tile (tile / city / army / location) into the
    /// inspector panel. Bound to the shared DTOs, not the core game model.
    /// </summary>
    public sealed class InspectorView
    {
        private readonly VisualElement content;

        public InspectorView(VisualElement content)
        {
            this.content = content;
        }

        public void ShowEmpty(string message)
        {
            content.Clear();
            content.Add(Hint(message));
        }

        public void Show(MapSelection selection, MapSnapshot map)
        {
            content.Clear();
            content.Add(Header($"Tile ({selection.X}, {selection.Y})"));

            if (selection.Tile != null)
            {
                content.Add(Field("Terrain", MapColors.CleanTerrainName(selection.Tile.TerrainType)));
                content.Add(Field("Has city", selection.Tile.HasCity ? "yes" : "no"));
            }

            var inf = map?.Influence;
            if (inf != null && inf.Width > 0 && selection.X >= 0 && selection.Y >= 0 &&
                selection.X < inf.Width && selection.Y < inf.Height)
            {
                var idx = inf.IndexOf(selection.X, selection.Y);
                content.Add(Header("Influence"));
                if (inf.Friendly != null && idx < inf.Friendly.Length)
                    content.Add(Field("Friendly", inf.Friendly[idx].ToString("0.00")));
                if (inf.Enemy != null && idx < inf.Enemy.Length)
                    content.Add(Field("Enemy", inf.Enemy[idx].ToString("0.00")));
                if (inf.Tension != null && idx < inf.Tension.Length)
                    content.Add(Field("Tension", inf.Tension[idx].ToString("+0.00;-0.00;0.00")));
            }

            if (selection.City != null)
            {
                content.Add(Header("City"));
                content.Add(Field("Name", Safe(selection.City.Name)));
                content.Add(OwnerField(selection.City.Owner));
                content.Add(Field("Defense", selection.City.Defense.ToString()));
            }

            if (selection.Army != null)
            {
                content.Add(Header(selection.Army.IsHero ? "Hero" : "Army"));
                content.Add(Field("Name", Safe(selection.Army.Name)));
                content.Add(OwnerField(selection.Army.Owner));
                content.Add(Field("Health", selection.Army.Health.ToString()));
            }

            if (selection.Location != null)
            {
                content.Add(Header("Location"));
                content.Add(Field("Name", Safe(selection.Location.Name)));
                content.Add(Field("Type", Safe(selection.Location.Type)));
            }

            if (selection.Tile == null && selection.City == null && selection.Army == null && selection.Location == null)
            {
                content.Add(Hint("Empty tile."));
            }
        }

        private static VisualElement Field(string key, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("insp-row");

            var keyLabel = new Label(key);
            keyLabel.AddToClassList("insp-key");
            row.Add(keyLabel);

            var valueLabel = new Label(value);
            valueLabel.AddToClassList("insp-val");
            row.Add(valueLabel);

            return row;
        }

        private static VisualElement OwnerField(string owner)
        {
            var row = new VisualElement();
            row.AddToClassList("insp-row");

            var keyLabel = new Label("Owner");
            keyLabel.AddToClassList("insp-key");
            row.Add(keyLabel);

            var swatch = new VisualElement();
            swatch.AddToClassList("insp-swatch");
            swatch.style.backgroundColor = MapColors.ClanColor(owner);
            row.Add(swatch);

            var valueLabel = new Label(string.IsNullOrWhiteSpace(owner) ? "Neutral" : owner);
            valueLabel.AddToClassList("insp-val");
            row.Add(valueLabel);

            return row;
        }

        private static Label Header(string text)
        {
            var label = new Label(text);
            label.AddToClassList("insp-header");
            return label;
        }

        private static Label Hint(string text)
        {
            var label = new Label(text);
            label.AddToClassList("insp-hint");
            return label;
        }

        private static string Safe(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
    }
}
