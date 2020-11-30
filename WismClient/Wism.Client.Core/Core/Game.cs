using System;
using System.Collections.Generic;
using Wism.Client.Modules;
using Wism.Client.War;

namespace Wism.Client.Core
{
    public class Game
    {
        private static Game current;

        public const int DefaultRandomSeed = 1990;

        private int currentPlayerIndex;

        public World World { get; set; }

        public List<Player> Players { get; set; }

        public Random Random { get; set; }

        public IWarStrategy WarStrategy { get; set; }

        public static Game Current
        {
            get 
            {
                if (Game.current == null)
                {
                    throw new InvalidOperationException("No current game exists.");
                }

                return Game.current;
            }
        }

        public Player GetCurrentPlayer()
        {
            if (Players == null || Players.Count == 0)
            {
                throw new InvalidOperationException("Players have not been initialized.");
            }

            return Players[currentPlayerIndex];
        }

        /// <summary>
        /// End the players turn.
        /// </summary>
        /// <remarks>
        /// Resets moves, triggers production, and allows for other clans 
        /// to complete their turns.
        /// </remarks>
        public bool EndTurn()
        {            
            // End current players turn
            Players[currentPlayerIndex].EndTurn();

            // Set next players turn
            currentPlayerIndex = (currentPlayerIndex + 1) % Players.Count;

            return true;
        }

        public static void CreateDefaultPlayers()
        {
            // Default two players for now
            ClanInfo clanInfo = ClanInfo.GetClanInfo("Sirians");
            Clan clan = Clan.Create(clanInfo);
            Player player1 = Player.Create(clan);
            Current.Players.Add(player1);

            clanInfo = ClanInfo.GetClanInfo("LordBane");
            clan = Clan.Create(clanInfo);
            Player player2 = Player.Create(clan);
            Current.Players.Add(player2);

            Game.Current.currentPlayerIndex = 0;
        }

        public static void CreateDefaultGame()
        {
            current = new Game();
            current.Random = new Random(Game.DefaultRandomSeed);
            current.WarStrategy = new DefaultWarStrategy();

            // Setup default players for testing
            current.Players = new List<Player>();
            CreateDefaultPlayers();

            // Setup default world for testing.
            World.CreateDefaultWorld();
        }
    }
}
