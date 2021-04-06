using NUnit.Framework;
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

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("Imaani", user.Username);
            Assert.AreEqual("qwerty", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            Assert.IsTrue(users.All(x => x.ValueAs<User>().Organisation == null));
            Assert.IsTrue(users.All(x => x.OriginalFetchedValue.ValueAs<User>().Organisation == null));

            Assert.IsFalse(users.All(x => x.IsNew == true));
            Assert.IsFalse(users.All(x => x.IsDirty == true));
            Assert.IsFalse(users.All(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.All(x => x.IsMarkAsDeleted == true));
            Assert.IsFalse(users.All(x => x.DisableChangeTracking == true));

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

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

            var user = users.FirstOrDefault() as User;

            Assert.AreEqual(-1, user.Id);
            Assert.AreEqual("Imaani", user.Username);
            Assert.AreEqual("qwerty", user.Password);
            Assert.IsNull(user.Organisation);

            Assert.IsNull(user.DateCreated);
            Assert.IsNull(user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            Assert.IsTrue(users.All(x => x.ValueAs<User>().Organisation == null));
            Assert.IsTrue(users.All(x => x.OriginalFetchedValue.ValueAs<User>().Organisation == null));

            Assert.IsFalse(users.All(x => x.IsNew == true));
            Assert.IsFalse(users.All(x => x.IsDirty == true));
            Assert.IsFalse(users.All(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.All(x => x.IsMarkAsDeleted == true));
            Assert.IsFalse(users.All(x => x.DisableChangeTracking == true));

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicFetchTopUsers")]
        public void BasicFetch_Top()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U];";

            var users = new Users();
            users.Fetch(1);

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("Imaani", user.Username);
            Assert.AreEqual("qwerty", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateLastModified);

            Assert.IsFalse(user.IsNew == true);
            Assert.IsFalse(user.IsDirty == true);
            Assert.IsFalse(user.IsAutoIncrement == false);
            Assert.IsFalse(user.IsMarkAsDeleted == true);
            Assert.IsFalse(user.DisableChangeTracking == true);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicJoinLeft")]
        public void Basic_Join_Left()
        {
            // A left join has two cases, either .Left() is provided or no join type is provided.
            // When no join is provided, it'll automatically use the left join. For this reason this
            // case is tested twice - once with, and without.

            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID];";

            // First case:
            var users = new Users();
            users.Join(x => x.Organisation.Left());
            users.Fetch();

            Assert.AreEqual(5, users.Count);

            Assert.IsFalse(users.All(x => x.IsNew == true));
            Assert.IsFalse(users.All(x => x.IsDirty == true));
            Assert.IsFalse(users.All(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.All(x => x.IsMarkAsDeleted == true));
            Assert.IsFalse(users.All(x => x.DisableChangeTracking == true));

            var user1 = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user1.Id);
            Assert.AreEqual("Imaani", user1.Username);
            Assert.AreEqual("qwerty", user1.Password);
            Assert.IsNotNull(user1.Organisation);
            Assert.AreEqual(user1.Organisation.Id, 1);
            Assert.AreEqual(user1.Organisation.Name, "The Boring Company");
            Assert.IsNotNull(user1.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user1.DateCreated);
            Assert.IsNotNull(user1.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user1.DateLastModified);

            Assert.AreEqual(user1, user1.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user1, user1.OriginalFetchedValue));

            Assert.AreEqual(user1.Organisation, user1.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsFalse(ReferenceEquals(user1.Organisation, user1.OriginalFetchedValue.ValueAs<User>().Organisation));

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

            Assert.AreEqual(user2.Organisation, user2.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsFalse(ReferenceEquals(user2.Organisation, user2.OriginalFetchedValue.ValueAs<User>().Organisation));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);

            // Second case:
            users = new Users();
            users.Join(x => x.Organisation);
            users.Fetch();

            Assert.AreEqual(5, users.Count);

            Assert.IsFalse(users.All(x => x.IsNew == true));
            Assert.IsFalse(users.All(x => x.IsDirty == true));
            Assert.IsFalse(users.All(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.All(x => x.IsMarkAsDeleted == true));
            Assert.IsFalse(users.All(x => x.DisableChangeTracking == true));

            user1 = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user1.Id);
            Assert.AreEqual("Imaani", user1.Username);
            Assert.AreEqual("qwerty", user1.Password);
            Assert.IsNotNull(user1.Organisation);
            Assert.AreEqual(user1.Organisation.Id, 1);
            Assert.AreEqual(user1.Organisation.Name, "The Boring Company");
            Assert.IsNotNull(user1.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user1.DateCreated);
            Assert.IsNotNull(user1.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user1.DateLastModified);

            Assert.AreEqual(user1, user1.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user1, user1.OriginalFetchedValue));

            Assert.AreEqual(user1.Organisation, user1.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsFalse(ReferenceEquals(user1.Organisation, user1.OriginalFetchedValue.ValueAs<User>().Organisation));

            user2 = users.LastOrDefault() as User;
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

            Assert.AreEqual(user2.Organisation, user2.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsFalse(ReferenceEquals(user2.Organisation, user2.OriginalFetchedValue.ValueAs<User>().Organisation));

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

            Assert.IsFalse(users.All(x => x.IsNew == true));
            Assert.IsFalse(users.All(x => x.IsDirty == true));
            Assert.IsFalse(users.All(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.All(x => x.IsMarkAsDeleted == true));
            Assert.IsFalse(users.All(x => x.DisableChangeTracking == true));

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual(user.Username, "Imaani");
            Assert.AreEqual(user.Password, "qwerty");
            Assert.IsNotNull(user.Organisation);
            Assert.AreEqual(user.Organisation.Id, 1);
            Assert.AreEqual(user.Organisation.Name, "The Boring Company");
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(user.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(user.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));

            Assert.IsFalse(user.IsNew == true);
            Assert.IsFalse(user.IsDirty == true);
            Assert.IsFalse(user.IsAutoIncrement == false);
            Assert.IsFalse(user.IsMarkAsDeleted == true);
            Assert.IsFalse(user.DisableChangeTracking == true);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsFalse(ReferenceEquals(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereAnd")]
        public void Basic_Where_And()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE (([U].[ID] = @PARAM1) AND ([U].[USERNAME] = @PARAM2));";

            var users = new Users();
            users.Where(x => x.Id == 1 && x.Username == "Imaani");
            users.Fetch();

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("Imaani", user.Username);
            Assert.AreEqual("qwerty", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereLessThanOrEqual")]
        public void Basic_Where_LessThan()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] < @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id < 2);
            users.Fetch();

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("Imaani", user.Username);
            Assert.AreEqual("qwerty", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereEqualTo")]
        public void Basic_Where_EqualTo()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var userId = 1;
            var users = new Users();
            users.Where(x => x.Id == userId);
            users.Fetch();

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("Imaani", user.Username);
            Assert.AreEqual("qwerty", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereNotEqualTo")]
        public void Basic_Where_NotEqualTo()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] <> @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id != 1);
            users.Fetch();

            Assert.AreEqual(4, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(2, user.Id);
            Assert.AreEqual("Clarence", user.Username);
            Assert.AreEqual("password", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.IsTrue(users.All(x => (x as User).Organisation == null));
            Assert.IsTrue(users.All(x => (x.OriginalFetchedValue as User).Organisation == null));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereGreaterThanOrEqual")]
        public void Basic_Where_GreaterThan()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] > @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id > 4);
            users.Fetch();

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(5, user.Id);
            Assert.AreEqual("Chloe", user.Username);
            Assert.AreEqual("dragon", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.IsTrue(users.All(x => (x as User).Organisation == null));
            Assert.IsTrue(users.All(x => (x.OriginalFetchedValue as User).Organisation == null));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("BasicWhereLessThanOrEqual")]
        public void Basic_Where_LessThanOrEqual()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] <= @PARAM1);";

            var users = new Users();
            users.Where(x => x.Id <= 1);
            users.Fetch();

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual(user.Username, "Imaani");
            Assert.AreEqual(user.Password, "qwerty");

            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);

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

            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);

            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(user.DateCreated, DateTime.Parse("2020-07-23T16:50:38.213"));
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(user.DateLastModified, DateTime.Parse("2020-07-23T16:50:38.213"));

            Assert.AreEqual(expectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("ComplexJoinA")]
        public void Complex_Join_A()
        {
            // First case
            var firstExpectedQuery = "SELECT TOP (1) [U].[USERNAME], [U].[PASSWORD], [O].[NAME] " +
                "FROM [DBO].[USERS] AS [U] " +
                "LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] " +
                "WHERE ([U].[ID] > @PARAM1) " +
                "ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.Select(x => new object[] { x.Username, x.Password, x.Organisation.Name })
                 .Join(x => new object[] { x.Organisation.Left() })
                 .Where(x => x.Id > 1)
                 .OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch(1);

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(-1, user.Id);
            Assert.AreEqual("Clarence", user.Username);
            Assert.AreEqual("password", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNull(user.DateCreated);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().DateCreated);
            Assert.IsNull(user.DateLastModified);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().DateLastModified);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);

            Assert.AreEqual(firstExpectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("ComplexJoinB")]
        public void Complex_Join_B()
        {
            var firstExpectedQuery = "SELECT TOP (1) [U].[USERNAME], [U].[PASSWORD], [O].[ID] " +
                "FROM [DBO].[USERS] AS [U] " +
                "LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] " +
                "WHERE ([U].[ID] > @PARAM1) " +
                "ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.Select(x => new object[] { x.Username, x.Password, x.Organisation.Id })
                 .Join(x => new object[] { x.Organisation.Left() })
                 .Where(x => x.Id > 1)
                 .OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch(1);

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(-1, user.Id);
            Assert.AreEqual("Clarence", user.Username);
            Assert.AreEqual("password", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNull(user.DateCreated);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().DateCreated);
            Assert.IsNull(user.DateLastModified);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().DateLastModified);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);

            Assert.AreEqual(firstExpectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("ComplexJoinC")]
        public void Complex_Join_C()
        {
            var secondExpectedQuery = "SELECT TOP (1) [U].[USERNAME], [U].[PASSWORD], [U].[ORGANISATION], [O].[ID] " +
                "FROM [DBO].[USERS] AS [U] " +
                "LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] " +
                "WHERE ([U].[ID] > @PARAM1) " +
                "ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.Select(x => new object[] { x.Username, x.Password, x.Organisation, x.Organisation.Id })
                 .Join(x => new object[] { x.Organisation.Left() })
                 .Where(x => x.Id > 1)
                 .OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch(1);

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(-1, user.Id);
            Assert.AreEqual("Clarence", user.Username);
            Assert.AreEqual("password", user.Password);
            Assert.IsNotNull(user.Organisation);
            Assert.AreEqual(user.Organisation.Id, 1);
            Assert.IsNull(user.Organisation.Name);
            Assert.IsNull(user.DateCreated);
            Assert.IsNull(user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 1);

            Assert.IsTrue(user.Organisation != null);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation != null);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsFalse(ReferenceEquals(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation));

            Assert.AreEqual(secondExpectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("ComplexJoinD")]
        public void Complex_Join_D()
        {
            var thirdExpectedQuery = "SELECT TOP (1) [U].[USERNAME], [U].[PASSWORD], [U].[ORGANISATION], [O].[NAME] " +
                "FROM [DBO].[USERS] AS [U] " +
                "LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] " +
                "WHERE ([U].[ID] > @PARAM1) " +
                "ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.Select(x => new object[] { x.Username, x.Password, x.Organisation, x.Organisation.Name })
                 .Join(x => new object[] { x.Organisation.Left() })
                 .Where(x => x.Id > 1)
                 .OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch(1);

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(-1, user.Id);
            Assert.AreEqual("Clarence", user.Username);
            Assert.AreEqual("password", user.Password);
            Assert.IsNotNull(user.Organisation);
            Assert.AreEqual(-1, user.Organisation.Id);
            Assert.AreEqual("The Boring Company", user.Organisation.Name);
            Assert.IsNull(user.DateCreated);
            Assert.IsNull(user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 1);

            Assert.IsTrue(user.Organisation != null);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation != null);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsFalse(ReferenceEquals(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation));

            Assert.AreEqual(thirdExpectedQuery, users.ExecutedQuery);
        }

        [Test, ORMUnitTest("ComplexJoinE")]
        public void Complex_Join_E()
        {
            var thirdExpectedQuery = "SELECT TOP (1) [U].[USERNAME], [U].[PASSWORD], [U].[ORGANISATION], [O].[ID], [O].[NAME] " +
                "FROM [DBO].[USERS] AS [U] " +
                "LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] " +
                "WHERE ([U].[ID] > @PARAM1) " +
                "ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.Select(x => new object[] { x.Username, x.Password, x.Organisation, x.Organisation.Id, x.Organisation.Name })
                 .Join(x => new object[] { x.Organisation.Left() })
                 .Where(x => x.Id > 1)
                 .OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch(1);

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(-1, user.Id);
            Assert.AreEqual("Clarence", user.Username);
            Assert.AreEqual("password", user.Password);
            Assert.IsNotNull(user.Organisation);
            Assert.AreEqual(user.Organisation.Id, 1);
            Assert.AreEqual(user.Organisation.Name, "The Boring Company");
            Assert.IsNull(user.DateCreated);
            Assert.IsNull(user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 1);

            Assert.IsTrue(user.Organisation != null);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation != null);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

            Assert.AreEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsFalse(ReferenceEquals(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation));

            Assert.AreEqual(thirdExpectedQuery, users.ExecutedQuery);
        }


        [Test, ORMUnitTest("ComplexWhereLike")]
        public void Complex_Where_Like()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ((([U].[ID] LIKE @PARAM1 + '%') " +
                                "OR ([U].[PASSWORD] LIKE '%' + @PARAM2 + '%')) OR ([U].[PASSWORD] LIKE @PARAM3 + '%')) " +
                                "ORDER BY [U].[USERNAME] DESC, [U].[PASSWORD] ASC;";

            var users = new Users();
            users.Where(x => x.Id.ToString().StartsWith("1") || x.Password.Contains("qwerty") || x.Password.StartsWith("welkom"))
                 .OrderBy(x => new object[] { x.Username.Descending(), x.Password.Ascending() });
            users.Fetch(1);

            Assert.AreEqual(1, users.Count);

            var user = users.FirstOrDefault() as User;
            Assert.AreEqual(user.Id, 1);
            Assert.AreEqual(user.Username, "Imaani");
            Assert.AreEqual(user.Password, "qwerty");
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);

            Assert.AreEqual(user, user.OriginalFetchedValue);
            Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));

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