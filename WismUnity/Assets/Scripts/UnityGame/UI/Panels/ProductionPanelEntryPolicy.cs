using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public static class ProductionPanelEntryPolicy
    {
        public static bool TryGetCity(Tile tile, out City city)
        {
            city = tile?.City;
            return city != null;
        }

        public static bool TryGetOwnedCity(Tile tile, Clan currentClan, out City city)
        {
            if (TryGetCity(tile, out city) && IsOwnedCity(city, currentClan))
            {
                return true;
            }

            city = null;
            return false;
        }

        public static bool IsOwnedCity(City city, Clan currentClan)
        {
            return city != null && currentClan != null && city.Clan == currentClan;
        }
    }
}