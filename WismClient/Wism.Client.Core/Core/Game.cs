using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.Comparers;
using Wism.Client.Core.Armies;
using Wism.Client.Core.Armies.MovementStrategies;
using Wism.Client.Core.Armies.TerrainTraversalStrategies;
using Wism.Client.Core.Armies.WarStrategies;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Pathing;

namespace Wism.Client.Core
{
    public class Game
    {
        // Default random seed (the year Warlords was born!)
        public const int DefaultRandomSeed = 1990;

        // Singleton instance
        private static Game current;
        private readonly HashSet<Tile> quitArmySet = new HashSet<Tile>();

        private List<Tile> nextArmyQueue = new List<Tile>();
        private List<Army> selectedArmies;

        /// <summary>
        ///     Active game players
        /// </summary>
        public List<Player> Players { get; set; }

        /// <summary>
        ///     Gets or sets the randomization generator used for the game
        /// </summary>
        /// <remarks>
        ///     IMPORTANT: *ALL* use of randomization that affects game state *MUST* use
        ///     this and only this for randomization. Otherwise, remote players will be
        ///     out of sync with the game state.
        /// </remarks>
        public Random Random { get; set; }

        /// <summary>
        ///     Random number seed
        /// </summary>
        public int RandomSeed { get; set; }

        /// <summary>
        ///     Gets or sets the strategy used for battles
        /// </summary>
        public IWarStrategy WarStrategy { get; set; }

        /// <summary>
        ///     Gets or sets the strategy used for terrain traversal
        /// </summary>
        public ITraversalStrategy TraversalStrategy { get; set; }

        /// <summary>
        ///     Gets or sets the pathing strategy for army movement
        /// </summary>
        public IPathingStrategy PathingStrategy { get; set; }

        /// <summary>
        ///     Gets or sets the seer that describes the strategies applicable movement
        /// </summary>
        public MovementStrategyCoordinator MovementCoordinator { get; set; }

        /// <summary>
        ///     Current GameState
        /// </summary>
        public GameState GameState { get; private set; }

        public bool IgnoreGameOver { get; set; }

        /// <summary>
        ///     Returns the current game instance as a Singleton
        /// </summary>
        public static Game Current
        {
            get
            {
                if (current == null)
                {
                    throw new InvalidOperationException("No current game exists.");
                }

                return current;
            }
        }

        internal int CurrentPlayerIndex { get; set; }

        /// <summary>
        ///     Test if the game has been initialized.
        /// </summary>
        /// <returns>True if so; otherwise False</returns>
        public static bool IsInitialized()
        {
            return current != null;
        }

        /// <summary>
        ///     Unload the current game
        /// </summary>
        public static void Unload()
        {
            current = null;
        }

        /// <summary>
        ///     Take a gave snapshot with all game state.
        /// </summary>
        /// <returns>Game snapshot</returns>
        /// <remarks>
        ///     Creates a graph of entities comprising the entire game state
        ///     to be persisted, saved, or shared.
        /// </remarks>
        public GameEntity Snapshot()
        {
            return GamePersistance.SnapshotGame(this);
        }

        /// <summary>
        ///     Gets all armies in the world for each player
        /// </summary>
        /// <returns></returns>
        public List<Army> GetAllArmies()
        {
            var armies = new List<Army>();
            foreach (var player in this.Players)
            {
                armies.AddRange(player.GetArmies());
            }

            return armies;
        }

        /// <summary>
        ///     Gets the current player based on their turn
        /// </summary>
        /// <returns>Player whose turn it is now</returns>
        public Player GetCurrentPlayer()
        {
            if (this.Players == null || this.Players.Count == 0)
            {
                throw new InvalidOperationException("Players have not been initialized.");
            }

            return this.Players[this.CurrentPlayerIndex];
        }

        /// <summary>
        ///     Gets the next alive player
        /// </summary>
        /// <returns>Player whose turn is next, or null if no players are alive</returns>
        public Player GetNextPlayer()
        {
            if (this.Players == null || this.Players.Count == 0)
            {
                throw new InvalidOperationException("Players have not been initialized.");
            }

            var currentPlayer = this.GetCurrentPlayer();

            // Find the next alive player
            var nextPlayer = this.Players[(this.CurrentPlayerIndex + 1) % this.Players.Count];
            while (nextPlayer.IsDead)
            {
                nextPlayer = this.Players[(this.CurrentPlayerIndex + 1) % this.Players.Count];
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
        ///     Transition to a new game state
        /// </summary>
        /// <param name="newState">State to transition to</param>
        public void Transition(GameState newState)
        {
            // For now just set it; later we can validate and manage the state machine
            this.GameState = newState;
        }

        /// <summary>
        ///     End the players turn.
        /// </summary>
        /// <remarks>
        ///     Resets moves, triggers production, and allows for other clans
        ///     to complete their turns.
        /// </remarks>
        public void EndTurn()
        {
            // End current players turn
            this.DeselectArmies();
            this.nextArmyQueue.Clear();
            this.quitArmySet.Clear();
            var player = this.GetCurrentPlayer();
            player.EndTurn();
            if (this.SelectNextPlayer())
            {
                this.Transition(GameState.StartingTurn);
            }
            else
            {
                this.HandleGameOver();
            }
        }

        /// <summary>
        ///     Select the next Player who is still in the fight.
        /// </summary>
        /// <returns>True if next player selected; False if there are no other players left</returns>
        private bool SelectNextPlayer()
        {
            var lastPlayerIndex = this.CurrentPlayerIndex;

            // Find next player that is still in the fight
            this.CurrentPlayerIndex = (this.CurrentPlayerIndex + 1) % this.Players.Count;
            this.CurrentPlayerIndex = this.Players.FindIndex(this.CurrentPlayerIndex, p => !p.IsDead);
            if (this.CurrentPlayerIndex == -1)
            {
                // Not found in first pass; look starting from beginning of list                
                this.CurrentPlayerIndex = this.Players.FindIndex(p => !p.IsDead);
            }

            // This may roll back to the original player (player wins); so return False in that case
            return lastPlayerIndex != this.CurrentPlayerIndex;
        }

        /// <summary>
        ///     Process production, new heros, evaluate if player is alive, etc.
        /// </summary>
        public void StartTurn()
        {
            var player = this.GetCurrentPlayer();

            if (player.GetCities().Count == 0 &&
                !this.IgnoreGameOver)
            {
                // You are no longer in the fight!
                player.IsDead = true;
                this.Transition(GameState.Ready);
                return;
            }

            player.StartTurn();

            // Select the next army if one is available
            if (!this.SelectNextArmy())
            {
                this.Transition(GameState.Ready);
            }
        }

        /// <summary>
        ///     Selects the next army with moves remaining
        /// </summary>
        /// <returns>True if a new army has been selected; otherwise, False</returns>
        public bool SelectNextArmy()
        {
            if (this.GameState != GameState.Ready &&
                this.GameState != GameState.SelectedArmy)
            {
                return false;
            }

            if (this.nextArmyQueue.Count == 0)
            {
                this.nextArmyQueue = this.GetTilesWithArmiesWithMoves(this.GetCurrentPlayer());

                // No more armies with moves
                if (this.nextArmyQueue.Count == 0)
                {
                    return false;
                }
            }

            Current.DeselectArmies();
            var tileWithArmies = this.nextArmyQueue[0];
            this.nextArmyQueue.RemoveAt(0);
            this.SelectArmies(tileWithArmies.Armies);

            return true;
        }

        /// <summary>
        ///     Get the current selected armies
        /// </summary>
        /// <returns>Armies that are selected</returns>
        public List<Army> GetSelectedArmies()
        {
            if (!this.ArmiesSelected())
            {
                return null;
            }

            // return a copy of the list
            return new List<Army>(this.selectedArmies);
        }

        /// <summary>
        ///     Sets the given armies as currently selected
        /// </summary>
        /// <param name="armies">Armies to select</param>
        /// <remarks>
        ///     Selecting an army moves its state on its Tile from Armies to VisitingArmies.
        /// </remarks>
        public void SelectArmies(List<Army> armies)
        {
            if (armies is null)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            if (this.GameState != GameState.Ready &&
                this.GameState != GameState.SelectedArmy)
            {
                return;
            }

            if (!armies.TrueForAll(army => this.GetCurrentPlayer().Clan.ShortName == army.Player.Clan.ShortName))
            {
                throw new InvalidOperationException("Only the current player can select an army.");
            }

            if (this.ArmiesSelected())
            {
                this.DeselectArmies();
            }

            var tile = armies[0].Tile;
            if (tile.HasVisitingArmies())
            {
                throw new InvalidOperationException(
                    $"Tile already has visiting armies: {ArmiesToString(tile.VisitingArmies)}");
            }

            // Move selected armies to Visiting Armies
            Log.WriteLine(Log.TraceLevel.Information, $"Selecting army: {ArmiesToString(armies)}");
            tile.VisitingArmies = new List<Army>(armies);
            tile.VisitingArmies.Sort(new ByArmyViewingOrder());
            foreach (var army in tile.VisitingArmies)
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

            this.SelectArmiesInternal(tile.VisitingArmies);
            this.Transition(GameState.SelectedArmy);
        }

        internal void SelectArmiesInternal(List<Army> armies)
        {
            this.selectedArmies = new List<Army>(armies);
            this.selectedArmies.ForEach(a => a.IsDefending = false);
            this.quitArmySet.Remove(armies[0].Tile);
        }

        /// <summary>
        ///     Removes the given armies from currently selected armies
        /// </summary>
        /// <param name="armies">Armies to remove</param>
        public void RemoveSelectedArmies(List<Army> armies)
        {
            if (!this.ArmiesSelected())
            {
                return;
            }

            // Commit armies to tile
            armies[0].Tile.RemoveVisitingArmies(armies);

            // Remove from selected armies
            armies.ForEach(a => this.selectedArmies.Remove(a));
        }

        /// <summary>
        ///     Test if there are any armies currently selected.
        /// </summary>
        /// <returns>True if armies are selected; otherwise False</returns>
        public bool ArmiesSelected()
        {
            return (this.GameState == GameState.SelectedArmy ||
                    this.GameState == GameState.MovingArmy ||
                    this.GameState == GameState.AttackingArmy) &&
                   this.selectedArmies != null &&
                   this.selectedArmies.Count > 0;
        }

        /// <summary>
        ///     Deselect the selected armies.
        /// </summary>
        public void DeselectArmies()
        {
            if (!this.ArmiesSelected())
            {
                return;
            }

            // Simplify deselect for attack scenarios
            var armiesToDeselect = RemoveDeadArmies(this.selectedArmies);

            // Deselect
            armiesToDeselect[0].Tile.CommitVisitingArmies();
            this.selectedArmies = null;
            this.Transition(GameState.Ready);
        }

        /// <summary>
        ///     Set the armies into defensive sentry mode
        /// </summary>
        public void DefendSelectedArmies()
        {
            if (!this.ArmiesSelected())
            {
                return;
            }

            this.selectedArmies.ForEach(a => a.Defend());
            this.DeselectArmies();
        }

        /// <summary>
        ///     "Quit" selecting these armies for one turn
        /// </summary>
        public void QuitSelectedArmies()
        {
            if (!this.ArmiesSelected())
            {
                return;
            }

            var tileWithArmiesToQuit = this.selectedArmies[0].Tile;
            this.quitArmySet.Add(tileWithArmiesToQuit);
            this.nextArmyQueue.Remove(tileWithArmiesToQuit);

            this.DeselectArmies();
        }

        /// <summary>
        ///     Create test-only default players for the game
        /// </summary>
        public static void CreateDefaultPlayers()
        {
            // Default two players for now
            var clanInfo = ClanInfo.GetClanInfo("Sirians");
            var clan = Clan.Create(clanInfo);
            var player1 = Player.Create(clan);
            Current.Players.Add(player1);

            clanInfo = ClanInfo.GetClanInfo("LordBane");
            clan = Clan.Create(clanInfo);
            var player2 = Player.Create(clan);
            Current.Players.Add(player2);

            Current.CurrentPlayerIndex = 0;
        }

        /// <summary>
        ///     Create test-only defaults for the game
        /// </summary>
        public static void CreateDefaultGame()
        {
            CreateDefaultGame(ModFactory.WorldPath);
        }

        /// <summary>
        ///     /// Create test-only defaults for the game
        /// </summary>
        /// <param name="worldName">World to use</param>
        public static void CreateDefaultGame(string worldName)
        {
            current = new Game
            {
                RandomSeed = DefaultRandomSeed,
                Random = new Random(DefaultRandomSeed),
                WarStrategy = new DefaultWarStrategy(),
                TraversalStrategy = CompositeTraversalStrategy.CreateDefault(),
                MovementCoordinator = MovementStrategyCoordinator.CreateDefault(),
                PathingStrategy = new DijkstraPathingStrategy(),
                Players = new List<Player>()
            };

            // Setup default players for testing
            CreateDefaultPlayers();

            World.CreateWorld(worldName);
        }

        /// <summary>
        ///     Create an empty game
        /// </summary>
        public static void CreateEmpty()
        {
            current = new Game();
        }

        #region Helper methods

        /// <summary>
        ///     Getes a list of tiles that have armies that can be moved
        /// </summary>
        /// <param name="player">Current player</param>
        /// <returns>List of tiles</returns>
        /// <remarks>
        ///     Armies that have been marked to "defend" or "quit" will
        ///     be skipped.
        /// </remarks>
        private List<Tile> GetTilesWithArmiesWithMoves(Player player)
        {
            var tiles = new HashSet<Tile>();
            var armies = player.GetArmies();

            foreach (var army in armies)
            {
                if (!army.IsDefending &&
                    army.MovesRemaining > 0 &&
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
            foreach (var army in armies)
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
            this.Transition(GameState.GameOver);
        }

        #endregion
    }
}