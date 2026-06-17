using NUnit.Framework;
using UnityEngine.UIElements;
using Wism.Companion.Shared.Models;
using WismCompanion.UI;

namespace WismCompanion.Tests
{
    public sealed class UiHelperTests
    {
        [TestCase("Grass(4)", "Grass")]
        [TestCase("Mountain", "Mountain")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void MapColors_CleanTerrainName_RemovesNumericSuffix(string raw, string expected)
        {
            Assert.That(MapColors.CleanTerrainName(raw), Is.EqualTo(expected));
        }

        [Test]
        public void MapColors_UnknownClanUsesNeutralColor()
        {
            Assert.That(MapColors.ClanColor("missing"), Is.EqualTo(MapColors.NeutralClan));
            Assert.That(MapColors.ClanColor(" "), Is.EqualTo(MapColors.NeutralClan));
        }

        [Test]
        public void MapColors_KnownClanIsStableWhenTrimmed()
        {
            Assert.That(MapColors.ClanColor(" Sirians "), Is.EqualTo(MapColors.ClanColor("Sirians")));
        }

        [TestCase("Light Infantry", "lightinfantry")]
        [TestCase("HeavyInfantry", "heavyinfantry")]
        [TestCase("DwarvenLegions", "dwarvenlegion")]
        [TestCase("Wolf Riders", "wolfriders")]
        public void SpriteRegistry_NormalizeArmyUnit_UsesResourceKeys(string raw, string expected)
        {
            Assert.That(SpriteRegistry.NormalizeArmyUnit(raw), Is.EqualTo(expected));
        }

        [Test]
        public void InspectorView_ShowEmptyWritesHint()
        {
            var root = new VisualElement();
            var inspector = new InspectorView(root);

            inspector.ShowEmpty("No map yet.");

            Assert.That(root.childCount, Is.EqualTo(1));
            Assert.That(((Label)root[0]).text, Is.EqualTo("No map yet."));
        }

        [Test]
        public void InspectorView_ShowRendersTileCityArmyAndLocation()
        {
            var root = new VisualElement();
            var inspector = new InspectorView(root);
            var map = CompanionStateTests.Map("alpha", 8, 8);
            var selection = new MapSelection(
                1,
                2,
                map.Tiles[0],
                map.Armies[0],
                map.Cities[0],
                map.Locations[0]);

            inspector.Show(selection, map);

            Assert.That(root.Query<Label>().ToList().ConvertAll(label => label.text), Does.Contain("Tile (1, 2)"));
            Assert.That(root.Query<Label>().ToList().ConvertAll(label => label.text), Does.Contain("Marthos"));
            Assert.That(root.Query<Label>().ToList().ConvertAll(label => label.text), Does.Contain("Hero"));
            Assert.That(root.Query<Label>().ToList().ConvertAll(label => label.text), Does.Contain("Old Ruins"));
        }

        [Test]
        public void InspectorView_ShowEmptySelectionWritesEmptyTileHint()
        {
            var root = new VisualElement();
            var inspector = new InspectorView(root);

            inspector.Show(new MapSelection(4, 5, null, null, null, null), CompanionStateTests.Map("alpha", 8, 8));

            Assert.That(root.Query<Label>().ToList().ConvertAll(label => label.text), Does.Contain("Empty tile."));
        }

        [Test]
        public void MapSelection_StoresProvidedDtoReferences()
        {
            var tile = new TileDto { X = 1, Y = 2, TerrainType = "Road" };
            var army = new ArmyDto { Name = "Hero" };
            var city = new CityDto { Name = "Marthos" };
            var location = new LocationDto { Name = "Ruins" };

            var selection = new MapSelection(1, 2, tile, army, city, location);

            Assert.That(selection.X, Is.EqualTo(1));
            Assert.That(selection.Y, Is.EqualTo(2));
            Assert.That(selection.Tile, Is.SameAs(tile));
            Assert.That(selection.Army, Is.SameAs(army));
            Assert.That(selection.City, Is.SameAs(city));
            Assert.That(selection.Location, Is.SameAs(location));
        }
    }
}
