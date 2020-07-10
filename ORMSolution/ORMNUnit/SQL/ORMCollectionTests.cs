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
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U];";

            var users = new Users();
            users.Fetch();
            
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void BasicFetch_Top()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U];";

            var users = new Users();
            users.Fetch(1);
            
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_And()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE (([U].[ID] = @PARAM1) AND ([U].[ID] = @PARAM2));";

            var users = new Users();
            users.Where(x => x.Id == 19 && x.Id == 12);
            users.Fetch();

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_LessThan()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] < @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id < 1);
            users.Fetch();

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_GreaterThan()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] > @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id > 1);
            users.Fetch();

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_LessThanOrEqual()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] <= @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id <= 1);
            users.Fetch();

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_GreaterThanOrEqual()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] >= @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id >= 1);
            users.Fetch();

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Complex_Where_Like()
        {
            var expectedQuery = 
                "SELECT * FROM [DBO].[USERS] AS [U] WHERE ((([U].[ID] LIKE @PARAM1 + '%') " +
                "OR ([U].[PASSWORD] LIKE '%' + @PARAM2 + '%')) OR ([U].[PASSWORD] LIKE @PARAM3 + '%')) " +
                "ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.Where(x => x.Id.StartsWith("1") || x.Password.Contains("qwerty") || x.Password.StartsWith("welkom"));
            users.OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch();
            
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Select()
        {
            var expectedQuery = "SELECT [U].[USERNAME], [U].[PASSWORD] FROM [DBO].[USERS] AS [U];";

            var users = new Users();
            users.Select(x => new object[] { x.Username, x.Password });
            users.Fetch();
            
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_OrderBy()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch();

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void DirectQuery_Simple()
        {
            // The framework shouldn't tamper with the query.
            var expectedQuery = "SELECT TOP 10 * FROM USERS;";
            var directQuery   = "SELECT TOP 10 * FROM USERS;";
            var collection    = ORMUtilities.ExecuteDirectQuery<Users, User>(directQuery);
            
            Assert.AreEqual(expectedQuery, collection.ExecutedQuery);
        }

        [Test]
        public void DirectQuery_Complex()
        {
            // The framework shouldn't tamper with the query.
            var expectedQuery = "SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;";
            var directQuery   = "SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;";
            var collection    = ORMUtilities.ExecuteDirectQuery<Users, User>(directQuery, 1, 2);
            
            Assert.AreEqual(expectedQuery, collection.ExecutedQuery);
        }
    }
}