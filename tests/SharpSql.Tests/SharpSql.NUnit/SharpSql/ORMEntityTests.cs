using NUnit.Framework;
using SharpSql.Attributes;
using SharpSql.UnitTests;
using System;
using System.Linq;

/*
    Important note: When creating memory entity tables, make sure legitamate data is being used from
    a SQL Server to correctly simulate what happens. The entity objects will actually fetch the data
    from the xml files.
 */
namespace SharpSql.NUnit
{
    [TestFixture]
    public class SharpSqlEntityTests
    {
        [Test]
        public void Fetch()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(1);

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
        }

        [Test]
        public void Fetch_Dirty()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(4)
            {
                Password = "qwerty"
            };

            // User object
            Assert.AreEqual(user.IsDirty, true);
            Assert.AreEqual(user.IsNew, false);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
        }

        [Test]
        public void Fetch_Join()
        {
            var expectedUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(1, x => x.Organisation.Left());

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 1);
            Assert.NotNull(user.Relations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.Relations.OfType<Organisation>().FirstOrDefault());

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations.Count == 0);

            // User query
            Assert.AreEqual(user.ExecutedQuery, expectedUserQuery);
        }

        [Test]
        public void Fetch_Join_Dirty()
        {
            var expectedUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(1, x => x.Organisation.Left());
            user.Organisation.Name = "Unit Test";

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 1);
            Assert.NotNull(user.Relations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.Relations.OfType<Organisation>().FirstOrDefault());

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.AreNotEqual(user.Organisation.Name, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations.Count == 0);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }

        [Test]
        public void Fetch_Join_New()
        {
            // @Todo, @Research: the TOP (1) has been removed for ManyTomany? We think?
            // But shouldn't we only remove this if there is a ManyTomany?
            // Doesn't this always cause a table scan?
            var expectedInitialUserQuery  = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";

            var expectedOrganisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES(@PARAM1); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var expectedOriginalOrganisationQuery = "INITIALISED THROUGH PARENT";

            var user = new User(1, x => x.Organisation.Left())
            {
                Organisation = new Organisation() { Name = "Unit Test" }
            };

            // Initial User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);

            Assert.AreEqual(true, user.IsDirty);

            user.Save();

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);
            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 1);
            Assert.NotNull(user.Relations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.Relations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedUserQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(expectedInitialUserQuery, user.OriginalFetchedValue.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations.Count == 0);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalOrganisationQuery, user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
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

            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(true, user.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);

            user.Save();

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.Relations.Count == 0);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.IsNull(user.Organisation);
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

            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(true, user.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);

            user.Save();

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.Relations.Count == 1);
            Assert.NotNull(user.Relations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.AreEqual(user.Organisation, user.Organisation.OriginalFetchedValue);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Insert_Join_New()
        {
            var expectedUserQuery = "INSERT INTO [DBO].[USERS] ([DBO].[USERS].[USERNAME], [DBO].[USERS].[PASSWORD], [DBO].[USERS].[ORGANISATION], [DBO].[USERS].[DATECREATED], [DBO].[USERS].[DATELASTMODIFIED]) VALUES(@PARAM1, @PARAM2, @PARAM3, @PARAM4, @PARAM5); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var expectedOrganisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES(@PARAM1); SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var user = new User()
            {
                Username = "Unit",
                Password = "Test",
                Organisation = new Organisation() { Name = "The Test Organisation" }
            };

            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(true, user.IsNew);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);

            Assert.IsNull(user.OriginalFetchedValue);
            Assert.IsNull(user.Organisation.OriginalFetchedValue);

            user.Save();

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsNotNull(user);
            Assert.IsTrue(user.Relations.Count == 1);
            Assert.NotNull(user.Relations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void UpdateDirectById()
        {
            var expectedUpdateQuery = "UPDATE [U] SET [U].[PASSWORD] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";
            var user = DatabaseUtilities.Update<User>(1, (x => x.Password, "UnitTest password"));

            Assert.AreEqual(expectedUpdateQuery, user.ExecutedQuery);

            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("UnitTest password", user.Password);

            // Initially the ObjectState is Record because no User entity is provided, but once it's updated it changes to Saved.
            Assert.AreEqual(ObjectState.Saved, user.ObjectState);
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
        }

        [Test]
        public void UpdateDirectByEntity()
        {
            var expectedUpdateQuery = "UPDATE [U] SET [U].[PASSWORD] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";
            var expectedOriginalQuery = "UPDATE [U] SET [U].[PASSWORD] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var tempUser = new User(1);
            var user = DatabaseUtilities.Update(tempUser, (x => x.Password, "UnitTest password"));

            Assert.AreEqual(expectedUpdateQuery, user.ExecutedQuery);

            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("UnitTest password", user.Password);

            // Initially the ObjectState is Fetched because a user is provided, but once it's updated it changes to Saved.
            Assert.AreEqual(ObjectState.Saved, user.ObjectState);
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);

            Assert.AreEqual(expectedOriginalQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(expectedInitialUserQuery, user.OriginalFetchedValue.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(ObjectState.NewRecord, user.OriginalFetchedValue.ObjectState);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsMarkedAsDeleted);
        }

        [Test]
        public void DeleteDirectById()
        {
            var expectedUpdateQuery = "DELETE FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var user = DatabaseUtilities.Delete<User>(1);

            Assert.AreEqual(expectedUpdateQuery, user.ExecutedQuery);
            Assert.AreEqual(ObjectState.Deleted, user.ObjectState);
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.AreEqual(true, user.IsMarkedAsDeleted);

            Assert.IsNull(user.OriginalFetchedValue);
        }

        [Test]
        public void DeleteDirectByEntity()
        {
            var expectedUpdateQuery = "DELETE FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOriginalQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = DatabaseUtilities.Delete(new User(1));

            Assert.AreEqual(expectedUpdateQuery, user.ExecutedQuery);
            Assert.AreEqual(ObjectState.Deleted, user.ObjectState);
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.AreEqual(true, user.IsMarkedAsDeleted);

            Assert.IsNotNull(user.OriginalFetchedValue);

            Assert.AreEqual(expectedOriginalQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(ObjectState.OriginalFetchedValue, user.OriginalFetchedValue.ObjectState);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsMarkedAsDeleted);
        }

        [Test]
        public void Delete()
        {
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedDeleteQuery = "DELETE FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(1);

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.AreEqual(ObjectState.Fetched, user.ObjectState);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            // User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);

            // Organisation is not null, but no join is provided so should be null.
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);

            user.Delete();

            // User object
            Assert.AreEqual(expectedDeleteQuery, user.ExecutedQuery);
            Assert.AreEqual(ObjectState.Deleted, user.ObjectState);
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.AreEqual(true, user.IsMarkedAsDeleted);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);

            // User query
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // Organisation is not null, but no join is provided so should be null.
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);
        }

        [Test]
        public void Update()
        {
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[PASSWORD] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(2)
            {
                Password = "UnitTest"
            };

            // Initial User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            user.Save();

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.IsNull(user.Organisation);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation == null);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);
        }

        [Test]
        public void Update_Join()
        {
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(2)
            {
                Organisation = new Organisation(1)
            };

            // Initial User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation == null);
            Assert.IsEmpty(user.OriginalFetchedValue.Relations);

            user.Save();

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.IsNotNull(user.Organisation);
            Assert.IsTrue(user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation == null);

            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsNotEmpty(user.OriginalFetchedValue.Relations);
        }

        [Test]
        public void Update_JoinInsert()
        {
            var expectedOriginalUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedInitialUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";

            var expectedOrganisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES(@PARAM1); SELECT CAST(SCOPE_IDENTITY() AS INT);";

            // The original Organisation object for User 2.
            var expectedOriginalUserOrganisationQuery = "INITIALISED THROUGH PARENT";

            var user = new User(2, x => x.Organisation.Left())
            {
                Organisation = new Organisation()
                {
                    Name = "UnitTest"
                }
            };

            // Initial User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);

            Assert.AreEqual(false, user.IsNew);
            Assert.AreEqual(true, user.Organisation.IsNew);

            Assert.IsNull(user.Organisation.OriginalFetchedValue);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation.OriginalFetchedValue);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(expectedOriginalUserOrganisationQuery, user.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);

            user.Save();

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);
            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 1);
            Assert.NotNull(user.Relations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.Relations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.AreEqual(ObjectState.Saved, user.Organisation.ObjectState);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsEmpty(user.Organisation.Relations);
            Assert.IsEmpty(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation);
            // Originally it was null, but since it's been saved it has a original fetched value to track changes.
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation.OriginalFetchedValue);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserOrganisationQuery, user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
        }

        [Test]
        public void Update_DirtyJoin()
        {
            var expectedOriginalUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedInitialUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";

            var expectedOrganisationQuery = "UPDATE [O] SET [O].[NAME] = @PARAM1 FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM2);";

            // The original Organisation object for User 2.
            var expectedOriginalUserOrganisationQuery = "INITIALISED THROUGH PARENT";
            
            // The original Organisation object for Orgnisation 1.
            var expectedOriginalNewOrganisationQuery = "INITIALISED THROUGH PARENT";

            var user = new User(2, x => x.Organisation.Left());
            user.Organisation.Name = "Unit Test";

            // Initial User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(expectedOriginalUserOrganisationQuery, user.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalNewOrganisationQuery, user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().ExecutedQuery);

            user.Save();

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);
            // The user overrides the Save() method, here the DateLastModified is set.
            // Once the Save() is done executing, it should no longer be dirty.
            Assert.AreEqual(false, user.IsFieldDirty(nameof(user.DateLastModified)));
            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 1);
            Assert.NotNull(user.Relations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.Relations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations.Count == 0);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().OriginalFetchedValue);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserOrganisationQuery, user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalNewOrganisationQuery, user.Organisation.OriginalFetchedValue.OriginalFetchedValue.ValueAs<Organisation>().ExecutedQuery);
        }

        [Test]
        public void Insert_JoinInsert_DisableChangeTracking()
        {
            var userQuery = "INSERT INTO [DBO].[USERS] ([DBO].[USERS].[USERNAME], [DBO].[USERS].[PASSWORD], [DBO].[USERS].[ORGANISATION], [DBO].[USERS].[DATECREATED], [DBO].[USERS].[DATELASTMODIFIED]) VALUES(@PARAM1, @PARAM2, @PARAM3, @PARAM4, @PARAM5); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var organisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES(@PARAM1); SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var user = new User(true)
            {
                Username = "Name",
                Password = "Password",
                Organisation = new Organisation(true)
                {
                    Name = "UnitTest"
                }
            };

            // User
            Assert.IsNotNull(user.Username);
            Assert.IsNotNull(user.Password);
            Assert.IsNotNull(user.Organisation);
            
            Assert.IsNull(user.DateCreated);
            Assert.IsNull(user.DateLastModified);
            Assert.IsNull(user.OriginalFetchedValue);
            Assert.IsNull(user.Roles);

            Assert.IsTrue(user.IsNew);
            Assert.IsTrue(user.DisableChangeTracking);
            Assert.IsTrue(user.IsAutoIncrement);
            Assert.IsTrue(user.IsDirty);

            Assert.IsFalse(user.IsMarkedAsDeleted);

            Assert.IsEmpty(user.ExecutedQuery);
            Assert.IsEmpty(user.Relations);

            Assert.AreEqual(ObjectState.New, user.ObjectState);

            // Organisation
            Assert.IsNotNull(user.Organisation.Name);

            Assert.IsNull(user.Organisation.OriginalFetchedValue);

            Assert.IsTrue(user.Organisation.IsNew);
            Assert.IsTrue(user.Organisation.DisableChangeTracking);
            Assert.IsTrue(user.Organisation.IsAutoIncrement);
            Assert.IsTrue(user.Organisation.IsDirty);

            Assert.IsFalse(user.Organisation.IsMarkedAsDeleted);

            Assert.IsEmpty(user.Organisation.ExecutedQuery);
            Assert.IsEmpty(user.Organisation.Relations);

            Assert.AreEqual(ObjectState.New, user.Organisation.ObjectState);

            user.Save();

            Assert.AreEqual(userQuery, user.ExecutedQuery);
            Assert.AreEqual(organisationQuery, user.Organisation.ExecutedQuery);

            // User
            Assert.IsNotNull(user.Username);
            Assert.IsNotNull(user.Password);
            Assert.IsNotNull(user.Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.IsNotNull(user.OriginalFetchedValue);

            Assert.IsNull(user.Roles);

            Assert.IsTrue(user.DisableChangeTracking);
            Assert.IsTrue(user.IsAutoIncrement);

            Assert.IsFalse(user.IsDirty);
            Assert.IsFalse(user.IsNew);
            Assert.IsFalse(user.IsMarkedAsDeleted);

            Assert.IsNotEmpty(user.Relations);

            Assert.AreEqual(ObjectState.Saved, user.ObjectState);

            // Organisation
            Assert.IsNotNull(user.Organisation.Name);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);

            Assert.IsTrue(user.Organisation.DisableChangeTracking);
            Assert.IsTrue(user.Organisation.IsAutoIncrement);
            
            Assert.IsFalse(user.Organisation.IsDirty);
            Assert.IsFalse(user.Organisation.IsNew);
            Assert.IsFalse(user.Organisation.IsMarkedAsDeleted);

            Assert.IsEmpty(user.Organisation.Relations);

            Assert.AreEqual(ObjectState.Saved, user.Organisation.ObjectState);

            // user.OriginalFetchedValue.User
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Username);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Password);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().DateCreated);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().DateLastModified);

            Assert.IsNull(user.OriginalFetchedValue.OriginalFetchedValue);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Roles);

            Assert.IsTrue(user.OriginalFetchedValue.DisableChangeTracking);
            Assert.IsTrue(user.OriginalFetchedValue.IsAutoIncrement);

            Assert.IsFalse(user.OriginalFetchedValue.IsDirty);
            Assert.IsFalse(user.OriginalFetchedValue.IsNew);
            Assert.IsFalse(user.OriginalFetchedValue.IsMarkedAsDeleted);

            Assert.IsNotEmpty(user.OriginalFetchedValue.Relations);

            Assert.AreEqual(userQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(ObjectState.NewRecord, user.OriginalFetchedValue.ObjectState);

            // user.OriginalFetchedValue.Organisation
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation.Name);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation.OriginalFetchedValue);

            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.DisableChangeTracking);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.IsAutoIncrement);

            Assert.IsFalse(user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.IsFalse(user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsFalse(user.OriginalFetchedValue.ValueAs<User>().Organisation.IsMarkedAsDeleted);

            Assert.IsEmpty(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations);

            Assert.AreEqual(organisationQuery, user.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
            Assert.AreEqual(ObjectState.Saved, user.OriginalFetchedValue.ValueAs<User>().Organisation.ObjectState);

            // User.Organisation.OriginalFetchedValue
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().Name);

            Assert.IsNull(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().OriginalFetchedValue);

            Assert.IsTrue(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().DisableChangeTracking);
            Assert.IsTrue(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().IsAutoIncrement);

            Assert.IsFalse(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().IsDirty);
            Assert.IsFalse(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().IsNew);
            Assert.IsFalse(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().IsMarkedAsDeleted);

            Assert.IsEmpty(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().Relations);

            Assert.AreEqual(organisationQuery, user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().ExecutedQuery);
            Assert.AreEqual(ObjectState.NewRecord, user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().ObjectState);
        }

        [Test]
        public void Update_JoinInsert_DisableChangeTracking()
        {
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM2);";
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOrganisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES(@PARAM1); SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var user = new User(2, true)
            {
                Organisation = new Organisation()
                {
                    Name = "UnitTest"
                }
            };

            // Initial User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);

            // User object
            Assert.AreEqual(true, user.IsDirty);
            // Organisation is also currently dirty.
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            // Once it's saved, it's no longer new.
            Assert.AreEqual(true, user.Organisation.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);
            // Because no Organisation was joined and the current Organisation field is new, the
            // current user doesn't have any relations, until the object is saved.
            Assert.IsTrue(user.Relations.Count == 0);

            user.Save();

            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.IsDirty);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.AreEqual(ObjectState.Saved, user.Organisation.ObjectState);
            Assert.AreEqual(ObjectState.NewRecord, user.Organisation.OriginalFetchedValue.ObjectState);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsNotNull(user.Organisation.ValueAs<Organisation>().OriginalFetchedValue);
            Assert.IsNull(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().OriginalFetchedValue);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Update_DirtyJoin_DisableChangeTracking_WithoutChanges()
        {
            var expectedOriginalUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedInitialUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";

            var expectedOriginalOrganisationQuery = "INITIALISED THROUGH PARENT";

            var user = new User(2, x => x.Organisation.Left())
            {
                Organisation = new Organisation(1, true)
            };

            // Initial User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.DisableChangeTracking);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);

            user.Save();

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);
            // The user overrides the Save() method, here the DateLastModified is set.
            // Once the Save() is done executing, it should no longer be dirty.
            Assert.AreEqual(false, user.IsFieldDirty(nameof(user.DateLastModified)));
            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 1);
            Assert.NotNull(user.Relations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.Relations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.OriginalFetchedValue.ExecutedQuery);

            // DisableChangeTracking
            Assert.AreEqual(false, user.DisableChangeTracking);
            Assert.AreEqual(true, user.Organisation.DisableChangeTracking);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().DisableChangeTracking);
            Assert.AreEqual(false, user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation.DisableChangeTracking);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations.Count == 0);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNull(user.Organisation.OriginalFetchedValue);

            // Organisation query
            Assert.AreEqual(expectedOriginalOrganisationQuery, user.OriginalFetchedValue.ValueAs<User>().OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
        }

        [Test]
        public void Update_DirtyJoin_DisableChangeTracking_WithChanges()
        {
            var expectedOriginalUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedInitialUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";

            var expectedOrganisationQueryBeforeChanges = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";
            var expectedOrganisationQueryAfterChanges = "UPDATE [O] SET [O].[NAME] = @PARAM1 FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM2);";
            var expectedOriginalOrganisationQuery = "INITIALISED THROUGH PARENT";

            var user = new User(2, x => x.Organisation.Left())
            {
                Organisation = new Organisation(1, true)
            };

            // Initial User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.DisableChangeTracking);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(expectedOriginalOrganisationQuery, user.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);

            user.Save();

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);
            // The user overrides the Save() method, here the DateLastModified is set.
            // Once the Save() is done executing, it should no longer be dirty.
            Assert.AreEqual(false, user.IsFieldDirty(nameof(user.DateLastModified)));
            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 1);
            Assert.NotNull(user.Relations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.Relations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.OriginalFetchedValue.ExecutedQuery);

            // DisableChangeTracking
            Assert.AreEqual(false, user.DisableChangeTracking);
            Assert.AreEqual(true, user.Organisation.DisableChangeTracking);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().DisableChangeTracking);
            Assert.AreEqual(false, user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation.DisableChangeTracking);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations.Count == 0);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNull(user.Organisation.OriginalFetchedValue);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQueryBeforeChanges, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalOrganisationQuery, user.OriginalFetchedValue.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);

            // Making changes
            user.Organisation.Name = "WithChanges";
            Assert.AreEqual(false, user.Organisation.IsDirty);

            // Making changes known to SharpSql
            user.Organisation.MarkFieldsAsDirty(nameof(user.Organisation.Name));
            Assert.AreEqual(true, user.Organisation.IsDirty);

            user.Save();
            Assert.AreEqual(expectedOrganisationQueryAfterChanges, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void DisableChangeTracking_WithChanges()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(2, true);

            Assert.AreEqual("password", user.Password);
            Assert.AreEqual(false, user.IsDirty);

            user.Password = "Password";
            Assert.AreEqual(false, user.IsDirty);

            user.MarkFieldsAsDirty(nameof(user.Password));
            Assert.AreEqual(true, user.IsDirty);

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsNull(user.OriginalFetchedValue);

            Assert.AreEqual(2, user.Id);
            Assert.AreEqual("Clarence", user.Username);
            Assert.AreEqual("Password", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateLastModified);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }

        [Test]
        public void DisableChangeTracking_WithoutChanges()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(2, true);

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsNull(user.OriginalFetchedValue);

            Assert.AreEqual(2, user.Id);
            Assert.AreEqual("Clarence", user.Username);
            Assert.AreEqual("password", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateLastModified);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
        }

        [Test]
        public void DisableChangeTracking_WithSavedChanges()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedSaveQuery = "UPDATE [U] SET [U].[PASSWORD] = @PARAM1 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM2);";

            var user = new User(2, true);

            Assert.AreEqual("password", user.Password);
            Assert.AreEqual(false, user.IsDirty);

            user.Password = "Password";
            Assert.AreEqual(false, user.IsDirty);

            user.MarkFieldsAsDirty(nameof(user.Password));
            Assert.AreEqual(true, user.IsDirty);

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsNull(user.OriginalFetchedValue);

            Assert.AreEqual(2, user.Id);
            Assert.AreEqual("Clarence", user.Username);
            Assert.AreEqual("Password", user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNotNull(user.DateCreated);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateCreated);
            Assert.IsNotNull(user.DateLastModified);
            Assert.AreEqual(DateTime.Parse("2020-07-23T16:50:38.213"), user.DateLastModified);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            user.Save();

            // Save query
            Assert.AreEqual(expectedSaveQuery, user.ExecutedQuery);
        }

        [Test]
        public void MultiplePrimaryKeys()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERROLES] AS [U] WHERE (([U].[USERID] = @PARAM1) AND ([U].[ROLEID] = @PARAM2));";

            var userRole = new UserRole(1, 1);

            Assert.AreEqual(1, userRole.Column_UserId);
            Assert.AreEqual(1, userRole.Column_RoleId);

            Assert.IsTrue(userRole.GetType().GetProperty(nameof(userRole.Column_UserId)).GetCustomAttributes(typeof(SharpSqlColumnAttribute), false).Length == 1);
            Assert.IsTrue(userRole.GetType().GetProperty(nameof(userRole.Column_RoleId)).GetCustomAttributes(typeof(SharpSqlColumnAttribute), false).Length == 1);

            Assert.AreEqual(expectedQuery, userRole.ExecutedQuery);
        }

        [Test, SharpSqlUnitTest("ManyToManyUserRoles", typeof(UserRole), "ManyToManyRoles", typeof(Role))]
        public void ManyToMany()
        {
            var expectedQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[USERROLES] AS [UU] ON [U].[ID] = [UU].[USERID] LEFT JOIN [DBO].[ROLES] AS [R] ON [UU].[ROLEID] = [R].[ID] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(1, x => x.Roles.Left());

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);

            // Organisation object
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);

            // Roles (many-to-many)
            Assert.IsNotNull(user.Roles);
            Assert.IsTrue(user.Roles.Count == 2);
            Assert.IsTrue(user.Roles[0].Description == "Admin");
            Assert.IsTrue(user.Roles[1].Description == "Moderator");

            Assert.AreEqual(expectedQuery, user.ExecutedQuery);
        }

        // Write a unit test that checks the MutableTableSchema and TableSchema for both the Organisation and Token objects.
        // Tokens have multiple PK's, with auto increment false. And Organisations have a single PK with auto increment.
        // Compare the mutable vs normal schema

        // Write a unit tests to check the PK's for both Organisations as Tokens.

        //[Test]
        //public void InsertDirectById() {}
        //[Test]
        //public void InsertDirectByEntity() {}

        //[Test]
        // JsonDeserializeTest
        //[Test]
        // JsonSerializeTest
    }
}