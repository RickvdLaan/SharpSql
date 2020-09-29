using NUnit.Framework;
using ORM;
using ORM.Attributes;

namespace ORMNUnit
{
    [SetUpFixture, ORMUnitTest]
    internal class NUnitSetupFixture
    {
        [OneTimeSetUp]
        public void Initialize()
        {
            new ORMInitialize("MemoryTables/USERS.xml",
                              "MemoryTables/ORGANISATIONS.xml");
        }
    }
}
