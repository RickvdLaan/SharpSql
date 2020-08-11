using NUnit.Framework;
using ORM;
using ORM.Attributes;
using ORMFakeDAL;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace ORMNUnit
{
    [SetUpFixture, ORMUnitTest]
    internal class NUnitSetupFixture
    {
        [OneTimeSetUp]
        public void Initialize()
        {
            new ORMInitialize();

            ORMUtilities.LoadMemoryTables("MemoryTables/USERS.xml",
                                          "MemoryTables/ORGANISATIONS.xml");

            new User(0);
            new Users().Fetch();
            ORMUtilities.ExecuteDirectQuery<Users, User>(string.Empty);
        }
    }
}
