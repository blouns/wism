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
    public class UnitTests
    {
        [TestMethod()]
        public void CreateTest()
        {
            Unit unit = Unit.Create(new UnitInfo());
        }
    }
}