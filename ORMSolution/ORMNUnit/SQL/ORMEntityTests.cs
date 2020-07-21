using NUnit.Framework;
using ORM;
using ORM.Attributes;
using ORMFakeDAL;
using System;
using System.Collections.Generic;

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
            var expectedQuery = "INSERT INTO [DBO].[USERS] ([DBO].[USERS].[USERNAME], [DBO].[USERS].[PASSWORD], [DBO].[USERS].[ORGANISATION]) VALUES('Unit', 'Test', NULL);";

            var user = new User()
            {
                Username = "Unit",
                Password = "Test",
                Organisation = null // new Organisation(1) // Todo: add a unit test for this case.
            };

            var tableScheme = new List<string>
            {
                nameof(user.Id),
                nameof(user.Username),
                nameof(user.Password),
                nameof(user.Organisation)
            };

            user.TableScheme = tableScheme;
            user.DisableChangeTracking = true;

            user.Save();

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
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

            user.TableScheme = tableScheme;
            user.IsDirtyList = dirtyFields;
            user.OriginalFetchedValue = (User)Activator.CreateInstance(user.GetType());

            user.FetchEntityById<Users, User>(1, tableScheme);
            user.Password = "Test";

            user.Save();

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }
    }
}