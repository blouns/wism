using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{    
    public class World
    {
        private const string DefaultMapPath = @"world.json";
        private string mapPath = DefaultMapPath;

        private Random random;

        private IList<Player> players;

        public IList<Player> Players { get => players; set => players = value; }

        private static World current;
        
        public static World Current
        {
            get
            {
                if (World.current == null)
                {
                    throw new InvalidOperationException("No current world exists.");
                }

                return World.current;
            }
        }

        private Tile[,] map;

        public Tile[,] Map { get => map; }        

        private IWarStrategy warStrategy;

        public IWarStrategy WarStrategy { get => warStrategy; set => warStrategy = value; }
        public Random Random { get => random; set => random = value; }

        private World()
        {
            // Create from Factory method
        }

        public static void CreateDefaultWorld()
        {
            World oldWorld = World.current;
            try
            {
                MapBuilder.Initialize();
                World.current = new World();
                World.current.Reset();
            }
            catch
            {
                Log.WriteLine(Log.TraceLevel.Critical, "Unable to create the default world.");
                World.current = oldWorld;
                throw;
            }
        }

        public static void CreateWorld(Tile[,] map)
        {
            World oldWorld = World.current;
            try
            {                
                World.current = new World();
                World.current.Reset(map);
            }
            catch
            {
                Log.WriteLine(Log.TraceLevel.Critical, "Unable to create the default world.");
                World.current = oldWorld;
                throw;
            }
        }

        private IList<Player> ReadyPlayers()
        {
            List<Player> players = new List<Player>();

            // Default two players for now
            AffiliationInfo affiliationInfo = AffiliationInfo.GetAffiliationInfo("Or"); // Orcs of Kor
            Affiliation affiliation = Affiliation.Create(affiliationInfo);
            Player player1 = Player.Create(affiliation);
            players.Add(player1);

            affiliationInfo = AffiliationInfo.GetAffiliationInfo("El"); // Elvallie
            affiliation = Affiliation.Create(AffiliationInfo.GetAffiliationInfo("El"));
            Player player2 = Player.Create(affiliation);
            players.Add(player2);

            return players;
        }

        private static AffiliationInfo GetAffiliationInfo(int index)
        {
            IList<AffiliationInfo> infos = ModFactory.GetAffiliationInfos(ModFactory.ModPath);

            if (infos.Count < index)
                throw new ArgumentOutOfRangeException("index", "Affiliation infos were empty.");

            return infos[index];
        }

        public void Reset()
        {            
            Tile[,] map = MapBuilder.LoadMapFromFile(DefaultMapPath);
            Reset(map);
        }

        public void Reset(Tile[,] map)
        {
            //if (!IsValid(map))
            //    throw new ArgumentException("Map is null or contains invalid elements.");

            this.map = map;
            this.players = this.ReadyPlayers();
            this.warStrategy = new DefaultWarStrategy();
            this.Random = new Random();
        }

        private bool IsValid(Tile[,] map)
        {
            bool isValid = true;
            for (int x = 0; x < map.GetLength(1); x++)
            {
                for (int y = 0; y < map.GetLength(0); y++)
                {
                    if (!(map[x, y] is Tile))
                    {
                        isValid = false;
                    }

                    // Valid terrain
                    if ((map[x, y].Terrain == null) ||
                        (!MapBuilder.TerrainKinds.ContainsKey(map[x, y].Terrain.ID)))
                    {
                        isValid = false;
                    }

                    // Valid units; units are optional
                    if ((map[x, y].Army != null) &&
                        (MapBuilder.UnitKinds.ContainsKey(map[x, y].Army.ID)))
                    {
                        isValid = false;
                    }
                }
            }

            return isValid;
        }
    }   
}
