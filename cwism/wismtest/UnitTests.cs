using NUnit.Framework;
using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace wism.Tests
{
    [TestFixture]
    public class UnitTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [SetUp]
        public void Setup()
        {
            World.CreateDefaultWorld();
        }

        [Test]
        public void CreateTest()
        {
            Unit unit = Unit.Create(new UnitInfo());
        }

        [Test]
        public void StrengthTest()
        {            
            Player player = World.Current.Players[0];
            Army lightInfantry = player.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[2, 2]);
            Assert.AreEqual(3, lightInfantry.Units[0].Strength, "Light Infantry not at the expected strength.");
        }
    }
}
