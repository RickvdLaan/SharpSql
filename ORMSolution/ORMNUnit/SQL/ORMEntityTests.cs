using NUnit.Framework;
using ORMFakeDAL;
using System.Collections.Generic;

namespace ORMNUnit
{

    [TestFixture]
    public class ORMEntityTests
    {
        /*
         IsDirty change tracking
        IsNew tracking
        Originally fetched value (ShallowCopy)
        Easy access to executed queries
         */

        [Test]
        public void Fetch()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(1);

            Assert.AreEqual(user.IsDirty, false);
            Assert.AreEqual(user.IsNew, false);
            Assert.AreEqual(user.OriginalFetchedValue, null);
            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        /*
        aparte tests
        DisableChangedTracking = true, dan IsDirty false, als false dan afhankelijk van data. (IsDirtyList)
        IsNew is altijd Dirty
        Executed query
        TableScheme
         */

        [Test]
        public void BasicFetch_Old()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User();

            var tableScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password)
            };

           // user.FetchEntityByPrimaryKey(tableScheme, 1);

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicFetch_Join_Old()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User();

            var tableScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password),
                nameof(user.Organisation)
            };

            //user.FetchEntityByPrimaryKey(tableScheme, 1);

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicInsert_Old()
        {
            var expectedQuery = "INSERT INTO [DBO].[USERS] ([DBO].[USERS].[USERNAME], [DBO].[USERS].[PASSWORD], [DBO].[USERS].[ORGANISATION]) VALUES('Unit', 'Test', NULL); SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var user = new User()
            {
                Username = "Unit",
                Password = "Test",
                Organisation = null
            };

            var tableScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password),
                nameof(user.Organisation)
            };

            user.MutableTableScheme = tableScheme;
            user.DisableChangeTracking = true;
            user.Save();

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicInsert_Join_Old()
        {
            var expectedUserQuery         = "INSERT INTO [DBO].[USERS] ([DBO].[USERS].[USERNAME], [DBO].[USERS].[PASSWORD], [DBO].[USERS].[ORGANISATION]) VALUES('Unit', 'Test', '-1'); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User()
            {
                Username = "Unit",
                Password = "Test"
            };

            var userTableScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password),
                nameof(user.Organisation)
            };

            user.MutableTableScheme = userTableScheme;
            user.DisableChangeTracking = true;

            var organisation = new Organisation()
            {
                Name = "Unit"
            };

            var organisationTableScheme = new List<string>
            {
                nameof(organisation.Id),
                nameof(organisation.Name)
            };

            //organisation.FetchEntityByPrimaryKey(organisationTableScheme, 1);

            user.Organisation = organisation;
            user.Save();

            Assert.AreEqual(expectedOrganisationQuery, organisation.ExecutedQuery);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicUpdate_Old()
        {
            var expectedQuery = "UPDATE [U] SET [U].[PASSWORD] = 'Test' FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM1);";

            var user = new User();

            var tableScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password),
                nameof(user.Organisation)
            };

            (string fieldName, bool isDirty)[] dirtyFields = new (string fieldName, bool isDirty)[tableScheme.Count - user.PrimaryKey.Count];
            dirtyFields[0].fieldName = nameof(user.Username);
            dirtyFields[0].isDirty = false;

            dirtyFields[1].fieldName = nameof(user.Password);
            dirtyFields[1].isDirty = true;

            dirtyFields[2].fieldName = nameof(user.Organisation);
            dirtyFields[2].isDirty = false;

            user.MutableTableScheme = tableScheme;
            user.IsDirtyList = dirtyFields;
            user.OriginalFetchedValue = user.ShallowCopy();

            var organisation = new Organisation()
            {
                Name = "Unit"
            };

            var organisationTableScheme = new List<string>
            {
                nameof(organisation.Id),
                nameof(organisation.Name)
            };

            (string fieldName, bool isDirty)[] dirtyOrganisationFields = new (string fieldName, bool isDirty)[organisationTableScheme.Count - organisation.PrimaryKey.Count];
            dirtyOrganisationFields[0].fieldName = nameof(organisation.Name);
            dirtyOrganisationFields[0].isDirty = false;

            //organisation.FetchEntityByPrimaryKey(organisationTableScheme, 1);
            organisation.IsNew = false;
            organisation.IsDirtyList = dirtyOrganisationFields;
            organisation.OriginalFetchedValue = organisation.ShallowCopy();

            user.IsNew = false;
            user.Password = "Test";
            user.Organisation = organisation;
            user.Save();

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicUpdate_Join_Old()
        {
            var expectedQuery = "UPDATE [U] SET [U].[PASSWORD] = 'Test' FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM1);";
            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User();

            var tableScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password),
                nameof(user.Organisation)
            };

            (string fieldName, bool isDirty)[] dirtyFields = new (string fieldName, bool isDirty)[tableScheme.Count - user.PrimaryKey.Count];
            dirtyFields[0].fieldName = nameof(user.Username);
            dirtyFields[0].isDirty = false;

            dirtyFields[1].fieldName = nameof(user.Password);
            dirtyFields[1].isDirty = true;

            dirtyFields[2].fieldName = nameof(user.Organisation);
            dirtyFields[2].isDirty = false;

            user.MutableTableScheme = tableScheme;
            user.IsDirtyList = dirtyFields;
            user.OriginalFetchedValue = user.ShallowCopy();

            var organisation = new Organisation()
            {
                Name = "Unit"
            };

            var organisationTableScheme = new List<string>
            {
                nameof(organisation.Id),
                nameof(organisation.Name)
            };

            (string fieldName, bool isDirty)[] dirtyOrganisationFields = new (string fieldName, bool isDirty)[organisationTableScheme.Count - organisation.PrimaryKey.Count];
            dirtyOrganisationFields[0].fieldName = nameof(organisation.Name);
            dirtyOrganisationFields[0].isDirty = false;

            organisation.MutableTableScheme = organisationTableScheme;
            //organisation.FetchEntityByPrimaryKey(organisationTableScheme, 1);
            organisation.IsNew = false;
            organisation.IsDirtyList = dirtyOrganisationFields;
            organisation.OriginalFetchedValue = organisation.ShallowCopy();

            user.Organisation = organisation;

            //user.FetchEntityByPrimaryKey(tableScheme, 1);
            user.Password = "Test";
            user.IsNew = false;
            user.OriginalFetchedValue = user.ShallowCopy();
            user.Save();

            Assert.AreEqual(expectedOrganisationQuery, organisation.ExecutedQuery);
            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicUpdate_JoinDirty_Old()
        {
            var expectedQuery = "UPDATE [O] SET [O].[NAME] = 'Test' FROM [dbo].[Organisations] AS [O] WHERE ([O].[Id] = @PARAM1); UPDATE [U] SET [U].[PASSWORD] = 'qwerty', [U].[ORGANISATION] = @PARAM1 FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM2);";

            var user = new User();

            var tableUserScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password),
                nameof(user.Organisation)
            };

            (string fieldName, bool isDirty)[] dirtyUserFields = new (string fieldName, bool isDirty)[tableUserScheme.Count - user.PrimaryKey.Count];
            dirtyUserFields[0].fieldName = nameof(user.Username);
            dirtyUserFields[0].isDirty = false;

            dirtyUserFields[1].fieldName = nameof(user.Password);
            dirtyUserFields[1].isDirty = true;

            dirtyUserFields[2].fieldName = nameof(user.Organisation);
            dirtyUserFields[2].isDirty = true;

            (string fieldName, bool isDirty)[] dirtyOriginalUserFields = new (string fieldName, bool isDirty)[tableUserScheme.Count - user.PrimaryKey.Count];
            dirtyOriginalUserFields[0].fieldName = nameof(user.Username);
            dirtyOriginalUserFields[0].isDirty = false;

            dirtyOriginalUserFields[1].fieldName = nameof(user.Password);
            dirtyOriginalUserFields[1].isDirty = false;

            dirtyOriginalUserFields[2].fieldName = nameof(user.Organisation);
            dirtyOriginalUserFields[2].isDirty = false;

            user.Password = "qwerty";

            var organisation = new Organisation()
            {
                Name = "Test"
            };

            var organisationTableScheme = new List<string>
            {
                nameof(organisation.Id),
                nameof(organisation.Name)
            };

            (string fieldName, bool isDirty)[] dirtyOrganisationFields = new (string fieldName, bool isDirty)[organisationTableScheme.Count - organisation.PrimaryKey.Count];
            dirtyOrganisationFields[0].fieldName = nameof(organisation.Name);
            dirtyOrganisationFields[0].isDirty = false;

            organisation.MutableTableScheme = organisationTableScheme;
            //organisation.FetchEntityByPrimaryKey(organisationTableScheme, 1);
            organisation.IsDirtyList = dirtyOrganisationFields;
            organisation.OriginalFetchedValue = organisation.ShallowCopy();

            user.Organisation = organisation;

            var originalUser = user.ShallowCopy() as User;
            originalUser.IsNew = false;
            originalUser.IsDirtyList = dirtyOriginalUserFields;
            originalUser.Password = "password";
           // user.FetchEntityByPrimaryKey(tableUserScheme, 1);
            user.IsDirtyList = dirtyUserFields;
            user.IsNew = false;
            user.OriginalFetchedValue = originalUser;
            user.Save();

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }


        [Test]
        public void BasicUpdate_JoinInsert_Old()
        {
            var expectedUserOriginalQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOrganisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES('Unit'); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var expectedUserQuery = "UPDATE [U] SET [U].[PASSWORD] = 'Test', [O].[NAME] = 'Unit' FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM1);";

            var user = new User();

            var tableScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password),
                nameof(user.Organisation)
            };

            (string fieldName, bool isDirty)[] dirtyFields = new (string fieldName, bool isDirty)[tableScheme.Count - user.PrimaryKey.Count];
            dirtyFields[0].fieldName = nameof(user.Username);
            dirtyFields[0].isDirty = false;

            dirtyFields[1].fieldName = nameof(user.Password);
            dirtyFields[1].isDirty = true;

            dirtyFields[2].fieldName = nameof(user.Organisation);
            dirtyFields[2].isDirty = true;

            user.MutableTableScheme = tableScheme;
            user.IsDirtyList = dirtyFields;
            user.OriginalFetchedValue = user.ShallowCopy();

            var organisation = new Organisation()
            {
                Name = "Unit"
            };

            var organisationTableScheme = new List<string>
            {
                nameof(organisation.Id),
                nameof(organisation.Name)
            };

            (string fieldName, bool isDirty)[] dirtyOrganisationFields = new (string fieldName, bool isDirty)[organisationTableScheme.Count - organisation.PrimaryKey.Count];
            dirtyOrganisationFields[0].fieldName = nameof(organisation.Name);
            dirtyOrganisationFields[0].isDirty = true;

            organisation.MutableTableScheme = organisationTableScheme;
            organisation.IsDirtyList = dirtyOrganisationFields;
            user.Organisation = organisation;

            //user.FetchEntityByPrimaryKey(tableScheme, 1);
            user.Password = "Test";
            user.IsNew = false;
            user.Save();

            Assert.AreEqual(expectedUserOriginalQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(expectedOrganisationQuery, organisation.ExecutedQuery);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }
    }
}