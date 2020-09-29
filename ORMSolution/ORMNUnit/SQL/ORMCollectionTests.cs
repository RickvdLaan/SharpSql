using NUnit.Framework;
using ORM;
using ORMFakeDAL;
using System.Linq;

namespace ORMNUnit
{
    [TestFixture]
    public class ORMCollectionTests
    {
        [Test]
        public void BasicFetch()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U];";

            var users = new Users();
            users.Fetch();

            Assert.AreEqual(5, users.Count);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Select()
        {
            var expectedQuery = "SELECT [U].[USERNAME], [U].[PASSWORD] FROM [DBO].[USERS] AS [U];";

            var users = new Users();
            users.Select(x => new object[] { x.Username, x.Password });
            users.Fetch();

            Assert.AreEqual(5, users.Count);

            Assert.IsFalse(users.Any(x => x.IsNew != true));
            Assert.IsFalse(users.Any(x => x.IsDirty != true));
            Assert.IsFalse(users.Any(x => x.IsAutoIncrement != true));
            Assert.IsFalse(users.Any(x => x.IsMarkAsDeleted != false));
            Assert.IsFalse(users.Any(x => x.DisableChangeTracking != false));

            Assert.IsTrue(users.All(x => (x as User).Id == -1));
            Assert.IsTrue(users.All(x => (x as User).Username != string.Empty));
            Assert.IsTrue(users.All(x => (x as User).Password != string.Empty));
            Assert.IsTrue(users.All(x => (x as User).Organisation == null));
            Assert.IsTrue(users.All(x => (x as User).DateCreated == null));
            Assert.IsTrue(users.All(x => (x as User).DateLastModified == null));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void BasicFetch_Top()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U];";

            var users = new Users();
            users.Fetch(1);

            Assert.AreEqual(1, users.Count);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Join_Left()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID];";

            var users = new Users();
            users.Join(x => x.Organisation.Left());
            users.Fetch();

            Assert.AreEqual(true, false);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Join_Inner()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] INNER JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID];";

            var users = new Users();
            users.Join(x => x.Organisation.Inner());
            users.Fetch();

            Assert.AreEqual(true, false);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_And()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE (([U].[ID] = @PARAM1) AND ([U].[ID] = @PARAM2));";

            var users = new Users();
            users.Where(x => x.Id == 19 && x.Id == 12);
            users.Fetch();

            Assert.AreEqual(true, false);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_LessThan()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] < @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id < 1);
            users.Fetch();

            Assert.AreEqual(true, false);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_GreaterThan()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] > @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id > 1);
            users.Fetch();

            Assert.AreEqual(true, false);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_LessThanOrEqual()
        {
            var expectedQuery = "SELECT [U].[USERNAME] FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] <= @PARAM1);";

            var users = new Users();
            users.Select(x => x.Username)
                 .Where(x => x.Id <= 1);
            users.Fetch();

            Assert.AreEqual(true, false);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_Where_GreaterThanOrEqual()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] >= @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id >= 1);
            users.Fetch();

            Assert.AreEqual(true, false);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Complex_Join()
        {
            var expectedQuery = "SELECT TOP (1) [U].[USERNAME], [U].[PASSWORD], [U].[ORGANISATION] " +
                "FROM [DBO].[USERS] AS [U] " +
                "LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] " +
                "INNER JOIN [DBO].[ORGANISATIONS] AS [OO] ON [U].[ORGANISATION] = [OO].[ID] " +
                "WHERE ([U].[ID] > @PARAM1) " +
                "ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.Select(x => new object[] { x.Username, x.Password, x.Organisation })
                 .Join(x => new object[] { x.Organisation.Left(), x.Organisation.Inner() })
                 .Where(x => x.Id > 1)
                 .OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch(1);

            Assert.AreEqual(true, false);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Complex_Where_Like()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ((([U].[ID] LIKE @PARAM1 + '%') " +
                                "OR ([U].[PASSWORD] LIKE '%' + @PARAM2 + '%')) OR ([U].[PASSWORD] LIKE @PARAM3 + '%')) " +
                                "ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.Where(x => x.Id.ToString().StartsWith("1") || x.Password.Contains("qwerty") || x.Password.StartsWith("welkom"))
                 .OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch();

            Assert.AreEqual(true, false);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void Basic_OrderBy()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";
            
            var users = new Users();
            users.OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch();

            Assert.AreEqual(true, false);
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test]
        public void DirectQuery_Simple()
        {
            var expectedQuery = "SELECT TOP 10 * FROM USERS;";
            var directQuery   = "SELECT TOP 10 * FROM USERS;";
            var collection    = ORMUtilities.ExecuteDirectQuery<Users, User>(directQuery);

            Assert.IsFalse(collection.DisableChangeTracking);
            Assert.IsFalse(collection.First().DisableChangeTracking);
            Assert.AreEqual(expectedQuery, collection.ExecutedQuery);
        }

        [Test]
        public void DirectQuery_Complex()
        {
            var expectedQuery = "SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;";
            var directQuery   = "SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;";
            var collection    = ORMUtilities.ExecuteDirectQuery<Users, User>(directQuery, true, 1, 2);

            Assert.IsTrue(collection.DisableChangeTracking);
            Assert.IsTrue(collection.First().DisableChangeTracking);
            Assert.AreEqual(expectedQuery, collection.ExecutedQuery);
        }
    }
}