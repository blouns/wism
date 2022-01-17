using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Wism.Client.Entities;
using Wism.Client.War;
using System.IO;
using Newtonsoft.Json;
using Wism.Client.Core;
using Wism.Client.Modules;
using System.Collections.Generic;

namespace Wism.Client.Test.Common
{
    public static class TestGameFactory
    {
        public const string DataPath = "Data";

        public static GameEntity CreateDefaultNewGameSettings(string worldName, int seed = 1990)
        {
            GameEntity settings = new GameEntity();

            settings.Random = new RandomEntity() { Seed = seed };
            settings.WarStrategy = CreateDefaultWarStrategy();
            settings.Players = CreateDefaultPlayers();
            settings.World = CreateWorld(worldName);

            return settings;
        }

        private static WorldEntity CreateWorld(string worldName)
        {
            // Deserialize map from the world
            WorldEntity entity = Deserialize<WorldEntity>($@"{DataPath}\{worldName}\Map.json");

            // Deserialize other objects
            entity.Cities = Deserialize<CityEntity[]>($@"{DataPath}\{worldName}\City.json");
            entity.Locations = Deserialize<LocationEntity[]>($@"{DataPath}\{worldName}\Location.json");

            return entity;
        }        

        private static T Deserialize<T>(string path)
        {
            var json = File.ReadAllText(path);

            var jsonSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            return JsonConvert.DeserializeObject<T>(json, jsonSettings);
        }

        private static PlayerEntity[] CreateDefaultPlayers()
        {
            return new PlayerEntity[]
            {
                new PlayerEntity()
                {
                    ClanShortName = "Sirians",
                    IsHuman = true
                },
                new PlayerEntity()
                {
                    ClanShortName = "LordBane",
                    IsHuman = true
                }
            };
        }

        private static WarStrategyEntity CreateDefaultWarStrategy()
        {
            var entity = new WarStrategyEntity()
            {
                AssemblyName = Assembly.GetAssembly(typeof(DefaultWarStrategy)).FullName,
                TypeName = (typeof(DefaultWarStrategy)).FullName
            };

            return entity;
        }
    }
}
