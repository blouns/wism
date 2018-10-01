using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BranallyGames.Wism
{    
    public class World
    {
        private const string DefaultMapPath = @"world.json";

        private string mapPath = DefaultMapPath;

        private IList<Player> players;

        public IList<Player> Players { get => players; set => players = value; }

        private static World current;
        
        public static World Current { get => current; }

        public Tile[,] Map { get => map; }        

        private Tile[,] map;

        static World()
        {
            current = new World();
            CreateDefaultWorld(current);
        }

        private static void CreateDefaultWorld(World world)
        {            
            world.map = MapBuilder.LoadMap(DefaultMapPath);
            world.players = ReadyPlayers();
        }

        private static IList<Player> ReadyPlayers()
        {
            List<Player> players = new List<Player>();

            // TODO: For now just one player
            AffiliationInfo affiliationInfo = GetFirstAffiliationInfo();
            Affiliation affiliation = Affiliation.Create(affiliationInfo);
            Player player1 = Player.Create(affiliation);
            players.Add(player1);

            return players;
        }

        private static AffiliationInfo GetFirstAffiliationInfo()
        {
            IList<AffiliationInfo> infos = ModFactory.GetAffiliationInfos();

            if (infos.Count == 0)
                throw new InvalidOperationException("Affiliation infos were empty.");

            return infos[0];
        }

        public void Serialize(string path)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects;
            settings.Formatting = Formatting.Indented;
            string json = JsonConvert.SerializeObject(this, settings);
            File.WriteAllText(path, json);
        }

        public void Serialize()
        {
            Serialize(World.DefaultMapPath);            
        }

        public void Reset()
        {
            CreateDefaultWorld(World.Current);
        }
    }   
}
