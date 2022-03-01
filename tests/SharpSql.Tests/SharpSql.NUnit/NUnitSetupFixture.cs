using NUnit.Framework;
using SharpSql.UnitTests;
using System.Reflection;

namespace SharpSql.NUnit;

[SetUpFixture, SharpSqlUnitTest]
internal class NUnitSetupFixture
{
    [OneTimeSetUp]
    public void Initialize()
    {
        _ = new SharpSqlInitializer(Assembly.GetAssembly(GetType()), "MemoryEntityTables", "MemoryCollectionTables");
    }
}