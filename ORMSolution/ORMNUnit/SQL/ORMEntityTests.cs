using NUnit.Framework;
using ORM;
using ORM.Attributes;
using ORMFakeDAL;

namespace ORMNUnit.SQL
{
    [ORMUnitTest]
    public class ORMEntityTests
    {
        [SetUp]
        public void Setup()
        {
            // Hack to force load the ORMFakeDAL assembly since the ORM has no clue of its existance.
            new User();
            // ¯\_(ツ)_/¯

            new ORMInitialize();
        }

        [Test]
        public void BasicFetch()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            User user = new User(1);

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }
    }
}