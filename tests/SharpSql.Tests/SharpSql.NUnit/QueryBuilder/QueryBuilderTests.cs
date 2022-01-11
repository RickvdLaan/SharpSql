using NUnit.Framework;
using SharpSql.UnitTests;
using System.Linq;

/*
    Important note: You're only allowed to create memory collection tables based on existing data within
    the memory entity tables. Creating new data within memory collection tables can cause unexpected
    behaviour, resulting in unrealistic datasets and/or results.

    The memory collection tables simulate what a SQL Server would have returned if the SQL Server
    contained the memory entity tables.
 */
namespace SharpSql.NUnit.QueryBuilder
{
    [TestFixture]
    public class QueryBuilderTests
    {
        private readonly UserData _userData = new();

        [Test, SharpSqlUnitTest("FetchCompareOutsideScope")]
        public void Fetch_ObjectOutsideScope_Variable_Compare()
        {
            // This tests the QueryBuilder.ReconstructConstantExpressionFromMemberExpression when
            // a scoped variable is used in its comparison. If the generated query is fine, the
            // process works as expected.

            var users = (new Users()
                .Where(x => x.Username == _userData._username)
                .Fetch(1) as Users);

            var user = users.FirstOrDefault();

            Assert.AreEqual(user.ExecutedQuery, "INITIALISED THROUGH COLLECTION");
            Assert.AreEqual(users.ExecutedQuery, "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[USERNAME] = @PARAM1);");
        }

        [Test, SharpSqlUnitTest("FetchCompareOutsideScope")]
        public void Fetch_ObjectOutsideScope_Property_Compare()
        {
            // This tests the QueryBuilder.ReconstructConstantExpressionFromMemberExpression when
            // a scoped property is used in its comparison. If the generated query is fine, the
            // process works as expected.

            var users = (new Users()
                .Where(x => x.Username == _userData.Username)
                .Fetch(1) as Users);

            var user = users.FirstOrDefault();

            Assert.AreEqual(user.ExecutedQuery, "INITIALISED THROUGH COLLECTION");
            Assert.AreEqual(users.ExecutedQuery, "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] WHERE ([U].[USERNAME] = @PARAM1);");
        }

        [Test, SharpSqlUnitTest("BasicFetchTopUsers")]
        public void NewExpression_Multiple_Single()
        {
            // This tests the QueryBuilder.ParseExpression.NewExpression keyword.If the generated
            // query is fine, the process works as expected.

            var users = (new Users()
                .Select(x => new { x.Username })
                .Fetch(1) as Users);

            var user = users.FirstOrDefault();

            Assert.AreEqual(user.ExecutedQuery, "INITIALISED THROUGH COLLECTION");
            Assert.AreEqual(users.ExecutedQuery, "SELECT TOP (1) [U].[USERNAME] FROM [DBO].[USERS] AS [U];");
        }

        [Test, SharpSqlUnitTest("BasicJoinLeft")]
        public void NewExpression_Single_Join()
        {
            // This tests the QueryBuilder.ParseExpression.NewExpression keyword. If the generated
            // query is fine, the process works as expected.

            var users = (new Users()
                .Join(x => new { x.Organisation })
                .Fetch(1) as Users);

            var user = users.FirstOrDefault();

            Assert.AreEqual(user.ExecutedQuery, "INITIALISED THROUGH COLLECTION");
            Assert.AreEqual(users.ExecutedQuery, "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID];");
        }

        [Test, SharpSqlUnitTest("BasicFetchTopUsers")]
        public void NewExpression_Multiple_Select()
        {
            // This tests the QueryBuilder.ParseExpression.NewExpression keyword.If the generated
            // query is fine, the process works as expected.

            var expectedQuery = "SELECT TOP (1) [U].[USERNAME], [U].[PASSWORD] FROM [DBO].[USERS] AS [U];";

            var users = (new Users()
                .Select(x => new { x.Username, x.Password })
                .Fetch(1) as Users);

            var user = users.FirstOrDefault();

            Assert.AreEqual(user.ExecutedQuery, "INITIALISED THROUGH COLLECTION");
            Assert.AreEqual(users.ExecutedQuery, expectedQuery);
        }

        [Test, SharpSqlUnitTest("")]
        public void NewExpression_Join_And_ManyToMany()
        {
            // This tests the QueryBuilder.ParseExpression.NewExpression keyword.If the generated
            // query is fine, the process works as expected.

            var expectedQuery = "SELECT TOP (1) * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[Id] LEFT JOIN [DBO].[USERROLES] AS [UU] ON [U].[ID] = [UU].[USERID] LEFT JOIN [DBO].[ROLES] AS [R] ON [UU].[ROLEID] = [R].[ID];";
            // SELECT TOP (1) * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATIONS] = [O].[Id] LEFT JOIN [DBO].[USERROLES] AS [UU] ON [U].[ID] = [UU].[USERID] LEFT JOIN [DBO].[ROLES] AS [R] ON [UU].[ROLEID] = [R].[ID];}
            // Deze ook in SharpSqlCollectionTests Join+M2M moet getest worden, werkt dit?

            // Query:
            // SELECT* FROM[DBO].[USERS] AS[U]
            // LEFT JOIN[DBO].[ORGANISATIONS] AS[O] ON[O].[ID] = [U].[ID]
            // LEFT JOIN[DBO].[USERROLES] AS[UU] ON[U].[ID] = [UU].[USERID]
            // LEFT JOIN[DBO].[ROLES] AS[R] ON[UU].[ROLEID] = [R].[ID] WHERE([U].[ID] = 1);
            // // Dataset:
            // Id Username    Password Organisation    DateCreated DateLastModified    Id Name    UserId RoleId  Id Name
            // 1   Imaani qwerty  140111  2020 - 07 - 23 16:50:38.213 2020 - 12 - 18 00:11:31.230 1   Nieuwe naam 1   1   1   Admin
            // 1   Imaani qwerty  140111  2020 - 07 - 23 16:50:38.213 2020 - 12 - 18 00:11:31.230 1   Nieuwe naam 1   2   2   Moderator

            // Moet ook werken zonder .Left en ook met .Right
            var user = (new Users()
                .Join(x => new object[]{ x.Organisation, x.Roles2 })
                .Fetch(1) as Users)
                .FirstOrDefault();

            // Voor deze unit test moet dit afgevangen worden:
            //var user = (new Users()
            //    .Join(x => new { x.Organisation, x.Roles })
            //    .Fetch(1) as Users)
            //    .FirstOrDefault();

            // Het zou wel clean zijn qua code als we gewoon new {} kunnen doen ipv new object[] {}
            // En wat gebeurt er als we x.Organisation in .Username() gebruiken? En wat als x.Username nu
            // in .Join() gebruikt wordt?

            // Ook ManyToMany bij meerdere records in collection fetch testen en supported.

            // Genoeg werk te doen...

            // UC unit test toevoegen.

            Assert.AreEqual(user.ExecutedQuery, expectedQuery);
        }


        [Test]
        public void ToQuery()
        {
            // Tests the ToQuery functionality.
            var expectedQuery = "SELECT [U].[Username], [U].[Organisation] FROM [DBO].[Users] AS [U] LEFT JOIN [DBO].[Organisations] AS [O] ON [U].[Organisation] = [O].[Id] WHERE ([U].[Username] LIKE @PARAM1 + '%') ORDER BY [U].[Username] ASC;";

            var queryBuilder = new Users()
                .Select(x => new { x.Username, x.Organisation })
                .Join(x => new { x.Organisation })
                .Where(x => x.Username.StartsWith('T'))
                .OrderBy(x => x.Username.Ascending())
                .ToQuery();

            Assert.AreEqual(expectedQuery, queryBuilder.GeneratedQuery);
            Assert.AreEqual(expectedQuery, queryBuilder.ToString());
        }
    }
}
