using NUnit.Framework;
using ORMFakeDAL;
using System.Linq;

/*
    Important note: When creating memory entity tables, make sure legitamate data is being used from
    a SQL Server to correctly simulate what happens. The entity objects will actually fetch the data
    from the xml files.
 */
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

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.EntityRelations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.EntityRelations.Count == 0);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object - null for User with Id 4.
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
            Assert.IsTrue(user.EntityRelations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.EntityRelations.Count == 0);

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object - null for User with Id 4.
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
        }

        [Test]
        public void Fetch_Join()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(1);

            // User object
            Assert.AreEqual(false, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(user.ExecutedQuery, expectedUserQuery);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.EntityRelations.Count == 0);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Fetch_Join_Dirty()
        {
            var expectedUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(1);
            user.Organisation.Name = "Unit Test";

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue);
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.AreNotEqual(user.Organisation.Name, user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.EntityRelations.Count == 0);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Fetch_Join_New()
        {
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM3);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var expectedOrganisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES(@PARAM1); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var expectedOriginalOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(1)
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
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.EntityRelations.Count == 0);

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
            Assert.IsTrue(user.EntityRelations.Count == 0);

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
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation);
            Assert.IsNotNull(user.Organisation.OriginalFetchedValue);
            Assert.AreEqual(user.Organisation, user.Organisation.OriginalFetchedValue);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);

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
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);
            Assert.IsNotNull(user.Organisation);
            Assert.IsNull(user.Organisation.OriginalFetchedValue);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Update()
        {
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[PASSWORD] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM3);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";
            var expectedOriginalOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

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
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.EntityRelations.Count == 0);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalOrganisationQuery, user.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
        }

        [Test]
        public void Update_Join()
        {
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM3);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";
            var expectedOriginalOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

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
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(false, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.EntityRelations.Count == 0);
            Assert.AreNotEqual(user.Organisation, user.OriginalFetchedValue.ValueAs<User>().Organisation);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
            Assert.AreEqual(expectedOriginalOrganisationQuery, user.OriginalFetchedValue.ValueAs<User>().Organisation.ExecutedQuery);
        }

        [Test]
        public void Update_JoinInsert()
        {
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM3);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var expectedOrganisationQuery = "INSERT INTO [DBO].[ORGANISATIONS] ([DBO].[ORGANISATIONS].[NAME]) VALUES(@PARAM1); SELECT CAST(SCOPE_IDENTITY() AS INT);";

            // The original Organisation object for User 2.
            var expectedOriginalUserOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(2)
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
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.EntityRelations.Count == 0);
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
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM3);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var expectedOrganisationQuery = "UPDATE [O] SET [O].[NAME] = @PARAM1 FROM [dbo].[Organisations] AS [O] WHERE ([O].[Id] = @PARAM2);";

            // The original Organisation object for User 2.
            var expectedOriginalUserOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";
            
            // The original Organisation object for Orgnisation 1.
            var expectedOriginalNewOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(2)
            {
                Organisation = new Organisation(1)
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
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);
            Assert.AreEqual(expectedOriginalUserQuery, user.OriginalFetchedValue.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsNotNull(user.OriginalFetchedValue.ValueAs<User>().Organisation);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsDirty);
            Assert.AreEqual(false, user.OriginalFetchedValue.ValueAs<User>().Organisation.IsNew);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.EntityRelations.Count == 0);
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
            var expectedUserQuery = "UPDATE [U] SET [U].[USERNAME] = @PARAM1, [U].[PASSWORD] = @PARAM2, [U].[ORGANISATION] = @PARAM3, [U].[DATECREATED] = @PARAM4, [U].[DATELASTMODIFIED] = @PARAM5 FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM6);";
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
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(true, user.Organisation.IsNew);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);
            Assert.IsNull(user.Organisation.ValueAs<Organisation>().OriginalFetchedValue);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }

        [Test]
        public void Update_DirtyJoin_DisableChangeTracking()
        {
            var expectedInitialUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";
            var expectedUserQuery = "UPDATE [U] SET [U].[ORGANISATION] = @PARAM1, [U].[DATELASTMODIFIED] = @PARAM2 FROM [dbo].[Users] AS [U] WHERE ([U].[Id] = @PARAM3);";
            var expectedOriginalUserQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);";

            var expectedOrganisationQuery = "UPDATE [O] SET [O].[NAME] = @PARAM1 FROM [dbo].[Organisations] AS [O] WHERE ([O].[Id] = @PARAM2);";
            var expectedOriginalOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(2)
            {
                Organisation = new Organisation(1, true)
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
            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.IsTrue(user.OriginalFetchedValue.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());
            Assert.NotNull(user.OriginalFetchedValue.EntityRelations.OfType<Organisation>().FirstOrDefault());

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
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);
            Assert.IsTrue(user.OriginalFetchedValue.ValueAs<User>().Organisation.EntityRelations.Count == 0);
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
            var expectedOrganisationQuery = "SELECT TOP (1) * FROM [DBO].[ORGANISATIONS] AS [O] WHERE ([O].[ID] = @PARAM1);";

            var user = new User(2, true);

            // User object
            Assert.AreEqual(true, user.IsDirty);
            Assert.AreEqual(false, user.IsNew);

            Assert.IsNull(user.OriginalFetchedValue);
            Assert.IsNull(user.Organisation.OriginalFetchedValue);

            Assert.IsTrue(user.EntityRelations.Count == 1);
            Assert.NotNull(user.EntityRelations.OfType<Organisation>().FirstOrDefault());

            // User query
            Assert.AreEqual(expectedUserQuery, user.ExecutedQuery);

            // Organisation object
            Assert.AreEqual(true, user.Organisation.IsDirty);
            Assert.AreEqual(false, user.Organisation.IsNew);
            Assert.IsTrue(user.Organisation.EntityRelations.Count == 0);

            // Organisation query
            Assert.AreEqual(expectedOrganisationQuery, user.Organisation.ExecutedQuery);
        }
    }
}