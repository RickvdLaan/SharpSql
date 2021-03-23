using NUnit.Framework;
using ORM;
using ORM.Attributes;
using System.Reflection;

namespace ORMNUnit
{
    [SetUpFixture, ORMUnitTest]
    internal class NUnitSetupFixture
    {
        [OneTimeSetUp]
        public void Initialize()
        {
            _ = new ORMInitialize(Assembly.GetAssembly(GetType()), "MemoryEntityTables", "MemoryCollectionTables");
        }
    }
}
