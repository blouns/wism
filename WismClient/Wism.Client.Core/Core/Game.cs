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

        private int currentPlayerIndex;
        private GameState gameState;
        private List<Army> selectedArmies;

        public World World { get; set; }

        public List<Player> Players { get; set; }

        public Random Random { get; set; }

        public IWarStrategy WarStrategy { get; set; }

        public GameState GameState { get => gameState; }

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
        public void EndTurn()
        {
            // End current players turn
            DeselectArmies();
            var player = GetCurrentPlayer(); 
            player.EndTurn();            

            // Set next players turn
            currentPlayerIndex = (currentPlayerIndex + 1) % Players.Count;

            Transition(GameState.StartingTurn);
        }

        /// <summary>
        /// Process production, new heros, evaluate if player is alive, etc.
        /// </summary>
        public void StartTurn()
        {
            // TODO: New heros, evaluate if player is alive, etc.
            var player = GetCurrentPlayer();
            player.StartTurn();
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

            var player = GetCurrentPlayer();
            var armies = player.GetArmies();
            foreach (Army army in armies)
            {
                // Exclude Defending and out-of-moves armies
                if (!army.IsDefending && army.MovesRemaining > 0)
                {
                    if (ArmiesSelected())
                    {
                        // Exclude currently selected armies
                        if (this.selectedArmies.Contains(army))
                        {
                            continue;
                        }

                        DeselectArmies();
                    }

                    SelectArmies(army.Tile.Armies);
                    return true;
                }
            }

            return false;
        }

        public List<Army> GetSelectedArmies()
        {
            if (!ArmiesSelected())
            {
                return null;
            }

            // return a copy of the list
            return new List<Army>(selectedArmies);
        }

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

            if (!armies.TrueForAll(army => GetCurrentPlayer() == army.Player))
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

            this.selectedArmies = new List<Army>(tile.VisitingArmies);
            this.selectedArmies.ForEach(a => a.IsDefending = false);
            
            Transition(GameState.SelectedArmy);
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

        public bool ArmiesSelected()
        {
            return (GameState == GameState.SelectedArmy) &&
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

        #region Helper methods

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

        #endregion
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
