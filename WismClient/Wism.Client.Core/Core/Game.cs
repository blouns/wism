using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Data;
using Wism.Client.War;

namespace Wism.Client.Core
{
    public class Game
    {
        // Default random seed (the year Warlords was born!)
        public const int DefaultRandomSeed = 1990;  

        // Singleton instance
        private static Game current;        

        private int currentPlayerIndex;
        private GameState gameState;
        private List<Army> selectedArmies;
        private List<Tile> nextArmyQueue = new List<Tile>();
        private HashSet<Tile> quitArmySet = new HashSet<Tile>();

        /// <summary>
        /// Active game players
        /// </summary>
        public List<Player> Players { get; set; }

        /// <summary>
        /// Gets or sets the randomization generator used for the game
        /// </summary>
        /// <remarks>
        /// IMPORTANT: *ALL* use of randomization that affects game state *MUST* use
        /// this and only this for randomization. Otherwise, remote players will be 
        /// out of sync with the game state.
        /// </remarks>
        public Random Random { get; set; }

        /// <summary>
        /// Random number seed
        /// </summary>
        public int RandomSeed { get; set; }

        /// <summary>
        /// Gets or sets the strategy used for battles
        /// </summary>
        public IWarStrategy WarStrategy { get; set; }

        /// <summary>
        /// Current GameState
        /// </summary>
        public GameState GameState { get => gameState; }

        public bool IgnoreGameOver { get; set; }

        /// <summary>
        /// Returns the current game instance as a Singleton
        /// </summary>
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

        internal int CurrentPlayerIndex { get => currentPlayerIndex; set => currentPlayerIndex = value; }

        /// <summary>
        /// Test if the game has been initialized.
        /// </summary>
        /// <returns>True if so; otherwise False</returns>
        public static bool IsInitialized()
        {
            return Game.current != null;
        }

        /// <summary>
        /// Take a gave snapshot with all game state.
        /// </summary>
        /// <returns>Game snapshot</returns>
        /// <remarks>
        /// Creates a graph of entities comprising the entire game state 
        /// to be persisted, saved, or shared.
        /// </remarks>
        public GameEntity Snapshot()
        {
            return GamePersistance.SnapshotGame(this);
        }

        /// <summary>
        /// Gets all armies in the world for each player
        /// </summary>
        /// <returns></returns>
        public List<Army> GetAllArmies()
        {
            var armies = new List<Army>();
            foreach (var player in Players)
            {
                armies.AddRange(player.GetArmies());
            }

            return armies;
        }

        /// <summary>
        /// Gets the current player based on their turn
        /// </summary>
        /// <returns>Player whose turn it is now</returns>
        public Player GetCurrentPlayer()
        {
            if (Players == null || Players.Count == 0)
            {
                throw new InvalidOperationException("Players have not been initialized.");
            }

            return Players[CurrentPlayerIndex];
        }

        /// <summary>
        /// Gets the next alive player
        /// </summary>
        /// <returns>Player whose turn is next, or null if no players are alive</returns>
        public Player GetNextPlayer()
        {
            if (Players == null || Players.Count == 0)
            {
                throw new InvalidOperationException("Players have not been initialized.");
            }

            var currentPlayer = GetCurrentPlayer();

            // Find the next alive player
            var nextPlayer = Players[(CurrentPlayerIndex + 1) % Players.Count];
            while (nextPlayer.IsDead)
            {
                nextPlayer = Players[(CurrentPlayerIndex + 1) % Players.Count];
                if (nextPlayer == currentPlayer)
                {
                    // No players are alive
                    nextPlayer = null;
                    break;
                }
            }

            return nextPlayer;
        }

        /// <summary>
        /// Transition to a new game state
        /// </summary>
        /// <param name="newState">State to transition to</param>
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
        public void EndTurn()
        {
            // End current players turn
            DeselectArmies();
            nextArmyQueue.Clear();
            quitArmySet.Clear();
            var player = GetCurrentPlayer();
            player.EndTurn();
            if (SelectNextPlayer())
            {
                Transition(GameState.StartingTurn);
            }
            else
            {
                HandleGameOver();
            }
        }

        /// <summary>
        /// Select the next Player who is still in the fight.
        /// </summary>
        /// <returns>True if next player selected; False if there are no other players left</returns>
        private bool SelectNextPlayer()
        {            
            int lastPlayerIndex = CurrentPlayerIndex;

            // Find next player that is still in the fight
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
            CurrentPlayerIndex = Players.FindIndex(CurrentPlayerIndex, p => !p.IsDead);
            if (CurrentPlayerIndex == -1)
            {
                // Not found in first pass; look starting from beginning of list                
                CurrentPlayerIndex = Players.FindIndex(p => !p.IsDead);
            }

            // This may roll back to the original player (player wins); so return False in that case
            return (lastPlayerIndex != CurrentPlayerIndex);
        }

        /// <summary>
        /// Process production, new heros, evaluate if player is alive, etc.
        /// </summary>
        public void StartTurn()
        {
            var player = GetCurrentPlayer();

            if (player.GetCities().Count == 0 &&
                !IgnoreGameOver)
            {
                // You are no longer in the fight!
                player.IsDead = true;
                Transition(GameState.Ready);
                return;
            }

            player.StartTurn();

            // Select the next army if one is available
            if (!SelectNextArmy())
            {
                Transition(GameState.Ready);
            }            
        }

        /// <summary>
        /// Selects the next army with moves remaining
        /// </summary>
        /// <returns>True if a new army has been selected; otherwise, False</returns>
        public bool SelectNextArmy()
        {
            if (GameState != GameState.Ready &&
                GameState != GameState.SelectedArmy)
            {
                return false;
            }

            if (nextArmyQueue.Count == 0)
            {
                this.nextArmyQueue = GetTilesWithArmiesWithMoves(GetCurrentPlayer());

                // No more armies with moves
                if (nextArmyQueue.Count == 0)
                {
                    return false;
                }
            }

            Game.Current.DeselectArmies();
            var tileWithArmies = nextArmyQueue[0];
            nextArmyQueue.RemoveAt(0);
            SelectArmies(tileWithArmies.Armies);            

            return true;
        }        

        /// <summary>
        /// Get the current selected armies
        /// </summary>
        /// <returns>Armies that are selected</returns>
        public List<Army> GetSelectedArmies()
        {
            if (!ArmiesSelected())
            {
                return null;
            }

            // return a copy of the list
            return new List<Army>(selectedArmies);
        }

        /// <summary>
        /// Sets the given armies as currently selected
        /// </summary>
        /// <param name="armies">Armies to select</param>
        /// <remarks>
        /// Selecting an army moves its state on its Tile from Armies to VisitingArmies.
        /// </remarks>
        public void SelectArmies(List<Army> armies)
        {
            if (armies is null)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            if (GameState != GameState.Ready &&
                GameState != GameState.SelectedArmy)
            {
                return;
            }

            if (!armies.TrueForAll(army => GetCurrentPlayer().Clan.ShortName == army.Player.Clan.ShortName))
            {
                throw new InvalidOperationException("Only the current player can select an army.");
            }

            if (ArmiesSelected())
            {
                DeselectArmies();
            }            

            Tile tile = armies[0].Tile;
            if (tile.HasVisitingArmies())
            {
                throw new InvalidOperationException(
                    $"Tile already has visiting armies: {ArmiesToString(tile.VisitingArmies)}");
            }

            // Move selected armies to Visiting Armies
            Log.WriteLine(Log.TraceLevel.Information, $"Selecting army: {ArmiesToString(armies)}");
            tile.VisitingArmies = new List<Army>(armies);
            tile.VisitingArmies.Sort(new ByArmyViewingOrder());
            foreach (Army army in tile.VisitingArmies)
            {
                tile.Armies.Remove(army);
            }

            // Clean up tile's unselected armies
            if (tile.HasArmies())
            {
                tile.Armies.Sort(new ByArmyViewingOrder());
            }
            else
            {
                tile.Armies = null;
            }

            SelectArmiesInternal(tile.VisitingArmies);            
            Transition(GameState.SelectedArmy);
        }

        internal void SelectArmiesInternal(List<Army> armies)
        {
            this.selectedArmies = new List<Army>(armies);
            this.selectedArmies.ForEach(a => a.IsDefending = false);
            this.quitArmySet.Remove(armies[0].Tile);
        }

        /// <summary>
        /// Removes the given armies from currently selected armies
        /// </summary>
        /// <param name="armies">Armies to remove</param>
        public void RemoveSelectedArmies(List<Army> armies)
        {
            if (!ArmiesSelected())
            {
                return;
            }

            // Commit armies to tile
            armies[0].Tile.RemoveVisitingArmies(armies);

            // Remove from selected armies
            armies.ForEach(a => selectedArmies.Remove(a));
        }

        /// <summary>
        /// Test if there are any armies currently selected.
        /// </summary>
        /// <returns>True if armies are selected; otherwise False</returns>
        public bool ArmiesSelected()
        {
            return (GameState == GameState.SelectedArmy || 
                    GameState == GameState.MovingArmy ||
                    GameState == GameState.AttackingArmy) &&
                   (selectedArmies != null) &&
                   (selectedArmies.Count > 0);
        }

        /// <summary>
        /// Deselect the selected armies.
        /// </summary>
        public void DeselectArmies()
        {
            if (!ArmiesSelected())
            {
                return;
            }

            // Simplify deselect for attack scenarios
            var armiesToDeselect = RemoveDeadArmies(this.selectedArmies);

            // Deselect
            armiesToDeselect[0].Tile.CommitVisitingArmies();
            this.selectedArmies = null;
            Transition(GameState.Ready);
        }

        /// <summary>
        /// Set the armies into defensive sentry mode
        /// </summary>
        public void DefendSelectedArmies()
        {
            if (!ArmiesSelected())
            {
                return;
            }
            
            this.selectedArmies.ForEach(a => a.Defend());
            DeselectArmies();
        }

        /// <summary>
        /// "Quit" selecting these armies for one turn
        /// </summary>
        public void QuitSelectedArmies()
        {
            if (!ArmiesSelected())
            {
                return;
            }

            Tile tileWithArmiesToQuit = this.selectedArmies[0].Tile;
            this.quitArmySet.Add(tileWithArmiesToQuit);
            this.nextArmyQueue.Remove(tileWithArmiesToQuit);

            DeselectArmies();
        }

        /// <summary>
        /// Create test-only default players for the game
        /// </summary>
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

            Game.Current.CurrentPlayerIndex = 0;
        }

        /// <summary>
        /// Create test-only defaults for the game
        /// </summary>
        public static void CreateDefaultGame()
        {
            CreateDefaultGame(ModFactory.WorldPath);
        }

        /// <summary>
        /// /// Create test-only defaults for the game
        /// </summary>
        /// <param name="worldName">World to use</param>
        public static void CreateDefaultGame(string worldName)
        {
            current = new Game();
            current.RandomSeed = Game.DefaultRandomSeed;
            current.Random = new Random(Game.DefaultRandomSeed);
            current.WarStrategy = new DefaultWarStrategy();

            // Setup default players for testing
            current.Players = new List<Player>();
            CreateDefaultPlayers();

            World.CreateWorld(worldName);
        }

        /// <summary>
        /// Create an empty game
        /// </summary>
        public static void CreateEmpty()
        {
            current = new Game();
        }

        #region Helper methods

        /// <summary>
        /// Getes a list of tiles that have armies that can be moved
        /// </summary>
        /// <param name="player">Current player</param>
        /// <returns>List of tiles</returns>
        /// <remarks>
        /// Armies that have been marked to "defend" or "quit" will 
        /// be skipped.
        /// </remarks>
        private List<Tile> GetTilesWithArmiesWithMoves(Player player)
        {
            var tiles = new HashSet<Tile>();
            var armies = player.GetArmies();

            foreach (var army in armies)
            {
                if (!army.IsDefending && 
                    (army.MovesRemaining > 0) &&
                    !this.quitArmySet.Contains(army.Tile))
                {
                    tiles.Add(army.Tile);
                }
            }

            return new List<Tile>(tiles);
        }
        private static List<Army> RemoveDeadArmies(List<Army> armies)
        {
            var armiesToReturn = new List<Army>(armies);
            foreach (Army army in armies)
            {
                if (!army.IsDead)
                {
                    armiesToReturn.Add(army);
                }
            }

            return armiesToReturn;
        }

        private static string ArmiesToString(List<Army> armies)
        {
            return $"Armies[{armies.Count}:{armies[0]}]";
        }

        private void HandleGameOver()
        {
            Transition(GameState.GameOver);
        }

        #endregion
    }
}
