using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Scenario;

[TestFixture]
public class SavedGameScenarioTests
{
    /// <summary>
    ///     Scenario: Multiple armies moving and attacking independently and regrouping
    ///     to attack as one across a series of turns.
    ///     1. Save the game after initial setup
    ///     2. Complete the scenario
    ///     3. Load the game
    ///     4. Replay the scenario end-to-end
    /// </summary>
    [Test]
    public void SaveLoad_TwoClanFourHeroDual_SameOutcome()
    {
        // Assemble
        var armyController = TestUtilities.CreateArmyController();
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();

        // Scenario setup
        //  =========================================================================================
        // (0, 0):[M,]     (1, 0):[M,]     (2, 0):[M,]     (3, 0):[M,]     (4, 0):[M,]     (5, 0):[M,]
        // (0, 1):[M,]     (1, 1):[S,H]    (2, 1):[G,]     (3, 1):[G,]     (4, 1):[G,]     (5, 1):[M,]
        // (0, 2):[M,]     (1, 2):[S,H]    (2, 2):[G,]     (3, 2):[G,]     (4, 2):[G,]     (5, 2):[M,]
        // (0, 3):[M,]     (1, 3):[G,]     (2, 3):[G,]     (3, 3):[G,]     (4, 3):[L,H]    (5, 3):[M,]
        // (0, 4):[M,]     (1, 4):[G,]     (2, 4):[G,]     (3, 4):[G,]     (4, 4):[L,H]    (5, 4):[M,]
        // (0, 5):[M,]     (1, 5):[M,]     (2, 5):[M,]     (3, 5):[M,]     (4, 5):[M,]     (5, 5):[M,]
        //  =========================================================================================            
        Game.CreateDefaultGame();
        Game.Current.IgnoreGameOver = true;

        // Initial Sirians setup
        var sirians = Game.Current.Players[0];
        var tile1 = World.Current.Map[1, 1];
        var tile2 = World.Current.Map[1, 2];
        sirians.HireHero(tile1);
        sirians.HireHero(tile2);
        var siriansHero1 = new List<Army>(tile1.Armies);
        var siriansHero2 = new List<Army>(tile2.Armies);

        // Initial Lord Bane setup
        var lordBane = Game.Current.Players[1];
        var tile3 = World.Current.Map[4, 3];
        var tile4 = World.Current.Map[4, 4];
        lordBane.HireHero(tile3);
        lordBane.HireHero(tile4);
        var lordBaneHero1 = new List<Army>(tile3.Armies);
        var lordBaneHero2 = new List<Army>(tile4.Armies);

        var snapshot = Game.Current.Snapshot();
        var iteration = 0;

        // Act
        while (iteration < 2)
        {
            if (iteration++ == 1)
            {
                // Load the game from saved snapshot on second run
                _ = GameFactory.Load(snapshot);
                Game.Current.IgnoreGameOver = true;

                // Refresh references
                sirians = Game.Current.Players[0];
                tile1 = World.Current.Map[1, 1];
                tile2 = World.Current.Map[1, 2];
                siriansHero1 = new List<Army>(tile1.Armies);
                siriansHero2 = new List<Army>(tile2.Armies);

                lordBane = Game.Current.Players[1];
                tile3 = World.Current.Map[4, 3];
                tile4 = World.Current.Map[4, 4];
                lordBaneHero1 = new List<Army>(tile3.Armies);
                lordBaneHero2 = new List<Army>(tile4.Armies);
            }

            // Turn 1: Sirians
            //  =========================================================================================
            // (0, 0):[M,]     (1, 0):[M,]     (2, 0):[M,]     (3, 0):[M,]     (4, 0):[M,]     (5, 0):[M,]
            // (0, 1):[M,]     (1, 1):[G,]     (2, 1):[G,]     (3, 1):[G,]     (4, 1):[G,]     (5, 1):[M,]
            // (0, 2):[M,]     (1, 2):[G,]     (2, 2):[G,]     (3, 2):[G,]     (4, 2):[S,H]    (5, 2):[M,]
            // (0, 3):[M,]     (1, 3):[G,]     (2, 3):[G,]     (3, 3):[S,H]    (4, 3):[L,H]    (5, 3):[M,]
            // (0, 4):[M,]     (1, 4):[G,]     (2, 4):[G,]     (3, 4):[G,]     (4, 4):[L,H]    (5, 4):[M,]
            // (0, 5):[M,]     (1, 5):[M,]     (2, 5):[M,]     (3, 5):[M,]     (4, 5):[M,]     (5, 5):[M,]
            //  =========================================================================================  
            TestUtilities.Select(commandController, armyController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                siriansHero1, 4, 2);
            TestUtilities.Deselect(commandController, armyController,
                siriansHero1);

            TestUtilities.Select(commandController, armyController,
                siriansHero2);
            TestUtilities.MoveUntilDone(commandController, armyController,
                siriansHero2, 3, 3);
            TestUtilities.Deselect(commandController, armyController,
                siriansHero2);

            TestUtilities.EndTurn(commandController, gameController);

            // Turn 1: Lord Bane
            //  =========================================================================================
            // (0, 0):[M,]     (1, 0):[M,]     (2, 0):[M,]     (3, 0):[M,]     (4, 0):[M,]     (5, 0):[M,]
            // (0, 1):[M,]     (1, 1):[L,H]    (2, 1):[G,]     (3, 1):[G,]     (4, 1):[G,]     (5, 1):[M,]
            // (0, 2):[M,]     (1, 2):[G,]     (2, 2):[G,]     (3, 2):[G,]     (4, 2):[S,H]    (5, 2):[M,]
            // (0, 3):[M,]     (1, 3):[G,]     (2, 3):[G,]     (3, 3):[L,H]    (4, 3):[G,]     (5, 3):[M,]
            // (0, 4):[M,]     (1, 4):[G,]     (2, 4):[G,]     (3, 4):[G,]     (4, 4):[G,]     (5, 4):[M,]
            // (0, 5):[M,]     (1, 5):[M,]     (2, 5):[M,]     (3, 5):[M,]     (4, 5):[M,]     (5, 5):[M,]
            //  =========================================================================================  
            TestUtilities.StartTurn(commandController, gameController);

            // Attack and lose
            TestUtilities.Select(commandController, armyController,
                lordBaneHero1);
            TestUtilities.AttackUntilDone(commandController, armyController,
                lordBaneHero1, 3, 3);

            // Run away
            TestUtilities.Select(commandController, armyController,
                lordBaneHero2);
            TestUtilities.MoveUntilDone(commandController, armyController,
                lordBaneHero2, 1, 4);
            TestUtilities.MoveUntilDone(commandController, armyController,
                lordBaneHero2, 1, 1);
            TestUtilities.Deselect(commandController, armyController,
                lordBaneHero2);

            TestUtilities.EndTurn(commandController, gameController);

            // Turn 2: Sirians
            //  =========================================================================================
            // (0, 0):[M,]     (1, 0):[M,]     (2, 0):[M,]     (3, 0):[M,]     (4, 0):[M,]     (5, 0):[M,]
            // (0, 1):[M,]     (1, 1):[L,H]    (2, 1):[G,]     (3, 1):[G,]     (4, 1):[G,]     (5, 1):[M,]
            // (0, 2):[M,]     (1, 2):[G,]     (2, 2):[G,]     (3, 2):[G,]     (4, 2):[S,H]    (5, 2):[M,]
            // (0, 3):[M,]     (1, 3):[G,]     (2, 3):[G,]     (3, 3):[S,H]    (4, 3):[G,]     (5, 3):[M,]
            // (0, 4):[M,]     (1, 4):[G,]     (2, 4):[G,]     (3, 4):[G,]     (4, 4):[G,]     (5, 4):[M,]
            // (0, 5):[M,]     (1, 5):[M,]     (2, 5):[M,]     (3, 5):[M,]     (4, 5):[M,]     (5, 5):[M,]
            //  =========================================================================================   
            TestUtilities.StartTurn(commandController, gameController);

            // Chase Lord Bane!
            TestUtilities.Select(commandController, armyController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                Game.Current.GetSelectedArmies(), 2, 2);
            TestUtilities.Deselect(commandController, armyController,
                Game.Current.GetSelectedArmies());

            TestUtilities.Select(commandController, armyController,
                siriansHero2);
            TestUtilities.MoveUntilDone(commandController, armyController,
                Game.Current.GetSelectedArmies(), 2, 2);
            TestUtilities.Deselect(commandController, armyController,
                Game.Current.GetSelectedArmies());

            // Attack with dual heros!            
            TestUtilities.Select(commandController, armyController,
                World.Current.Map[2, 2].Armies);
            TestUtilities.AttackUntilDone(commandController, armyController,
                Game.Current.GetSelectedArmies(), 1, 1);

            TestUtilities.EndTurn(commandController, gameController);

            // Assert
            Assert.That(lordBane.GetArmies().Count, Is.EqualTo(0), "Lord Bane is not yet defeated!");
            Assert.That(sirians.GetArmies().Count, Is.EqualTo(2), "Sirians army took more losses than expected!");
            Assert.That(sirians.Turn, Is.EqualTo(3), "Unexpected player's turn");
            Assert.That(Game.Current.GetCurrentPlayer().Clan.ShortName, Is.EqualTo("LordBane"), "Unexpected player's turn");
        }
    }

    /// <summary>
    ///     Scenario: Investing 101: Real estate! Gather money over a set of turns.
    /// </summary>
    [Test]
    public void SaveLoad_MrMoneybags_SameOutcome()
    {
        // Assemble
        var cityController = TestUtilities.CreateCityController();
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();

        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        MapBuilder.AddCitiesFromWorldPath(World.Current, TestUtilities.DefaultTestWorld);
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];
        const int startingGold = 432;
        sirians.Gold = startingGold;
        lordBane.Gold = startingGold;

        var snapshot = Game.Current.Snapshot();
        var iteration = 0;

        // Act
        while (iteration < 2)
        {
            if (iteration++ == 1)
            {
                // Load the game from saved snapshot on second run
                _ = GameFactory.Load(snapshot);
                Game.Current.IgnoreGameOver = true;

                // Refresh references
                sirians = Game.Current.Players[0];
                lordBane = Game.Current.Players[1];
            }

            // Act
            // Starting with 432 gp
            // +216 gp for pillaging Lord Bane (50% of 432)
            cityController.ClaimCity(lordBane.GetCities()[0], sirians);

            // Do nothing; earn money!
            for (var i = 0; i < 100; i++)
            {
                // Player 1
                // +(30gp + 32gp) = +$62gp per turn
                TestUtilities.EndTurn(commandController, gameController);
                TestUtilities.StartTurn(commandController, gameController);

                // Player 2
                TestUtilities.EndTurn(commandController, gameController);
                TestUtilities.StartTurn(commandController, gameController);
            }

            // Assert
            Assert.That(sirians.Gold, Is.EqualTo(startingGold + 216 + 100 * 62), "Sirians have a sketchy accountant");
            Assert.That(lordBane.Gold, Is.EqualTo(0), "Lord Bane has a great accountant.");
        }
    }


    /// <summary>
    ///     Scenario: Take the city!
    ///     1. Save at after setup
    ///     2. Run the scenario
    ///     3. Load and rerun
    ///     Start state:
    ///     ============================================
    ///     00^   10^     20^     30^     40^     50^
    ///     01^   11.H8   21^     31^     41.     51^
    ///     02^   12.     22$     32$i1   42.     52^
    ///     03^   13.     23$i1   33$i1   43.     53^
    ///     04^   14.     24.     34.     44.     54^
    ///     05^   15^     25^     35^     45^     55^
    ///     ============================================
    ///     End State:
    ///     ============================================
    ///     00^   10^     20^     30^     40^     50^
    ///     01^   11.     21^     31^     41.     51^
    ///     02^   12.     22$H8   32$     42.     52^
    ///     03^   13.     23$     33$     43.     53^
    ///     04^   14.     24.     34.     44.     54^
    ///     05^   15^     25^     35^     45^     55^
    ///     ============================================
    ///     Legend: 1:X, 2:Y, 3:Terrain, 4:Army 5:ArmyCount
    /// </summary>
    [Test]
    public void Attack_CityWithArmies_Succcess()
    {
        // Assemble
        var armyController = TestUtilities.CreateArmyController();
        var commandController = TestUtilities.CreateCommandController();

        Game.CreateDefaultGame();
        Game.Current.IgnoreGameOver = true;

        // Initial Sirians setup
        var sirians = Game.Current.Players[0];
        var tile1 = World.Current.Map[1, 1];
        var siriansHero1 = new List<Army>
        {
            sirians.HireHero(tile1),
            sirians.HireHero(tile1),
            sirians.HireHero(tile1),
            sirians.HireHero(tile1),
            sirians.HireHero(tile1),
            sirians.HireHero(tile1),
            sirians.HireHero(tile1),
            sirians.HireHero(tile1)
        };

        // Initial Bane's setup
        const int x = 2;
        const int y = 2;
        var bane = Game.Current.Players[1];
        var tile2 = World.Current.Map[x, y - 1];
        var tile3 = World.Current.Map[x + 1, y];
        var tile4 = World.Current.Map[x + 1, y - 1];
        bane.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile2);
        bane.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile3);
        bane.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile4);

        // Add city owned by Lord Bane full of troops (but not in (2,2))
        MapBuilder.AddCity(World.Current, 2, 2, "BanesCitadel", "LordBane");

        var snapshot = Game.Current.Snapshot();
        var iteration = 0;

        // Act
        while (iteration < 2)
        {
            if (iteration++ == 1)
            {
                // Load the game from saved snapshot on second run
                _ = GameFactory.Load(snapshot);
                Game.Current.IgnoreGameOver = true;

                // Refresh references
                sirians = Game.Current.Players[0];
                siriansHero1 = new List<Army>(World.Current.Map[1, 1].Armies);

                bane = Game.Current.Players[1];
            }

            // Act
            TestUtilities.Select(commandController, armyController,
                siriansHero1);
            TestUtilities.AttackUntilDone(commandController, armyController,
                siriansHero1, 2, 2);

            // Assert
            Assert.That(sirians.GetArmies().Count, Is.EqualTo(8), "More heros fell than expected.");
            Assert.That(bane.GetArmies().Count, Is.EqualTo(0), "Unexpected number of armies remaining.");
            Assert.That(sirians.GetCities().Count, Is.EqualTo(1), "Unexpected number of cities.");
            Assert.That(bane.GetCities().Count, Is.EqualTo(0), "Unexpected number of cities.");
            Assert.That(siriansHero1[0].X, Is.EqualTo(2), "Army didn't actually move into the city.");
            Assert.That(siriansHero1[0].Y, Is.EqualTo(2), "Army didn't actually move into the city.");
            Assert.That(World.Current.Map[2, 2].City.Clan, Is.EqualTo(sirians.Clan),
                "Sirian's couldn't take the city.");
        }
    }

    /// <summary>
    ///     Hero and his minstrels will search every last ruin, tomb, temple, and sage!
    ///     Who needs to read?
    ///     1. Save at after setup
    ///     2. Run the scenario
    ///     3. Load and rerun
    ///     Start state:
    ///     ==========================================
    ///     05^     15^     25^     35^     45^     55^
    ///     04^     14+     24+     34+     44+     54^
    ///     03^     13=     23¥     33.     43.     53^
    ///     02^     12$     22$     32.     42&     52^
    ///     01^     11$H3   21$     31.     41L     51^
    ///     00^     10^     20^     30^     40^     50^
    ///     ==========================================
    ///     End state:
    ///     ==========================================
    ///     05^     15^     25^     35^     45^     55^
    ///     04^     14+     24+     34+     44+     54^
    ///     03^     13=     23¥H3   33.     43.     53^
    ///     02^     12$     22$     32.     42&     52^
    ///     01^     11$     21$     31.     41L     51^
    ///     00^     10^     20^     30^     40^     50^
    ///     ==========================================
    ///     Legend: 1:X, 2:Y, 3:Terrain, 4:Army 5:ArmyCount
    ///     = Sage, + Temple, & Tomb, ¥ Ruins, L Library
    /// </summary>
    [Test]
    public void SaveLoad_HeroOnAQuest_SameOutcome()
    {
        // Assemble
        var armyController = TestUtilities.CreateArmyController();
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();
        var locationController = TestUtilities.CreateLocationController();
        var heroController = TestUtilities.CreateHeroController();

        // Scenario setup
        // ==========================================
        // 05^     15^     25^     35^     45^     55^
        // 04^     14+     24+     34+     44+     54^
        // 03^     13=     23¥     33.     43.     53^
        // 02^     12$     22$     32.     42&     52^
        // 01^     11$H3   21$     31.     41L     51^
        // 00^     10^     20^     30^     40^     50^
        // ==========================================           
        Game.CreateDefaultGame("SearchWorld");
        Game.Current.IgnoreGameOver = true;

        // Initial Sirians setup
        var sirians = Game.Current.Players[0];
        var tile1 = World.Current.Map[1, 1];
        sirians.HireHero(tile1);
        sirians.ConscriptArmy(ModFactory.FindArmyInfo("Griffins"), tile1);
        sirians.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile1);
        var siriansHero1 = new List<Army>(tile1.Armies);

        // Add cities and locations
        MapBuilder.AddCitiesFromWorldPath(World.Current, World.Current.Name);
        MapBuilder.AddLocationsFromWorldPath(World.Current, World.Current.Name);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        var snapshot = Game.Current.Snapshot();
        var iteration = 0;

        // Act
        while (iteration < 2)
        {
            if (iteration++ == 1)
            {
                // Load the game from saved snapshot on second run
                _ = GameFactory.Load(snapshot);
                Game.Current.IgnoreGameOver = true;

                // Refresh references
                sirians = Game.Current.Players[0];
                siriansHero1 = sirians.GetArmies()[0].Tile.GetAllArmies();
            }

            // Turn 1: Sirians: Let's get blessed!
            TestUtilities.Select(commandController, armyController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                siriansHero1, 1, 4);
            TestUtilities.SearchTemple(commandController, locationController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                siriansHero1, 2, 4);
            TestUtilities.SearchTemple(commandController, locationController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                siriansHero1, 3, 4);
            TestUtilities.SearchTemple(commandController, locationController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                siriansHero1, 4, 4);
            TestUtilities.SearchTemple(commandController, locationController,
                siriansHero1);
            TestUtilities.Deselect(commandController, armyController,
                siriansHero1);

            TestUtilities.EndTurn(commandController, gameController);

            // Turn 1: Lord Bane: Skip 
            TestUtilities.StartTurn(commandController, gameController);
            TestUtilities.EndTurn(commandController, gameController);

            // Turn 2: Sirians: Search the tomb!
            // ==========================================
            // 05^     15^     25^     35^     45^     55^
            // 04^     14x     24x     34x     44xH3   54^
            // 03^     13=     23¥     33.     43.     53^
            // 02^     12$     22$     32.     42&     52^
            // 01^     11$     21$     31.     41L     51^
            // 00^     10^     20^     30^     40^     50^
            // ========================================== 
            // Legend: x = Searched
            TestUtilities.StartTurn(commandController, gameController);

            TestUtilities.Select(commandController, armyController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                Game.Current.GetSelectedArmies(), 4, 2);
            TestUtilities.SearchRuins(commandController, locationController,
                siriansHero1);
            TestUtilities.Deselect(commandController, armyController,
                Game.Current.GetSelectedArmies());

            // Pick up the staff of might (found in tomb)
            TestUtilities.TakeItems(commandController, heroController,
                (Hero)siriansHero1[0]);

            TestUtilities.EndTurn(commandController, gameController);

            // Turn 2: Lord Bane: Skip 
            TestUtilities.StartTurn(commandController, gameController);
            TestUtilities.EndTurn(commandController, gameController);

            // Turn 3: Sirians: Search the library!
            // ==========================================
            // 05^     15^     25^     35^     45^     55^
            // 04^     14x     24x     34x     44x     54^
            // 03^     13=     23¥     33.     43.     53^
            // 02^     12$     22$     32.     42&H3   52^
            // 01^     11$     21$     31.     41L     51^
            // 00^     10^     20^     30^     40^     50^
            // ========================================== 
            // Legend: x = Searched  
            TestUtilities.StartTurn(commandController, gameController);

            TestUtilities.Select(commandController, armyController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                Game.Current.GetSelectedArmies(), 4, 1);
            TestUtilities.SearchLibrary(commandController, locationController,
                siriansHero1);
            TestUtilities.SearchLibrary(commandController, locationController,
                siriansHero1);
            TestUtilities.SearchLibrary(commandController, locationController,
                siriansHero1);
            TestUtilities.Deselect(commandController, armyController,
                Game.Current.GetSelectedArmies());

            TestUtilities.EndTurn(commandController, gameController);

            // Turn 3: Lord Bane: Skip 
            TestUtilities.StartTurn(commandController, gameController);
            TestUtilities.EndTurn(commandController, gameController);

            // Turn 4: Sirians: Search the ruins!
            // ==========================================
            // 05^     15^     25^     35^     45^     55^
            // 04^     14x     24x     34x     44x     54^
            // 03^     13=     23¥     33.     43.     53^
            // 02^     12$     22$     32.     42&     52^
            // 01^     11$     21$     31.     41LH3   51^
            // 00^     10^     20^     30^     40^     50^
            // ========================================== 
            // Legend: x = Searched  
            TestUtilities.StartTurn(commandController, gameController);

            TestUtilities.Select(commandController, armyController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                Game.Current.GetSelectedArmies(), 2, 3);
            TestUtilities.SearchRuins(commandController, locationController,
                siriansHero1);
            TestUtilities.Deselect(commandController, armyController,
                Game.Current.GetSelectedArmies());

            TestUtilities.EndTurn(commandController, gameController);

            // Turn 4: Lord Bane: Skip 
            TestUtilities.StartTurn(commandController, gameController);
            TestUtilities.EndTurn(commandController, gameController);

            // Turn 5: Sirians: Search the sage!
            // ==========================================
            // 05^     15^     25^     35^     45^     55^
            // 04^     14x     24x     34x     44x     54^
            // 03^     13=     23¥H3   33.     43.     53^
            // 02^     12$     22$     32.     42&     52^
            // 01^     11$     21$     31.     41L     51^
            // 00^     10^     20^     30^     40^     50^
            // ========================================== 
            // Legend: x = Searched  
            TestUtilities.StartTurn(commandController, gameController);

            TestUtilities.Select(commandController, armyController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                Game.Current.GetSelectedArmies(), 1, 3);
            TestUtilities.SearchSage(commandController, locationController,
                siriansHero1);
            TestUtilities.Deselect(commandController, armyController,
                Game.Current.GetSelectedArmies());

            TestUtilities.EndTurn(commandController, gameController);

            // Assert
            Assert.IsTrue(((Hero)siriansHero1[0]).HasItems(), "Hero had a hole in his pocket!");
            Assert.That(((Hero)siriansHero1[0]).Items[0].ShortName, Is.EqualTo("CrownOfLoriel"),
                "Hero had a hole in his pocket!");
            Assert.That(siriansHero1[0].Strength, Is.EqualTo(9), "Hero was not pias enough!");
            Assert.That(siriansHero1[1].Strength, Is.EqualTo(9), "Griffins were not pias enough!");
            Assert.That(siriansHero1[2].Strength, Is.EqualTo(7), "Light infantry was not pias enough!");
            Assert.IsTrue(Game.Current.Players[0].Gold > 1000, "Sage was super cheap!");
            Assert.That(sirians.Turn, Is.EqualTo(6), "Unexpected player's turn");
            Assert.That(Game.Current.GetCurrentPlayer().Clan.ShortName, Is.EqualTo("LordBane"), "Unexpected player's turn");
        }
    }
}