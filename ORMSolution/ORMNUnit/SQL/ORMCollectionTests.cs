using NUnit.Framework;
using ORM;
using ORM.Attributes;
using ORMFakeDAL;

namespace ORMNUnit.SQL
{
    [ORMUnitTest]
    public class ORMCollectionTests
    {
        [SetUp]
        public void Setup()
        {
            // Hack to force load the ORMFakeDAL assembly since the ORM has no clue of its existance.
            new Users();
            // ¯\_(ツ)_/¯

            new ORMInitialize();
        }

        [Test]
        public void BasicFetch()
        {
            var users = new Users();
            users.Fetch();
            Assert.AreEqual("SELECT * FROM [DBO].[USERS] AS [U];", users.ExecutedQuery);
        }

        [Test]
        public void BasicFetch_Top()
        {
            var users = new Users();
            users.Fetch(1);
            Assert.AreEqual("SELECT TOP (1) * FROM [DBO].[USERS] AS [U];", users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_And()
        {
            var users = new Users();
            users.Where(x => x.Id == 19 && x.Id == 12);
            users.Fetch();
            Assert.AreEqual("SELECT * FROM [DBO].[USERS] AS [U] WHERE (([U].[ID] = @PARAM1) AND ([U].[ID] = @PARAM2));", users.ExecutedQuery);
        }

        [Test]
        public void Complex_Where_StartsWith_Contains()
        {
            var users = new Users();
            users.Where(x => x.Id.ToString().StartsWith("1") || x.Password.Contains("qwerty") || x.Password.StartsWith("welkom"));
            users.Fetch();
            Assert.AreEqual("SELECT * FROM [DBO].[USERS] AS [U] WHERE ((([U].[ID] LIKE @PARAM1 + '%') OR ([U].[PASSWORD] LIKE '%' + @PARAM2 + '%')) OR ([U].[PASSWORD] LIKE @PARAM3 + '%'));", users.ExecutedQuery);
        }

        [Test]
        public void Basic_Select()
        {
            var users = new Users();
            users.Select(User.Fields.Username, User.Fields.Password);
            users.Fetch();
            Assert.AreEqual("SELECT [USERNAME], [PASSWORD] FROM [DBO].[USERS] AS [U];", users.ExecutedQuery);
        }

        [Test]
        public void Basic_OrderBy()
        {
            var users = new Users();
            users.OrderBy(User.Fields.Username.Descending(), User.Fields.Password.Ascending());
            users.Fetch();
            Assert.AreEqual("SELECT * FROM [DBO].[USERS] AS [U] ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;", users.ExecutedQuery);
        }

        [Test]
        public void DirectQuery_Simple()
        {
            var collection = ORMUtilities.ExecuteDirectQuery<Users, User>("SELECT TOP 10 * FROM USERS;");
            Assert.AreEqual("SELECT TOP 10 * FROM USERS;", collection.ExecutedQuery);
        }

        [Test]
        public void DirectQuery_Complex()
        {
            var collection = ORMUtilities.ExecuteDirectQuery<Users, User>("SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;", 1, 2);
            Assert.AreEqual("SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;", collection.ExecutedQuery);
        }

    }
}