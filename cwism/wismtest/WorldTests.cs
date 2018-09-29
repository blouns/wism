//using NUnit.Framework;
using wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;

namespace wism.Tests
{
    //[TestFixture]
    [TestFixture]
    public class WorldTests
    {
        private const string TestMapPath = "worldTest.json";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }


        [SetUp]
        public void TestSetup()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (File.Exists(TestMapPath))
                File.Delete(TestMapPath);
        }

        //[Test]
        [Test]
        public void SerializeTest()
        {            
            World.Current.Serialize(TestMapPath);
            Assert.IsTrue(File.Exists(TestMapPath), "File did not serialize.");    if (File.Exists(TestMapPath))
                File.Delete(TestMapPath);
        }

    }
}