using NUnit.Framework;
using ORM;
using ORM.Attributes;
using ORMFakeDAL;

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

            new User(1);
            new Users().Fetch();
            ORMUtilities.ExecuteDirectQuery<Users, User>(string.Empty);
        }
    }
}
