using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using BranallyGames.Wism;
using NUnit.Framework;

namespace wism.Tests
{
    [TestFixture]
    public class ModFactoryTest
    {
        #region Test constants

        public const string TestModPath = @"..\..\..\wism\bin\Debug\mod";
        
        private const string affiliationFileName = "Affiliation_Template.json";
        private const string unitFileName = "Unit_Template.json";
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

            AffiliationInfo affiliationInfo = new AffiliationInfo();
            UnitInfo unitInfo = new UnitInfo();
            TerrainInfo terrainInfo = new TerrainInfo();

            SerializeType(affiliationFileName, affiliationInfo);
            SerializeType(unitFileName, unitInfo);
            SerializeType(terrainFileName, terrainInfo);

            if (!File.Exists(affiliationFileName) ||
                !File.Exists(unitFileName) ||
                !File.Exists(terrainFileName))
            {
                Assert.Fail("Templates not written as expected.");
            }
        }

        [Test]
        public void LoadAffiliationsTest()
        {
            bool foundOrcs = false;
            IList<Affiliation> affiliations = ModFactory.LoadAffiliations(TestModPath);
            foreach (Affiliation affiliation in affiliations)
            {
                TestContext.WriteLine("Affiliation: {0}", affiliation);
                if (affiliation.DisplayName == "Orcs of Kor")
                    foundOrcs = true;
            }

            Assert.IsTrue(foundOrcs);
        }

        [Test]
        public void LoadUnitsTest()
        {
            bool foundHero = false;
            IList<Unit> units = ModFactory.LoadUnits(TestModPath);            

            foreach (Unit unit in units)
            {
                TestContext.WriteLine("Unit: {0}", unit);
                if (unit.ID == "Hero")
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
            if (File.Exists(affiliationFileName))
                File.Delete(affiliationFileName);
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
