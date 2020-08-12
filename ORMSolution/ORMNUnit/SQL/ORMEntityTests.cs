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

            Assert.AreEqual(user.IsDirty, false);
            Assert.AreEqual(user.IsNew, false);
            Assert.IsNull(user.Organisation, null);
            Assert.NotNull(user.OriginalFetchedValue, null);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }

        [Test]
        public void Fetch_Join()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(1);

            Assert.AreEqual(user.IsDirty, false);
            Assert.AreEqual(user.IsNew, false);
            Assert.NotNull(user.OriginalFetchedValue, null);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            Assert.AreEqual(user.Organisation.IsDirty, false);
            Assert.AreEqual(user.Organisation.IsNew, false);
            Assert.NotNull(user.Organisation.OriginalFetchedValue, null);
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Fetch_Join_Dirty()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(1);
            user.Organisation.Name = "Unit Test";

            Assert.AreEqual(user.IsDirty, true);
            Assert.AreEqual(user.IsNew, false);
            Assert.NotNull(user.OriginalFetchedValue, null);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            Assert.AreEqual(user.Organisation.IsDirty, true);
            Assert.AreEqual(user.Organisation.IsNew, false);
            Assert.NotNull(user.Organisation.OriginalFetchedValue, null);
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Fetch_Join_New()
        {
            var expectedUserQuery = "UPDATE [O] SET [O].[NAME] = @PARAM2 FROM [dbo].[Organisations] AS [O] WHERE ([O].[Id] = @PARAM1); UPDATE [U] SET [U].[ORGANISATION] = @PARAM3 [U].[DATELASTMODIFIED] = @PARAM4 FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM1);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOrganisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES(@PARAM1); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var expectedOriginalOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(1)
            {
                Organisation = new Organisation() { Name = "Unit Test" }
            };
            user.Save();

            Assert.AreEqual(user.IsDirty, true);
            Assert.AreEqual(user.IsNew, false);
            Assert.NotNull(user.OriginalFetchedValue, null);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            Assert.AreEqual(user.Organisation.IsDirty, true);
            Assert.AreEqual(user.Organisation.IsNew, true);
            Assert.NotNull(user.Organisation.OriginalFetchedValue, null);
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalOrganisationQuery, user.Organisation.OriginalFetchedValue.ExecutedQuery);
        }

        [Test]
        public void Insert()
        {

        }

        [Test]
        public void Insert_Join()
        {

        }

        [Test]
        public void Update()
        {

        }

        [Test]
        public void Update_Join()
        {

        }

        [Test]
        public void Update_JoinInsert()
        {

        }

        [Test]
        public void Update_DirtyJoin()
        {

        }
    }
}