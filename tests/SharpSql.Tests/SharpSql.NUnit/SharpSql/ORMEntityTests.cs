using NUnit.Framework;
using SharpSql.Attributes;
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
    public class ORMEntityTests
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

            // Organisation is not null, but no join is provided so should be null.
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

            // Organisation object - null for User with Id 4.
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
            var expectedInitialUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedOriginalUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";

            var expectedOrganisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES(@PARAM1); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var expectedOriginalOrganisationQuery = "INITIALISED THROUGH PARENT";

            var user = new User(1, x => x.Organisation.Left())
            {
                Organisation = new Organisation() { Name = "Unit Test" }
            };

            // Initial User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);

            user.Save();

            // User object
            Assert.AreEqual(true, user.IsDirty);
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
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations.Count == 0);

            // Organisation query
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

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(true, user.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);
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

            user.Save();

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(true, user.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);
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

            user.Save();

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(true, user.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);
            Assert.IsNotNull(user);
            Assert.IsTrue(user.Relations.Count == 1);
            Assert.NotNull(user.Relations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation);
            Assert.IsNull(user.Organisation.OriginalFetchedValue);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void UpdateDirectById()
        {
            var expectedUpdateQuery = "UPDATE [U] SET [U].[PASSWORD] = @PARAM1 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM2);";
            var user = DatabaseUtilities.Update<User>(1, (x => x.Password, "UnitTest password"));

            Assert.AreEqual(expectedUpdateQuery, user.ExecutedQuery);

            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("UnitTest password", user.Password);

            Assert.AreEqual(ObjectState.Record, user.ObjectState);
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);
        }

        [Test]
        public void UpdateDirectByEntity()
        {
            var expectedUpdateQuery = "UPDATE [U] SET [U].[PASSWORD] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";
            var expectedOriginalQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var tempUser = new User(1);
            var user = DatabaseUtilities.Update(tempUser, (x => x.Password, "UnitTest password"));

            Assert.AreEqual(expectedUpdateQuery, user.ExecutedQuery);

            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("UnitTest password", user.Password);

            Assert.AreEqual(ObjectState.Fetched, user.ObjectState);
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);

            Assert.AreEqual(expectedOriginalQuery, user.OriginalFetchedValue.ExecutedQuery);
            Assert.AreEqual(ObjectState.Fetched, user.OriginalFetchedValue.ObjectState);
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
            Assert.AreEqual(ObjectState.Fetched, user.OriginalFetchedValue.ObjectState);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsMarkedAsDeleted);
        }

        //[Test]
        //public void InsertDirectById() {}
        //[Test]
        //public void InsertDirectByEntity() {}

        //[Test]
        // JsonDeserializeTest
        //[Test]
        // JsonSerializeTest

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

            user.Save();

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

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

            user.Save();

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.IsNew);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.IsNotNull(user.Organisation);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation == null);

            Assert.IsTrue(user.Relations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);
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

            user.Save();

            // User object
            Assert.AreEqual(true, user.IsDirty);
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
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations.Count == 0);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNull(user.Organisation.OriginalFetchedValue);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation.OriginalFetchedValue);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserOrganisationQuery, user.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
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

            user.Save();

            // User object
            Assert.AreEqual(true, user.IsDirty);
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
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations.Count == 0);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNull(user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().OriginalFetchedValue);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserOrganisationQuery, user.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalNewOrganisationQuery, user.Organisation.OriginalFetchedValue.ValueAs<Organisation>().ExecutedQuery);
        }

        [Test]
        public void Update_JoinInsert_DisableChangeTracking()
        {
            var expectedUserQuery = "UPDATE [U] SET [U].[USERNAME] = @PARAM1, [U].[PASSWORD] = @PARAM2, [U].[ORGANISATION] = @PARAM3, [U].[DATECREATED] = @PARAM4, [U].[DATELASTMODIFIED] = @PARAM5 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM6);";
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

            user.Save();

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.Relations.Count == 1);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsNull(user.Organisation.ValueAs<Organisation>().OriginalFetchedValue);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Update_DirtyJoin_DisableChangeTracking()
        {
            var expectedOriginalUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedInitialUserQuery = "SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM3);";

            var expectedOrganisationQuery = "UPDATE [O] SET [O].[NAME] = @PARAM1 FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM2);";
            var expectedOriginalOrganisationQuery = "INITIALISED THROUGH PARENT";

            var user = new User(2, x => x.Organisation.Left())
            {
                Organisation = new Organisation(1, true)
            };

            // Initial User query
            Assert.AreEqual(expectedInitialUserQuery, user.ExecutedQuery);

            user.Save();

            // User object
            Assert.AreEqual(true, user.IsDirty);
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
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // DisableChangeTracking
            Assert.AreEqual(false, user.DisableChangeTracking);
            Assert.AreEqual(true, user.Organisation.DisableChangeTracking);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().DisableChangeTracking);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.DisableChangeTracking);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.Relations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.Relations.Count == 0);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsNull(user.Organisation.OriginalFetchedValue);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalOrganisationQuery, user.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
        }

        [Test]
        public void DisableChangeTracking()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var user = new User(2, true);

            // User object
            Assert.AreEqual(true, user.IsDirty);
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
        public void MultiplePrimaryKeys()
        {
            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERROLES] AS [U] WHERE (([U].[USERID] = @PARAM1) AND ([U].[ROLEID] = @PARAM2));";

            var userRole = new UserRole(1, 1);

            Assert.AreEqual(1, userRole.Column_UserId);
            Assert.AreEqual(1, userRole.Column_RoleId);

            Assert.IsTrue(userRole.GetType().GetProperty(nameof(userRole.Column_UserId)).GetCustomAttributes(typeof(ORMColumnAttribute), false).Length == 1);
            Assert.IsTrue(userRole.GetType().GetProperty(nameof(userRole.Column_RoleId)).GetCustomAttributes(typeof(ORMColumnAttribute), false).Length == 1);

            Assert.AreEqual(expectedQuery, userRole.ExecutedQuery);
        }

        [Test, ORMUnitTest("ManyToManyUserRoles", typeof(UserRole), "ManyToManyRoles", typeof(Role))]
        public void ManyToMany()
        {
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
        }

        // Write a unit test that checks the MutableTableSchema and TableSchema for both the Organisation and Token objects.
        // Tokens have multiple PK's, with auto increment false. And Organisations have a single PK with auto increment.
        // Compare the mutable vs normal schema

        // Write a unit tests to check the PK's for both Organisations as Tokens.
    }
}