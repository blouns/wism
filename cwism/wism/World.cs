using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace wism
{    
    public class World
    {
        private const string path = @"world.json";

        private static World current;

        static World()
        {
            current = new World();
            current.map = MapBuilder.LoadMap(path);
        }

        public static World Current { get => current; }

        public Tile[,] Map { get => map; }

        private Tile[,] map;

        public void Serialize()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects;
            settings.Formatting = Formatting.Indented;
            string mapJson = JsonConvert.SerializeObject(this.Map, settings);
            File.WriteAllText(World.path, mapJson);
        }

        public void Reset()
        {
            current.map = MapBuilder.LoadMap(path);
        }
    }   
}
