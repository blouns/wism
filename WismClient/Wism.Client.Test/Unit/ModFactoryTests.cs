using System;
using System.IO;
using System.Runtime.Serialization.Json;
using NUnit.Framework;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class ModFactoryTest
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        this.CleanupTestFiles();
    }

    public const string TestModPath = @"mod";

    private const string clanFileName = "Clan_Template.json";
    private const string unitFileName = "Army_Template.json";
    private const string terrainFileName = "Terrain_Template.json";

    [Test]
    public void WriteTemplatesTest()
    {
        this.CleanupTestFiles();

        var clanInfo = new ClanInfo();
        var unitInfo = new ArmyInfo();
        var terrainInfo = new TerrainInfo();

        SerializeType(clanFileName, clanInfo);
        SerializeType(unitFileName, unitInfo);
        SerializeType(terrainFileName, terrainInfo);

        if (!File.Exists(clanFileName) ||
            !File.Exists(unitFileName) ||
            !File.Exists(terrainFileName))
        {
            Assert.Fail("Templates not written as expected.");
        }
    }

    [Test]
    public void LoadClansTest()
    {
        var foundOrcs = false;
        var clans = ModFactory.LoadClans(TestModPath);
        foreach (var clan in clans)
        {
            TestContext.WriteLine("Clan: {0}", clan);
            if (clan.DisplayName == "Orcs of Kor")
            {
                foundOrcs = true;
                Assert.That(clan.GetTerrainModifier("Marsh"), Is.EqualTo(1));
                Assert.That(clan.GetTerrainModifier("Forest"), Is.EqualTo(-1));
                Assert.That(clan.GetTerrainModifier("Grass"), Is.EqualTo(0));
                Assert.That(clan.GetTerrainModifier("Hill"), Is.EqualTo(0));
                Assert.That(clan.GetTerrainModifier("OuterSpace"), Is.EqualTo(0));
            }
        }

        Assert.IsTrue(foundOrcs);
    }


    [Test]
    public void LoadArmysTest()
    {
        var foundHero = false;
        var units = ModFactory.LoadArmies(TestModPath);

        foreach (var unit in units)
        {
            TestContext.WriteLine("Army: {0}", unit);
            if (unit.ShortName == "Hero")
            {
                foundHero = true;
            }
        }

        Assert.IsTrue(foundHero);
    }

    [Test]
    public void LoadTerrainTest()
    {
        var foundMeadow = false;
        var terrains = ModFactory.LoadTerrains(TestModPath);
        foreach (var terrain in terrains)
        {
            TestContext.WriteLine("Terrain: {0}", terrain);
            if (terrain.DisplayName == "Grass")
            {
                foundMeadow = true;
            }
        }

        Assert.IsTrue(foundMeadow);
    }

    private void CleanupTestFiles()
    {
        if (File.Exists(clanFileName))
        {
            File.Delete(clanFileName);
        }

        if (File.Exists(unitFileName))
        {
            File.Delete(unitFileName);
        }

        if (File.Exists(terrainFileName))
        {
            File.Delete(terrainFileName);
        }
    }

    private static void SerializeType(string fileName, object obj)
    {
        var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(obj.GetType());
        serializer.WriteObject(stream, obj);

        TestContext.WriteLine("Writing: {0}", fileName);

        stream.Position = 0;
        var sr = new StreamReader(stream);
        File.WriteAllText(fileName, sr.ReadToEnd());
    }
}