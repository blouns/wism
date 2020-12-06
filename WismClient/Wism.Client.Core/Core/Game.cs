using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.War;

namespace Wism.Client.Core
{
    public class Game
    {
        private static Game current;

        public const int DefaultRandomSeed = 1990;

        private int turn = 1;
        private int currentPlayerIndex;
        private GameState gameState;
        private List<Army> selectedArmies;

        public World World { get; set; }

        public List<Player> Players { get; set; }

        public Random Random { get; set; }

        public IWarStrategy WarStrategy { get; set; }

        public GameState GameState { get => gameState; }

        public int Turn { get => turn;  }

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

        public void Transition(GameState newState)
        {
            // For now just set it; later we can validate and manage the state machine
            this.gameState = newState;
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
            this.selectedArmies = null;

            // Set next players turn
            currentPlayerIndex = (currentPlayerIndex + 1) % Players.Count;

            turn++;
            Transition(GameState.StartingTurn);

            return true;
        }

        public bool StartTurn()
        {
            // TODO: Process production, new heros, evaluate if player is alive, etc.

            Transition(GameState.Ready);

            return true;
        }

        public List<Army> GetSelectedArmies()
        {
            if (selectedArmies == null)
            {
                return null;
            }

            // return a copy of the list
            return new List<Army>(selectedArmies);
        }

        public void SelectArmies(List<Army> visitingArmies)
        {
            if (visitingArmies is null)
            {
                throw new ArgumentNullException(nameof(visitingArmies));
            }

            if (!visitingArmies.TrueForAll(army => GetCurrentPlayer() == army.Player))
            {
                throw new InvalidOperationException("Only the current player can select an army.");
            }

            this.selectedArmies = new List<Army>(visitingArmies);
            Transition(GameState.SelectedArmy);
        }

        public void RemoveSelectedArmies(List<Army> armies)
        {
            if (selectedArmies == null || selectedArmies.Count == 0)
            {
                return;
            }

            armies.ForEach(a => selectedArmies.Remove(a));
        }

        public void DeselectArmies()
        {
            this.selectedArmies = null;
            Transition(GameState.Ready);
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
    public enum GameState
    {
        Ready,
        SelectedArmy,
        MovingArmy,
        AttackingArmy,
        EndingTurn,
        StartingTurn,
        GameOver
    }
}
