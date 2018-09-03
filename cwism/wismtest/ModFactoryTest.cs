using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using wism;

namespace wismtest
{
    [TestClass]
    public class ModFactoryTest
    {
        #region Test constants

        private const string modPath = @"..\..\..\wism\bin\Debug\mod";
        
        private const string affiliationFileName = "Affiliation_Template.json";
        private const string unitFileName = "Unit_Template.json";
        private const string terrainFileName = "Terrain_Template.json";

        #endregion

        [TestMethod]
        public void WriteTemplatesTest()
        {
            CleanupTestFiles();

            IList<ICustomizable> objects = new List<ICustomizable>();
            objects.Add(new AffiliationInfo());
            objects.Add(new UnitInfo());
            objects.Add(new TerrainInfo());

            foreach (ICustomizable obj in objects)
                SerializeType(@".\", obj);

            if (!File.Exists(affiliationFileName) ||
                !File.Exists(unitFileName) ||
                !File.Exists(terrainFileName))
            {
                Assert.Fail("Templates not written as expected.");
            }
        }

        [TestMethod]
        public void LoadAffiliationsTest()
        {
            bool foundOrcs = false;
            IList<Affiliation> affiliations = ModFactory.LoadAffiliations(modPath);
            foreach (Affiliation affiliation in affiliations)
            {
                Logger.LogMessage("Affiliation: {0}", affiliation);
                if (affiliation.DisplayName == "Orcs of Kor")
                    foundOrcs = true;
            }

            Assert.IsTrue(foundOrcs);
        }

        [TestMethod]
        public void LoadUnitsTest()
        {
            bool foundHero = false;
            IList<Unit> units = ModFactory.LoadUnits(modPath);
            foreach (Unit unit in units)
            {
                Logger.LogMessage("Unit: {0}", unit);
                if (unit.DisplayName == "Hero")
                    foundHero = true;
            }

            Assert.IsTrue(foundHero);
        }

        [TestMethod]
        public void LoadTerrainTest()
        {
            bool foundMeadow = false;
            IList<Terrain> terrains = ModFactory.LoadTerrains(modPath);
            foreach (Terrain terrain in terrains)
            {
                Logger.LogMessage("Terrain: {0}", terrain);
                if (terrain.DisplayName == "Meadow")
                    foundMeadow = true;
            }

            Assert.IsTrue(foundMeadow);
        }

        [TestCleanup]
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

        private static void SerializeType(string path, ICustomizable obj)
        {
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            serializer.WriteObject(stream, obj);

            string fileName = String.Format("{0}\\{1}", path, obj.FileName);
            Logger.LogMessage("Writing: {0}", fileName);

            stream.Position = 0;
            StreamReader sr = new StreamReader(stream);
            File.WriteAllText(fileName, sr.ReadToEnd());
        }

        #endregion

    }
}
