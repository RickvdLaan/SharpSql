using NUnit.Framework;
using ORMFakeDAL;

namespace ORMNUnit
{

    [TestFixture]
    public class ORMEntityTests
    {
        [Test]
        public void Fetch()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(4);

            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNull(user.Organisation);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }

        [Test]
        public void Fetch_Dirty()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(4)
            {
                Password = "qwerty"
            };

            Assert.AreEqual(user.IsDirty, true);
            Assert.AreEqual(user.IsNew, false);
            Assert.IsNull(user.Organisation);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }

        [Test]
        public void Fetch_Join()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(1);

            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(user.ExecutedQuery, expectedUserQuery);

            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Fetch_Join_Dirty()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(1);
            user.Organisation.Name = "Unit Test";

            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.AreNotEqual(user.Organisation.Name, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Fetch_Join_New()
        {
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM3);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOrganisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES(@PARAM1); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var expectedOriginalOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(1)
            {
                Organisation = new Organisation() { Name = "Unit Test" }
            };
            user.Save();

            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalOrganisationQuery, user.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
        }

        [Test]
        public void Insert()
        {
            var expectedUserQuery = "INSERT INTO [DBO].[USERS] ([DBO].[USERS].[USERNAME], [DBO].[USERS].[PASSWORD], [DBO].[USERS].[ORGANISATION], [DBO].[USERS].[DATECREATED], [DBO].[USERS].[DATELASTMODIFIED]) VALUES(@PARAM1, @PARAM2, @PARAM3, @PARAM4, @PARAM5); SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var user = new User()
            {
                Username = "Unit",
                Password = "Test",
                Organisation = null
            };

            user.Save();

            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(true, user.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);
            Assert.IsNull(user.Organisation);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }

        [Test]
        public void Insert_Join_Existing()
        {
            var expectedUserQuery = "INSERT INTO [DBO].[USERS] ([DBO].[USERS].[USERNAME], [DBO].[USERS].[PASSWORD], [DBO].[USERS].[ORGANISATION], [DBO].[USERS].[DATECREATED], [DBO].[USERS].[DATELASTMODIFIED]) VALUES(@PARAM1, @PARAM2, @PARAM3, @PARAM4, @PARAM5); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User()
            {
                Username = "Unit",
                Password = "Test",
                Organisation = new Organisation(1)
            };

            user.Save();

            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(true, user.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);
            Assert.IsNotNull(user.Organisation);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);

            Assert.AreEqual(user.Organisation, user.Organisation.OriginalFetchedValue);
        }

        [Test]
        public void Insert_Join_New()
        {
            var expectedUserQuery = "INSERT INTO [DBO].[USERS] ([DBO].[USERS].[USERNAME], [DBO].[USERS].[PASSWORD], [DBO].[USERS].[ORGANISATION], [DBO].[USERS].[DATECREATED], [DBO].[USERS].[DATELASTMODIFIED]) VALUES(@PARAM1, @PARAM2, @PARAM3, @PARAM4, @PARAM5); SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var user = new User()
            {
                Username = "Unit",
                Password = "Test",
                // @Bug, @FixMe, @Important: Doesn't work yet, unknown query exected... Relations is empty.
                Organisation = new Organisation() { Name = "The Test Organisation" }
            };

            user.Save();

            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(true, user.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);
            Assert.IsNotNull(user.Organisation);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);

            Assert.AreEqual(user.Organisation, user.Organisation.OriginalFetchedValue);
        }

        [Test]
        public void Update()
        {
            Assert.AreEqual(true, false);
        }

        [Test]
        public void Update_Join()
        {
            Assert.AreEqual(true, false);
        }

        [Test]
        public void Update_JoinInsert()
        {
            Assert.AreEqual(true, false);
        }

        [Test]
        public void Update_DirtyJoin()
        {
            Assert.AreEqual(true, false);
        }
    }
}