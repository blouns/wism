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
            //current.map = MapBuilder.GenerateMap(MapBuilder.DefaultMap);
            current.map = MapBuilder.LoadMap(path);
        }

        public static World Current { get => current; }

        public Tile[,] Map { get => map; }

        private Tile[,] map;

        public void Serialize()
        {
            
            string mapJson = JsonConvert.SerializeObject(this.Map);
            File.WriteAllText(World.path, mapJson);
        }
     }   
}
