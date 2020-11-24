using NUnit.Framework;
using ORM;
using ORM.Attributes;
using ORMFakeDAL;
using System;
using System.Linq;

/*
    Important note: You're only allowed to create memory collection tables based on existing data within
    the memory entity tables. Creating new data within memory collection tables can cause unexpected
    behaviour, resulting in unrealistic datasets and/or results.

    The memory collection tables simulate what a SQL Server would have returned if the SQL Server
    contained the memory entity tables.
 */
namespace ORMNUnit
{
    [TestFixture]
    public class ORMCollectionTests
    {
        [Test, ORMUnitTest("BasicFetchUsers")]
        public void BasicFetch()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U];";

            var users = new Users();
            users.Fetch();

            Assert.AreEqual(5, users.Count);

            var user1 = users.EntityCollection[0] as User;
            Assert.AreEqual(user1.Id, 1);
            Assert.AreEqual(user1.Username, "Imaani");
            Assert.AreEqual(user1.Password, "qwerty");
            Assert.IsNotNull(user1.Organisation);
            Assert.AreEqual(user1.Organisation.Id, 1);
            Assert.IsNotNull(user1.DateCreated);
            Assert.AreEqual(user1.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user1.DateLastModified);
            Assert.AreEqual(user1.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));

            var user4 = users.EntityCollection[3] as User;
            Assert.AreEqual(user4.Id, 4);
            Assert.AreEqual(user4.Username, "Adyan");
            Assert.AreEqual(user4.Password, "123456");
            Assert.IsNull(user4.Organisation);
            Assert.IsNull(user4.DateCreated);
            Assert.IsNull(user4.DateLastModified);

            Assert.IsFalse(users.Any(x => x.IsNew == true));
            Assert.IsFalse(users.Any(x => x.IsDirty == true));
            Assert.IsFalse(users.Any(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.Any(x => x.IsMarkAsDeleted == true));
            Assert.IsFalse(users.Any(x => x.DisableChangeTracking == true));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicSelectUsers")]
        public void Basic_Select()
        {
            var expectedQuery = "SELECT [U].[USERNAME], [U].[PASSWORD] FROM [DBO].[USERS] AS [U];";

            var users = new Users();
            users.Select(x => new object[] { x.Username, x.Password });
            users.Fetch();

            Assert.AreEqual(5, users.Count);

            Assert.IsFalse(users.Any(x => x.IsNew == true));
            Assert.IsFalse(users.Any(x => x.IsDirty == true));
            Assert.IsFalse(users.Any(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.Any(x => x.IsMarkAsDeleted == true));
            Assert.IsFalse(users.Any(x => x.DisableChangeTracking == true));

            var user1 = users.FirstOrDefault() as User;
            Assert.AreEqual(user1.Id, -1);
            Assert.AreEqual(user1.Username, "Imaani");
            Assert.AreEqual(user1.Password, "qwerty");
            Assert.IsNull(user1.Organisation);
            Assert.IsNull(user1.DateCreated);
            Assert.IsNull(user1.DateLastModified);

            Assert.AreEqual(user1, user1.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user1, user1.OriginalFetchedValue));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicFetchTopUsers")]
        public void BasicFetch_Top()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U];";

            var users = new Users();
            users.Fetch(1);

            Assert.AreEqual(1, users.Count);

            Assert.IsFalse(users.Any(x => x.IsNew == true));
            Assert.IsFalse(users.Any(x => x.IsDirty == true));
            Assert.IsFalse(users.Any(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.Any(x => x.IsMarkAsDeleted == true));
            Assert.IsFalse(users.Any(x => x.DisableChangeTracking == true));

            Assert.IsTrue(users.All(x => (x as User).Id == 1));
            Assert.IsTrue(users.All(x => (x as User).Username != string.Empty));
            Assert.IsTrue(users.All(x => (x as User).Password != string.Empty));
            Assert.IsTrue(users.All(x => (x as User).Organisation != null));
            Assert.IsTrue(users.All(x => (x as User).DateCreated != null));
            Assert.IsTrue(users.All(x => (x as User).DateLastModified != null));

            Assert.AreEqual(users.FirstOrDefault(), users.FirstOrDefault().OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(users.FirstOrDefault(), users.FirstOrDefault().OriginalFetchedValue));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicJoinLeft")]
        public void Basic_Join_Left()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID];";

            var users = new Users();
            users.Join(x => x.Organisation.Left());
            users.Fetch();

            Assert.AreEqual(2, users.Count);

            Assert.IsFalse(users.Any(x => x.IsNew == true));
            Assert.IsFalse(users.Any(x => x.IsDirty == true));
            Assert.IsFalse(users.Any(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.Any(x => x.IsMarkAsDeleted == true));
            Assert.IsFalse(users.Any(x => x.DisableChangeTracking == true));

            var user1 = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user1.Id);
            Assert.AreEqual(user1.Username, "Imaani");
            Assert.AreEqual(user1.Password, "qwerty");
            Assert.IsNotNull(user1.Organisation);
            Assert.AreEqual(user1.Organisation.Id, 1);
            Assert.AreEqual(user1.Organisation.Name, "The Boring Company");
            Assert.IsNotNull(user1.DateCreated);
            Assert.AreEqual(user1.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user1.DateLastModified);
            Assert.AreEqual(user1.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));

            Assert.AreEqual(user1, user1.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user1, user1.OriginalFetchedValue));

            var user2 = users.LastOrDefault() as User;
            Assert.AreEqual(5, user2.Id);
            Assert.AreEqual(user2.Username, "Chloe");
            Assert.AreEqual(user2.Password, "dragon");
            Assert.IsNotNull(user2.Organisation);
            Assert.AreEqual(user2.Organisation.Id, 2);
            Assert.AreEqual(user2.Organisation.Name, "SpaceX");
            Assert.IsNotNull(user2.DateCreated);
            Assert.AreEqual(user2.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user2.DateLastModified);
            Assert.AreEqual(user2.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));

            Assert.AreEqual(user2, user2.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user2, user2.OriginalFetchedValue));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicJoinInner")]
        public void Basic_Join_Inner()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] INNER JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID];";

            var users = new Users();
            users.Join(x => x.Organisation.Inner());
            users.Fetch();

            Assert.AreEqual(4, users.Count);

            Assert.IsFalse(users.Any(x => x.IsNew == true));
            Assert.IsFalse(users.Any(x => x.IsDirty == true));
            Assert.IsFalse(users.Any(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.Any(x => x.IsMarkAsDeleted == true));
            Assert.IsFalse(users.Any(x => x.DisableChangeTracking == true));

            var user1 = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user1.Id);
            Assert.AreEqual(user1.Username, "Imaani");
            Assert.AreEqual(user1.Password, "qwerty");
            Assert.IsNotNull(user1.Organisation);
            Assert.AreEqual(user1.Organisation.Id, 1);
            Assert.AreEqual(user1.Organisation.Name, "The Boring Company");
            Assert.IsNotNull(user1.DateCreated);
            Assert.AreEqual(user1.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user1.DateLastModified);
            Assert.AreEqual(user1.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));

            Assert.AreEqual(user1, user1.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user1, user1.OriginalFetchedValue));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereAnd")]
        public void Basic_Where_And()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE (([U].[ID] = @PARAM1) AND ([U].[USERNAME] = @PARAM2));";

            var users = new Users();
            users.Where(x => x.Id == 1 && x.Username == "Imaani");
            users.Fetch();

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual(user.Username, "Imaani");
            Assert.AreEqual(user.Password, "qwerty");
            
            // @TODO: Fix bug
            // Known bug, will be fixed before 0.2 release
            // Assert.IsNull(user1.Organisation);
            
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(user.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(user.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));
            
            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereLessThanOrEqual")]
        public void Basic_Where_LessThan()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] < @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id < 2);
            users.Fetch();

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual(user.Username, "Imaani");
            Assert.AreEqual(user.Password, "qwerty");

            // @TODO: Fix bug
            // Known bug, will be fixed before 0.2 release
            // Assert.IsNull(user1.Organisation);

            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(user.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(user.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereGreaterThanOrEqual")]
        public void Basic_Where_GreaterThan()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] > @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id > 4);
            users.Fetch();

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(5, user.Id);
            Assert.AreEqual(user.Username, "Chloe");
            Assert.AreEqual(user.Password, "dragon");

            // @TODO: Fix bug
            // Known bug, will be fixed before 0.2 release
            // Assert.IsNull(user1.Organisation);

            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(user.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(user.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));


            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereLessThanOrEqual")]
        public void Basic_Where_LessThanOrEqual()
        {
            var expectedQuery = "SELECT [U].[USERNAME] FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] <= @PARAM1);";

            var users = new Users();
            users.Select(x => x.Username)
                 .Where(x => x.Id <= 1);
            users.Fetch();

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual(user.Username, "Imaani");
            Assert.AreEqual(user.Password, "qwerty");

            // @TODO: Fix bug
            // Known bug, will be fixed before 0.2 release
            // Assert.IsNull(user1.Organisation);

            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(user.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(user.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereGreaterThanOrEqual")]
        public void Basic_Where_GreaterThanOrEqual()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] >= @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id >= 5);
            users.Fetch();

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(5, user.Id);
            Assert.AreEqual(user.Username, "Chloe");
            Assert.AreEqual(user.Password, "dragon");

            // @TODO: Fix bug
            // Known bug, will be fixed before 0.2 release
            // Assert.IsNull(user1.Organisation);

            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(user.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(user.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("ComplexJoin")]
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

            var user1 = users.FirstOrDefault() as User;
            Assert.AreEqual(user1.Username, "Imaani");
            Assert.AreEqual(user1.Password, "qwerty");
            Assert.IsNotNull(user1.Organisation);
            Assert.AreEqual(user1.Organisation.Id, 1);
            Assert.AreEqual(user1.Organisation.Name, "The Boring Company");

            Assert.IsNull(user1.DateCreated);
            Assert.IsNull(user1.DateLastModified);
            
            Assert.AreEqual(user1, user1.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user1, user1.OriginalFetchedValue));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("ComplexWhereLike")]
        public void Complex_Where_Like()
        {
            var expectedQuery = "SELECT TOP (2) * FROM [DBO].[USERS] AS [U] WHERE ((([U].[ID] LIKE @PARAM1 + '%') " +
                                "OR ([U].[PASSWORD] LIKE '%' + @PARAM2 + '%')) OR ([U].[PASSWORD] LIKE @PARAM3 + '%')) " +
                                "ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.Where(x => x.Id.ToString().StartsWith("1") || x.Password.Contains("qwerty") || x.Password.StartsWith("welkom"))
                 .OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch(2);

            var user1 = users.FirstOrDefault() as User;
            Assert.AreEqual(user1.Id, 1);
            Assert.AreEqual(user1.Username, "Imaani");
            Assert.AreEqual(user1.Password, "qwerty");
            Assert.IsNotNull(user1.Organisation);
            Assert.AreEqual(user1.Organisation.Id, 1);
            Assert.AreEqual(user1.Organisation.Name, "The Boring Company");

            Assert.IsNotNull(user1.DateCreated);
            Assert.IsNotNull(user1.DateLastModified);

            var user2 = users.LastOrDefault() as User;
            Assert.AreEqual(user2.Id, 12);
            Assert.AreEqual(user2.Username, "Elon");
            Assert.AreEqual(user2.Password, "welkom");
            Assert.IsNotNull(user2.Organisation);
            Assert.AreEqual(user2.Organisation.Id, 1);
            Assert.AreEqual(user2.Organisation.Name, "The Boring Company");

            Assert.IsNotNull(user2.DateCreated);
            Assert.IsNotNull(user2.DateLastModified);


            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicOrderBy")]
        public void Basic_OrderBy()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";
            
            var users = new Users();
            users.OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch();

            Assert.AreEqual(users.EntityCollection[0]["Id"], 4);
            Assert.AreEqual(users.EntityCollection[1]["Id"], 3);
            Assert.AreEqual(users.EntityCollection[2]["Id"], 5);
            Assert.AreEqual(users.EntityCollection[3]["Id"], 2);
            Assert.AreEqual(users.EntityCollection[4]["Id"], 1);

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        // @TODO: Fix before 0.2 release
        // Currently does not have an implementation to read from the memory database
        //[Test]
        //public void DirectQuery_Simple()
        //{
        //    var expectedQuery = "SELECT TOP 10 * FROM USERS;";
        //    var directQuery   = "SELECT TOP 10 * FROM USERS;";
        //    var collection    = ORMUtilities.ExecuteDirectQuery<Users, User>(directQuery);

        //    Assert.IsFalse(collection.DisableChangeTracking);
        //    Assert.IsFalse(collection.First().DisableChangeTracking);
        //    Assert.AreEqual(expectedQuery, collection.ExecutedQuery);
        //}

        //[Test]
        //public void DirectQuery_Complex()
        //{
        //    var expectedQuery = "SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;";
        //    var directQuery   = "SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;";
        //    //var collection    = ORMUtilities.ExecuteDirectQuery(directQuery, 1, 2);

        //    Assert.AreEqual(true, false);
        //    Assert.AreEqual(expectedQuery, directQuery);
        //}
    }
}