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
            // Hack to force load the ORMFakeDAL assembly since the ORM has no clue of its existance
            // during initialization.
            new Users();
            // ¯\_(ツ)_/¯

            new ORMInitialize();
        }
    }
}
