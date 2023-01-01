using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class LibrarianTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void Setup()
    {
        Game.CreateDefaultGame(this.WorldName);
    }

    private readonly string WorldName = TestUtilities.DefaultTestWorld;

    [Test]
    public void GetLocations_Three_RuinsTombLibrary()
    {
        // Assemble
        var map = World.Current.Map;
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, map[1, 1]);
        location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, map[1, 2]);
        location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, map[1, 3]);
        MapBuilder.AllocateBoons(World.Current.GetLocations());
        var librarian = new Librarian();

        // Act
        var locationNames = librarian.GetAllLocationNames();

        // Assert
        Assert.IsNotNull(locationNames);
        Assert.IsTrue(locationNames.Length > 0);
        TestContext.WriteLine("Locations:");
        foreach (var name in locationNames)
        {
            TestContext.WriteLine(name);
            Assert.IsTrue(
                "Crypt Keeper's Lair" == name ||
                "Suzzallo" == name ||
                "Stonehenge" == name);
        }
    }

    [Test]
    public void GetArtifacts_OneArtifact_OnlyLocationsTiles()
    {
        // Assemble
        var map = World.Current.Map;
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, map[1, 1]);
        location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, map[1, 2]);
        location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, map[1, 3]);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        var librarian = new Librarian();

        // Act
        var artifactNames = librarian.GetAllArtifactNames();

        // Assert
        Assert.IsNotNull(artifactNames);
        Assert.IsTrue(artifactNames.Length > 0);
        TestContext.WriteLine("Artifacts:");
        var found = false;
        foreach (var name in artifactNames)
        {
            TestContext.WriteLine(name);
            if (name == "Crown of Loriel")
            {
                found = true;
            }
        }

        Assert.IsTrue(found, "Did not find the artifact");
    }

    [Test]
    public void GetArtifacts_OneArtifact_OnlyHerosTiles()
    {
        // Assemble
        var map = World.Current.Map;
        var player1 = Game.Current.Players[0];
        var hero1 = player1.HireHero(map[1, 1]);
        _ = player1.HireHero(map[1, 2]);
        _ = player1.HireHero(map[1, 3]);
        var player2 = Game.Current.Players[1];
        _ = player2.HireHero(map[2, 2]);
        var artifact = new Artifact(
            ModFactory.FindArtifactInfo("Firesword"));
        artifact.DisplayName = "Firesword";
        hero1.Items = new List<Artifact>();
        hero1.Items.Add(artifact);
        var librarian = new Librarian();

        // Act
        var artifactNames = librarian.GetAllArtifactNames();

        // Assert
        Assert.IsNotNull(artifactNames);
        Assert.IsTrue(artifactNames.Length > 0);
        TestContext.WriteLine("Artifacts:");
        var found = false;
        foreach (var name in artifactNames)
        {
            TestContext.WriteLine(name);
            if (name == "Firesword")
            {
                found = true;
            }
        }

        Assert.IsTrue(found, "Did not find the artifact");
    }

    [Test]
    public void GetArtifacts_OneArtifact_OnlyTiles()
    {
        // Assemble
        var map = World.Current.Map;
        var player1 = Game.Current.Players[0];
        var artifact = new Artifact(
            ModFactory.FindArtifactInfo("Firesword"));
        artifact.DisplayName = "Firesword";
        map[1, 1].AddItem(artifact);
        var librarian = new Librarian();

        // Act
        var artifactNames = librarian.GetAllArtifactNames();

        // Assert
        Assert.IsNotNull(artifactNames);
        Assert.IsTrue(artifactNames.Length > 0);
        TestContext.WriteLine("Artifacts:");
        var found = false;
        foreach (var name in artifactNames)
        {
            TestContext.WriteLine(name);
            if (name == "Firesword")
            {
                found = true;
            }
        }

        Assert.IsTrue(found, "Did not find the artifact");
    }

    [Test]
    public void GetArtifacts_OneArtifactInLocationsHerosTiles_Hero()
    {
        // Assemble
        var map = World.Current.Map;

        // Locations
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, map[1, 1]);
        location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, map[1, 2]);
        location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, map[1, 3]);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        // Heros
        var player1 = Game.Current.Players[0];
        var hero1 = player1.HireHero(map[1, 1]);
        _ = player1.HireHero(map[1, 2]);
        _ = player1.HireHero(map[1, 3]);
        var player2 = Game.Current.Players[1];
        _ = player2.HireHero(map[2, 2]);
        var artifact = new Artifact(
            ModFactory.FindArtifactInfo("Firesword"));
        artifact.DisplayName = "Firesword";
        hero1.Items = new List<Artifact>();
        hero1.Items.Add(artifact);

        // Tiles
        artifact = new Artifact(
            ModFactory.FindArtifactInfo("Icesword"));
        artifact.DisplayName = "Icesword";
        map[4, 4].AddItem(artifact);

        var librarian = new Librarian();

        // Act
        var artifactNames = librarian.GetAllArtifactNames();

        // Assert
        Assert.IsNotNull(artifactNames);
        Assert.IsTrue(artifactNames.Length > 0);
        TestContext.WriteLine("Artifacts:");
        var found = false;
        foreach (var name in artifactNames)
        {
            TestContext.WriteLine(name);
            if (name == "Firesword" ||
                name == "Icesword" ||
                name == "Staff of Might")
            {
                found = true;
            }
        }

        Assert.IsTrue(found, "Did not find the artifact");
    }

    [Test]
    public void GetArtifacts_MultipleArtifacts_InLocationHeroTile()
    {
        // Assemble
        var map = World.Current.Map;

        // Locations
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, map[1, 1]);
        location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, map[1, 2]);
        location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, map[1, 3]);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        // Heros
        var player1 = Game.Current.Players[0];
        var hero1 = player1.HireHero(map[4, 1]);
        _ = player1.HireHero(map[1, 2]);
        _ = player1.HireHero(map[1, 3]);
        var player2 = Game.Current.Players[1];
        _ = player2.HireHero(map[2, 2]);
        var artifact = new Artifact(
            ModFactory.FindArtifactInfo("Firesword"));
        artifact.DisplayName = "Firesword";
        hero1.Items = new List<Artifact>();
        hero1.Items.Add(artifact);
        var librarian = new Librarian();

        artifact = new Artifact(
            ModFactory.FindArtifactInfo("Icesword"));
        artifact.DisplayName = "Icesword";
        map[2, 4].AddItem(artifact);

        // Act
        var artifactNames = librarian.GetAllArtifactNames();

        // Assert
        Assert.IsNotNull(artifactNames);
        Assert.IsTrue(artifactNames.Length > 0);
        TestContext.WriteLine("Artifacts:");
        var found = false;
        foreach (var name in artifactNames)
        {
            TestContext.WriteLine(name);
            if (name == "Firesword")
            {
                found = true;
            }
        }

        Assert.IsTrue(found, "Did not find the artifact");
    }

    [Test]
    public void GetRandomKnowledge_AllKinds_10Samples()
    {
        // Assemble
        var map = World.Current.Map;

        // Locations
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, map[1, 1]);
        location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, map[1, 2]);
        location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, map[1, 3]);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        // Heros
        var player1 = Game.Current.Players[0];
        var hero1 = player1.HireHero(map[4, 1]);
        _ = player1.HireHero(map[1, 2]);
        _ = player1.HireHero(map[1, 3]);
        var player2 = Game.Current.Players[1];
        _ = player2.HireHero(map[2, 2]);
        var artifact = new Artifact(
            ModFactory.FindArtifactInfo("Firesword"));
        artifact.DisplayName = "Firesword";
        hero1.Items = new List<Artifact>();
        hero1.Items.Add(artifact);

        // Tiles
        artifact = new Artifact(
            ModFactory.FindArtifactInfo("Icesword"));
        artifact.DisplayName = "Icesword";
        map[2, 4].AddItem(artifact);

        // Add cities
        var city = MapBuilder.FindCity("Marthos");
        World.Current.AddCity(city, map[2, 1]);
        city = MapBuilder.FindCity("BanesCitadel");
        World.Current.AddCity(city, map[3, 3]);

        var librarian = new Librarian();

        // Act
        var knowledge = new string[10];
        for (var i = 0; i < knowledge.Length; i++)
        {
            knowledge[i] = librarian.GetRandomKnowledge(player1);
        }

        // Assert
        for (var i = 0; i < knowledge.Length; i++)
        {
            Assert.IsNotNull(knowledge[i]);
            TestContext.WriteLine(knowledge[i]);
        }
    }

    [Test]
    public void GetRandomKnowledge_OnlyTiles5Artifacts_10Samples()
    {
        // Assemble
        var map = World.Current.Map;
        var player1 = Game.Current.Players[0];

        // Tiles
        map[0, 0].AddItem(GetArtifact("Firesword"));
        map[0, 1].AddItem(GetArtifact("Icesword"));
        map[0, 2].AddItem(GetArtifact("StaffOfMight"));
        map[0, 3].AddItem(GetArtifact("SpearOfAnk"));
        map[0, 4].AddItem(GetArtifact("RingOfPower"));

        // Add cities
        MapBuilder.AddCity(World.Current, 2, 1, "Marthos", "Sirians");
        MapBuilder.AddCity(World.Current, 3, 3, "BanesCitadel", "LordBane");

        var librarian = new Librarian();

        // Act
        var knowledge = new string[10];
        for (var i = 0; i < knowledge.Length; i++)
        {
            knowledge[i] = librarian.GetRandomKnowledge(player1);
        }

        // Assert
        for (var i = 0; i < knowledge.Length; i++)
        {
            Assert.IsNotNull(knowledge[i]);
            TestContext.WriteLine(knowledge[i]);
        }
    }

    [Test]
    public void GetArtifact_Firesword_Exists()
    {
        // Assemble
        var map = World.Current.Map;

        // Locations
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, map[1, 1]);
        location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, map[1, 2]);
        location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, map[1, 3]);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        // Heros
        var player1 = Game.Current.Players[0];
        var hero1 = player1.HireHero(map[4, 1]);
        _ = player1.HireHero(map[1, 2]);
        _ = player1.HireHero(map[1, 3]);
        var player2 = Game.Current.Players[1];
        _ = player2.HireHero(map[2, 2]);
        var artifact = new Artifact(
            ModFactory.FindArtifactInfo("Firesword"));
        artifact.DisplayName = "Firesword";
        hero1.Items = new List<Artifact>();
        hero1.Items.Add(artifact);

        // Items
        artifact = new Artifact(
            ModFactory.FindArtifactInfo("Icesword"));
        artifact.DisplayName = "Icesword";
        map[2, 4].AddItem(artifact);

        var librarian = new Librarian();

        // Act
        artifact = librarian.GetArtifact("Firesword");

        // Assert
        Assert.AreEqual("Firesword", artifact.DisplayName);
    }

    [Test]
    public void GetArtifact_SpearOfAnk_DoesNotExist()
    {
        // Assemble
        var map = World.Current.Map;

        // Locations
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, map[1, 1]);
        location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, map[1, 2]);
        location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, map[1, 3]);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        // Heros
        var player1 = Game.Current.Players[0];
        var hero1 = player1.HireHero(map[4, 1]);
        _ = player1.HireHero(map[1, 2]);
        _ = player1.HireHero(map[1, 3]);
        var player2 = Game.Current.Players[1];
        _ = player2.HireHero(map[2, 2]);
        var artifact = new Artifact(
            ModFactory.FindArtifactInfo("Firesword"));
        artifact.DisplayName = "Firesword";
        hero1.Items = new List<Artifact>();
        hero1.Items.Add(artifact);

        // Items
        artifact = new Artifact(
            ModFactory.FindArtifactInfo("Icesword"));
        artifact.DisplayName = "Icesword";
        map[2, 4].AddItem(artifact);

        var librarian = new Librarian();

        // Act
        artifact = librarian.GetArtifact("SpearOfAnk");

        // Assert
        Assert.IsNull(artifact);
    }

    [Test]
    public void GetLocation_CryptKeeper_Exists()
    {
        // Assemble
        var map = World.Current.Map;

        // Locations
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, map[1, 1]);
        location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, map[1, 2]);
        location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, map[1, 3]);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        // Heros
        var player1 = Game.Current.Players[0];
        var hero1 = player1.HireHero(map[4, 1]);
        _ = player1.HireHero(map[1, 2]);
        _ = player1.HireHero(map[1, 3]);
        var player2 = Game.Current.Players[1];
        _ = player2.HireHero(map[2, 2]);
        var artifact = new Artifact(
            ModFactory.FindArtifactInfo("Firesword"));
        artifact.DisplayName = "Firesword";
        hero1.Items = new List<Artifact>();
        hero1.Items.Add(artifact);

        // Items
        artifact = new Artifact(
            ModFactory.FindArtifactInfo("Icesword"));
        artifact.DisplayName = "Icesword";
        map[2, 4].AddItem(artifact);

        var librarian = new Librarian();

        // Act
        location = librarian.GetLocation("CryptKeeper");

        // Assert
        Assert.AreEqual("Crypt Keeper's Lair", location.DisplayName);
    }

    [Test]
    public void GetLocation_SagesHut_DoesNotExist()
    {
        // Assemble
        var map = World.Current.Map;

        // Locations
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, map[1, 1]);
        location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, map[1, 2]);
        location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, map[1, 3]);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        // Heros
        var player1 = Game.Current.Players[0];
        var hero1 = player1.HireHero(map[4, 1]);
        _ = player1.HireHero(map[1, 2]);
        _ = player1.HireHero(map[1, 3]);
        var player2 = Game.Current.Players[1];
        _ = player2.HireHero(map[2, 2]);
        var artifact = new Artifact(
            ModFactory.FindArtifactInfo("Firesword"));
        artifact.DisplayName = "Firesword";
        hero1.Items = new List<Artifact>();
        hero1.Items.Add(artifact);

        // Items
        artifact = new Artifact(
            ModFactory.FindArtifactInfo("Icesword"));
        artifact.DisplayName = "Icesword";
        map[2, 4].AddItem(artifact);

        var librarian = new Librarian();

        // Act
        location = librarian.GetLocation("SagesHut");

        // Assert
        Assert.IsNull(location);
    }

    private static Artifact GetArtifact(string shortName)
    {
        return new Artifact(
            ModFactory.FindArtifactInfo(shortName))
        {
            DisplayName = shortName
        };
    }
}