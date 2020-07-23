using NUnit.Framework;
using ORMFakeDAL;
using System;
using System.Collections.Generic;

namespace ORMNUnit
{
    [TestFixture]
    public class ORMEntityTests
    {
        [Test]
        public void BasicFetch()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User();

            var tableScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password)
            };

            user.FetchEntityById<Users, User>(1, tableScheme);

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicFetch_Join()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";

            var user = new User();

            var tableScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password),
                nameof(user.Organisation)
            };

            user.FetchEntityById<Users, User>(1, tableScheme);

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicInsert()
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
        public void BasicInsert_Join()
        {
            var expectedUserQuery         = "INSERT INTO [DBO].[USERS] ([DBO].[USERS].[USERNAME], [DBO].[USERS].[PASSWORD], [DBO].[USERS].[ORGANISATION]) VALUES('Unit', 'Test', '1'); SELECT CAST(SCOPE_IDENTITY() AS INT);";
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

            organisation.FetchEntityById<Organisations, Organisation>(1, organisationTableScheme);

            user.Organisation = organisation;
            user.Save();

            Assert.AreEqual(expectedOrganisationQuery, organisation.ExecutedQuery);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicUpdate()
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

            (string fieldName, bool isDirty)[] dirtyFields = new (string fieldName, bool isDirty)[tableScheme.Count - 1];
            dirtyFields[0].fieldName = nameof(user.Username);
            dirtyFields[0].isDirty = false;

            dirtyFields[1].fieldName = nameof(user.Password);
            dirtyFields[1].isDirty = true;

            dirtyFields[2].fieldName = nameof(user.Organisation);
            dirtyFields[2].isDirty = false;

            user.MutableTableScheme = tableScheme;
            user.IsDirtyList = dirtyFields;
            user.OriginalFetchedValue = (User)Activator.CreateInstance(user.GetType());

            user.FetchEntityById<Users, User>(1, tableScheme);
            user.Password = "Test";
            user.Save();

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicUpdate_Join()
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

            (string fieldName, bool isDirty)[] dirtyFields = new (string fieldName, bool isDirty)[tableScheme.Count - 1];
            dirtyFields[0].fieldName = nameof(user.Username);
            dirtyFields[0].isDirty = false;

            dirtyFields[1].fieldName = nameof(user.Password);
            dirtyFields[1].isDirty = true;

            dirtyFields[2].fieldName = nameof(user.Organisation);
            dirtyFields[2].isDirty = false;

            user.MutableTableScheme = tableScheme;
            user.IsDirtyList = dirtyFields;
            user.OriginalFetchedValue = (User)Activator.CreateInstance(user.GetType());

            user.FetchEntityById<Users, User>(1, tableScheme);
            user.Password = "Test";

            var organisation = new Organisation()
            {
                Name = "Unit"
            };

            var organisationTableScheme = new List<string>
            {
                nameof(organisation.Id),
                nameof(organisation.Name)
            };

            (string fieldName, bool isDirty)[] dirtyOrganisationFields = new (string fieldName, bool isDirty)[organisationTableScheme.Count - 1];
            dirtyOrganisationFields[0].fieldName = nameof(organisation.Name);
            dirtyOrganisationFields[0].isDirty = false;

            organisation.IsDirtyList = dirtyOrganisationFields;
            organisation.FetchEntityById<Organisations, Organisation>(1, organisationTableScheme);

            user.Organisation = organisation;
            user.Save();

            Assert.AreEqual(expectedOrganisationQuery, organisation.ExecutedQuery);
            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        [Test]
        public void BasicUpdate_JoinDirty()
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

            (string fieldName, bool isDirty)[] dirtyUserFields = new (string fieldName, bool isDirty)[tableUserScheme.Count - 1];
            dirtyUserFields[0].fieldName = nameof(user.Username);
            dirtyUserFields[0].isDirty = false;

            dirtyUserFields[1].fieldName = nameof(user.Password);
            dirtyUserFields[1].isDirty = true;

            dirtyUserFields[2].fieldName = nameof(user.Organisation);
            dirtyUserFields[2].isDirty = true;

            user.FetchEntityById<Users, User>(1, tableUserScheme);
            user.IsDirtyList = dirtyUserFields;

            (string fieldName, bool isDirty)[] dirtyOriginalUserFields = new (string fieldName, bool isDirty)[tableUserScheme.Count - 1];
            dirtyOriginalUserFields[0].fieldName = nameof(user.Username);
            dirtyOriginalUserFields[0].isDirty = false;

            dirtyOriginalUserFields[1].fieldName = nameof(user.Password);
            dirtyOriginalUserFields[1].isDirty = false;

            dirtyOriginalUserFields[2].fieldName = nameof(user.Organisation);
            dirtyOriginalUserFields[2].isDirty = false;

            var originalUser = new User();
            originalUser.FetchEntityById<Users, User>(1, tableUserScheme);
            originalUser.IsDirtyList = dirtyOriginalUserFields;
            originalUser.Password = "password";

            user.OriginalFetchedValue = originalUser;
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

            (string fieldName, bool isDirty)[] dirtyOrganisationFields = new (string fieldName, bool isDirty)[organisationTableScheme.Count - 1];
            dirtyOrganisationFields[0].fieldName = nameof(organisation.Name);
            dirtyOrganisationFields[0].isDirty = false;

            organisation.MutableTableScheme = organisationTableScheme;
            organisation.IsDirtyList = dirtyOrganisationFields;
            user.Organisation = organisation;

            user.Save();

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }


        [Test]
        public void BasicUpdate_JoinInsert()
        {
            var expectedUserOriginalQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
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

            (string fieldName, bool isDirty)[] dirtyFields = new (string fieldName, bool isDirty)[tableScheme.Count - 1];
            dirtyFields[0].fieldName = nameof(user.Username);
            dirtyFields[0].isDirty = false;

            dirtyFields[1].fieldName = nameof(user.Password);
            dirtyFields[1].isDirty = true;

            dirtyFields[2].fieldName = nameof(user.Organisation);
            dirtyFields[2].isDirty = true;

            user.MutableTableScheme = tableScheme;
            user.IsDirtyList = dirtyFields;
            user.OriginalFetchedValue = (User)Activator.CreateInstance(user.GetType());

            var organisation = new Organisation()
            {
                Name = "Unit"
            };

            var organisationTableScheme = new List<string>
            {
                nameof(organisation.Id),
                nameof(organisation.Name)
            };

            (string fieldName, bool isDirty)[] dirtyOrganisationFields = new (string fieldName, bool isDirty)[organisationTableScheme.Count - 1];
            dirtyOrganisationFields[0].fieldName = nameof(organisation.Name);
            dirtyOrganisationFields[0].isDirty = true;

            organisation.MutableTableScheme = organisationTableScheme;
            organisation.IsDirtyList = dirtyOrganisationFields;
            user.Organisation = organisation;

            user.FetchEntityById<Users, User>(1, tableScheme);
            user.Password = "Test";

            user.Save();

            Assert.AreEqual(expectedUserOriginalQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(expectedOrganisationQuery, organisation.ExecutedQuery);
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }
    }
}