using Microsoft.VisualStudio.TestTools.UnitTesting;
using wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism.Tests
{
    [TestClass()]
    public class AffiliationTests
    {
        [TestMethod()]
        public void CreateTest()
        {
            Affiliation affiliation = Affiliation.Create(new AffiliationInfo());            
        }

        [TestMethod()]
        public void ToStringTest()
        {
            Affiliation affiliation = Affiliation.Create(new AffiliationInfo());
            Assert.IsNotNull(affiliation.ToString());
        }
    }
}