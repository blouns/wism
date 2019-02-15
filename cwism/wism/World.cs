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

            // TODO: This logic should move out of World completely

            // Default two players for now
            AffiliationInfo affiliationInfo = AffiliationInfo.GetAffiliationInfo("Sirians");
            Affiliation affiliation = Affiliation.Create(affiliationInfo);
            Player player1 = Player.Create(affiliation);
            players.Add(player1);

            affiliationInfo = AffiliationInfo.GetAffiliationInfo("LordBane");
            affiliation = Affiliation.Create(affiliationInfo);
            Player player2 = Player.Create(affiliation);
            players.Add(player2);

            return players;
        }

        private static AffiliationInfo GetAffiliationInfo(int index)
        {
            IList<AffiliationInfo> infos = ModFactory.LoadAffiliationInfos(ModFactory.ModPath);

            if (infos.Count < index)
                throw new ArgumentOutOfRangeException("index", "Affiliation infos were empty.");

            return infos[index];
        }

        public void Reset()
        {
            Tile[,] map = MapBuilder.CreateDefaultMap();
            Reset(map);
        }

        public void Reset(Tile[,] map)
        {
            Validate(map);

            this.map = map;            
            this.warStrategy = new DefaultWarStrategy();
            this.Random = new Random();

            // TODO: Move player creation outside of World
            this.players = this.ReadyPlayers();
        }

        private void Validate(Tile[,] map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    if (map[x, y] == null)
                    {
                        throw new ArgumentException(
                            String.Format("Map tile is null at ({0}, {1})", x, y));
                    }

                    if (!(map[x, y] is Tile))
                    {
                        throw new ArgumentException(
                            String.Format("Map contains element not of type Tile at ({0}, {1})", x, y));
                    }

                    if (map[x, y].Terrain == null)
                    {
                        throw new ArgumentException(
                            String.Format("Map contains null Terrain at ({0}, {1})", x, y));
                    }
                        
                    if (!MapBuilder.TerrainKinds.ContainsKey(map[x, y].Terrain.ID))
                    {
                        throw new ArgumentException(
                            String.Format("Map contains unknown Terrain '{2}' at ({0}, {1})", x, y, map[x, y].Terrain.ID));
                    }

                    // Valid units; units are optional
                    if ((map[x, y].Army != null) &&
                        (!MapBuilder.UnitKinds.ContainsKey(map[x, y].Army.ID)))
                    {
                        throw new ArgumentException(
                            String.Format("Map tile contains unknown Army type '{2}' at ({0}, {1})", x, y, map[x, y].Army.ID));
                    }
                }
            }
        }
    }   
}
