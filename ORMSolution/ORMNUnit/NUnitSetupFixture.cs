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
            new ORMInitialize();

            new User(0);
            new Users().Fetch();
            ORMUtilities.ExecuteDirectQuery<Users, User>(string.Empty);
        }
    }
}
