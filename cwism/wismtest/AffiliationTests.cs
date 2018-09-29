using NUnit.Framework;
using wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism.Tests
{
    [TestFixture]
    public class AffiliationTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void CreateTest()
        {
            Affiliation affiliation = Affiliation.Create(new AffiliationInfo());            
        }

        [Test]
        public void ToStringTest()
        {
            Affiliation affiliation = Affiliation.Create(new AffiliationInfo());
            Assert.IsNotNull(affiliation.ToString());
        }
    }
}