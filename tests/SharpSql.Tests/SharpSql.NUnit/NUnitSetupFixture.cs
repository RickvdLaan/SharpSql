using NUnit.Framework;
using SharpSql.Attributes;
using System.Reflection;

namespace SharpSql.NUnit
{
    [SetUpFixture, ORMUnitTest]
    internal class NUnitSetupFixture
    {
        [OneTimeSetUp]
        public void Initialize()
        {
            _ = new SharpSqlInitializer(Assembly.GetAssembly(GetType()), "MemoryEntityTables", "MemoryCollectionTables");
        }
    }
}
