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

        [Test]
        public void CreateTest()
        {
            Unit unit = Unit.Create(new UnitInfo());
        }
    }
}