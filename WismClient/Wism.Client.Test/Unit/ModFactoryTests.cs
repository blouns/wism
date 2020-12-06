using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Test.Unit
{
    [TestFixture]
    public class ModFactoryTest
    {
        #region Test constants

        public const string TestModPath = @"mod";

        private const string clanFileName = "Clan_Template.json";
        private const string unitFileName = "Army_Template.json";
        private const string terrainFileName = "Terrain_Template.json";

        #endregion

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void WriteTemplatesTest()
        {
            CleanupTestFiles();

            ClanInfo clanInfo = new ClanInfo();
            ArmyInfo unitInfo = new ArmyInfo();
            TerrainInfo terrainInfo = new TerrainInfo();

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
            bool foundOrcs = false;
            IList<Clan> clans = ModFactory.LoadClans(TestModPath);
            foreach (Clan clan in clans)
            {
                TestContext.WriteLine("Clan: {0}", clan);
                if (clan.DisplayName == "Orcs of Kor")
                {
                    foundOrcs = true;
                    Assert.AreEqual(1, clan.GetTerrainModifier("Marsh"));
                    Assert.AreEqual(-1, clan.GetTerrainModifier("Forest"));
                    Assert.AreEqual(0, clan.GetTerrainModifier("Grass"));
                    Assert.AreEqual(0, clan.GetTerrainModifier("Hill"));
                    Assert.AreEqual(0, clan.GetTerrainModifier("OuterSpace"));
                }
            }

            Assert.IsTrue(foundOrcs);
        }


        [Test]
        public void LoadArmysTest()
        {
            bool foundHero = false;
            IList<Army> units = ModFactory.LoadArmies(TestModPath);

            foreach (Army unit in units)
            {
                TestContext.WriteLine("Army: {0}", unit);
                if (unit.ShortName == "Hero")
                    foundHero = true;
            }

            Assert.IsTrue(foundHero);
        }

        [Test]
        public void LoadTerrainTest()
        {
            bool foundMeadow = false;
            IList<Terrain> terrains = ModFactory.LoadTerrains(TestModPath);
            foreach (Terrain terrain in terrains)
            {
                TestContext.WriteLine("Terrain: {0}", terrain);
                if (terrain.DisplayName == "Grass")
                    foundMeadow = true;
            }

            Assert.IsTrue(foundMeadow);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            CleanupTestFiles();
        }

        #region Utility methods        
        private void CleanupTestFiles()
        {
            if (File.Exists(clanFileName))
                File.Delete(clanFileName);
            if (File.Exists(unitFileName))
                File.Delete(unitFileName);
            if (File.Exists(terrainFileName))
                File.Delete(terrainFileName);
        }

        private static void SerializeType(string fileName, object obj)
        {
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            serializer.WriteObject(stream, obj);

            TestContext.WriteLine("Writing: {0}", fileName);

            stream.Position = 0;
            StreamReader sr = new StreamReader(stream);
            File.WriteAllText(fileName, sr.ReadToEnd());
        }

        #endregion

    }
}
