using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Assets.Scripts.UI
{
    public sealed class ProductionChoiceViewModel
    {
        public string ArmyInfoName { get; set; }
        public string DisplayName { get; set; }
        public int TurnsToProduce { get; set; }
        public int Upkeep { get; set; }
    }

    public sealed class ProductionDeliveryViewModel
    {
        public City SourceCity { get; set; }
        public City DestinationCity { get; set; }
        public string SourceCityName { get; set; }
        public string DestinationCityName { get; set; }
        public string ArmyDisplayName { get; set; }
        public int TurnsRemaining { get; set; }
    }

    public sealed class ProductionMinimapMarkerViewModel
    {
        public City City { get; set; }
        public string Kind { get; set; }
    }

    public sealed class ProductionCityViewModel
    {
        public City City { get; set; }
        public string CityName { get; set; }
        public string CurrentArmyName { get; set; }
        public City CurrentDestinationCity { get; set; }
        public string DestinationCityName { get; set; }
        public int? TurnsRemaining { get; set; }
        public bool IsIdle { get; set; }
        public IReadOnlyList<ProductionChoiceViewModel> Choices { get; set; }
        public IReadOnlyList<ProductionDeliveryViewModel> OutgoingDeliveries { get; set; }
        public IReadOnlyList<ProductionDeliveryViewModel> IncomingSources { get; set; }
    }

    public sealed class ProductionManagementViewModel
    {
        public ProductionPanelMode Mode { get; set; }
        public int SelectedCityIndex { get; set; }
        public IReadOnlyList<ProductionCityViewModel> Cities { get; set; }
        public IReadOnlyList<ProductionMinimapMarkerViewModel> MinimapMarkers { get; set; }
        public ProductionCityViewModel SelectedCity => this.Cities[this.SelectedCityIndex];
    }

    public static class ProductionPanelViewModelBuilder
    {
        public static ProductionManagementViewModel BuildSingleCity(City city)
        {
            return new ProductionManagementViewModel
            {
                Mode = ProductionPanelMode.SingleCity,
                SelectedCityIndex = 0,
                Cities = new[] { BuildCity(city, new[] { city }) },
                MinimapMarkers = BuildMinimapMarkers(new[] { city }, city)
            };
        }

        public static ProductionManagementViewModel BuildManagement(Player player, City selectedCity = null)
        {
            var cities = player.GetCities();
            var selectedIndex = selectedCity == null ? 0 : cities.FindIndex(city => city == selectedCity);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            return new ProductionManagementViewModel
            {
                Mode = ProductionPanelMode.Management,
                SelectedCityIndex = selectedIndex,
                Cities = BuildManagementCities(cities).ToArray(),
                MinimapMarkers = BuildMinimapMarkers(cities, cities[selectedIndex])
            };
        }

        public static ProductionManagementViewModel BuildManagement(IReadOnlyList<City> cities, City selectedCity = null)
        {
            var selectedIndex = selectedCity == null ? 0 : FindCityIndex(cities, selectedCity);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            return new ProductionManagementViewModel
            {
                Mode = ProductionPanelMode.Management,
                SelectedCityIndex = selectedIndex,
                Cities = BuildManagementCities(cities).ToArray(),
                MinimapMarkers = BuildMinimapMarkers(cities, cities[selectedIndex])
            };
        }

        private static IEnumerable<ProductionCityViewModel> BuildManagementCities(IReadOnlyList<City> cities)
        {
            return cities.Select(city => BuildCity(city, cities));
        }

        private static int FindCityIndex(IReadOnlyList<City> cities, City city)
        {
            for (var i = 0; i < cities.Count; i++)
            {
                if (cities[i] == city)
                {
                    return i;
                }
            }

            return -1;
        }

        private static ProductionCityViewModel BuildCity(City city, IReadOnlyList<City> ownedCities)
        {
            var barracks = city.Barracks;
            var armyInTraining = barracks.ArmyInTraining;
            var destinationCity = armyInTraining?.DestinationCity ?? city;
            return new ProductionCityViewModel
            {
                City = city,
                CityName = city.DisplayName,
                CurrentArmyName = armyInTraining?.ArmyInfo?.DisplayName,
                CurrentDestinationCity = destinationCity,
                DestinationCityName = destinationCity.DisplayName,
                TurnsRemaining = armyInTraining?.TurnsToProduce,
                IsIdle = !barracks.ProducingArmy(),
                Choices = barracks.GetProductionKinds()
                    .Select(info => new ProductionChoiceViewModel
                    {
                        ArmyInfoName = info.ArmyInfoName,
                        DisplayName = ResolveArmyDisplayName(info.ArmyInfoName),
                        TurnsToProduce = info.TurnsToProduce,
                        Upkeep = info.Upkeep
                    })
                    .ToArray(),
                OutgoingDeliveries = barracks.HasDeliveries()
                    ? barracks.ArmiesToDeliver.Select(ToDelivery).Take(2).ToArray()
                    : new ProductionDeliveryViewModel[0],
                IncomingSources = ownedCities
                    .Where(source => source != city)
                    .SelectMany(source => IncomingFrom(source, city))
                    .Take(4)
                    .ToArray()
            };
        }

        private static IEnumerable<ProductionDeliveryViewModel> IncomingFrom(City source, City destination)
        {
            if (source.Barracks.ArmyInTraining?.DestinationCity == destination)
            {
                yield return ToDelivery(source.Barracks.ArmyInTraining);
            }

            if (!source.Barracks.HasDeliveries())
            {
                yield break;
            }

            foreach (var delivery in source.Barracks.ArmiesToDeliver.Where(delivery => delivery.DestinationCity == destination))
            {
                yield return ToDelivery(delivery);
            }
        }

        private static ProductionDeliveryViewModel ToDelivery(ArmyInTraining delivery)
        {
            var destination = delivery.DestinationCity ?? delivery.ProductionCity;
            return new ProductionDeliveryViewModel
            {
                SourceCity = delivery.ProductionCity,
                DestinationCity = destination,
                SourceCityName = delivery.ProductionCity?.DisplayName,
                DestinationCityName = destination?.DisplayName,
                ArmyDisplayName = delivery.ArmyInfo?.DisplayName,
                TurnsRemaining = delivery.DestinationCity == null ? delivery.TurnsToProduce : delivery.TurnsToDeliver
            };
        }

        private static IReadOnlyList<ProductionMinimapMarkerViewModel> BuildMinimapMarkers(IReadOnlyList<City> cities, City selectedCity)
        {
            var markers = new List<ProductionMinimapMarkerViewModel>();
            foreach (var city in cities)
            {
                markers.Add(new ProductionMinimapMarkerViewModel
                {
                    City = city,
                    Kind = city == selectedCity
                        ? "selected"
                        : city.Barracks.ProducingArmy() ? "producing" : "idle"
                });

                var destination = city.Barracks.ArmyInTraining?.DestinationCity;
                if (destination != null && destination != city)
                {
                    markers.Add(new ProductionMinimapMarkerViewModel
                    {
                        City = destination,
                        Kind = city == selectedCity ? "redirect-target" : "receiver"
                    });
                }

                if (!city.Barracks.HasDeliveries())
                {
                    continue;
                }

                foreach (var delivery in city.Barracks.ArmiesToDeliver)
                {
                    if (delivery.DestinationCity != null)
                    {
                        markers.Add(new ProductionMinimapMarkerViewModel
                        {
                            City = delivery.DestinationCity,
                            Kind = city == selectedCity ? "delivery-target" : "receiver"
                        });
                    }
                }
            }

            return markers;
        }

        private static string ResolveArmyDisplayName(string armyInfoName)
        {
            try
            {
                return ModFactory.FindArmyInfo(armyInfoName)?.DisplayName ?? armyInfoName;
            }
            catch
            {
                return armyInfoName;
            }
        }
    }
}
