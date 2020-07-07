﻿using NUnit.Framework;
using ORM;
using ORM.Attributes;
using ORMFakeDAL;

namespace ORMNUnit.SQL
{
    [ORMUnitTest]
    public class ORMCollectionTests
    {
        [Test]
        public void BasicFetch()
        {
            var users = new Users();
            users.Fetch();
            Assert.AreEqual("SELECT * FROM [DBO].[USERS];", users.ExecutedQuery);
        }

        [Test]
        public void BasicFetch_Top()
        {
            var users = new Users();
            users.Fetch(1);
            Assert.AreEqual("SELECT TOP (1) * FROM [DBO].[USERS];", users.ExecutedQuery);
        }

        [Test]
        public void BasicWhere_And()
        {
            var users = new Users();
            users.Where(x => x.Id == 19 && x.Id == 12);
            users.Fetch();
            Assert.AreEqual("SELECT * FROM [DBO].[USERS] WHERE (([ID] = @PARAM1) AND ([ID] = @PARAM2));", users.ExecutedQuery);
        }

        [Test]
        public void ComplexWhere_StartsWith_Contains()
        {
            var users = new Users();
            users.Where(x => x.Id.ToString().StartsWith("1") || x.Password.Contains("qwerty") || x.Password.StartsWith("welkom"));
            users.Fetch();
            Assert.AreEqual("SELECT * FROM [DBO].[USERS] WHERE ((([ID] LIKE @PARAM1 + '%') OR ([PASSWORD] LIKE '%' + @PARAM2 + '%')) OR ([PASSWORD] LIKE @PARAM3 + '%'));", users.ExecutedQuery);
        }

        [Test]
        public void BasicSelect()
        {
            var users = new Users();
            users.Select(User.Fields.Username, User.Fields.Password);
            users.Fetch();
            Assert.AreEqual("SELECT [USERNAME], [PASSWORD] FROM [DBO].[USERS];", users.ExecutedQuery);
        }

        [Test]
        public void BasicOrderBy()
        {
            var users = new Users();
            users.OrderBy(User.Fields.Username.Descending(), User.Fields.Password.Ascending());
            users.Fetch();
            Assert.AreEqual("SELECT * FROM [DBO].[USERS] ORDER BY USERNAME DESC, PASSWORD ASC;", users.ExecutedQuery);
        }

        [Test]
        public void DirectQuerySimple()
        {
            var collection = ORMUtilities.ExecuteDirectQuery<Users, User>("SELECT TOP 10 * FROM USERS;");
            Assert.AreEqual("SELECT TOP 10 * FROM USERS;", collection.ExecutedQuery);
        }

        [Test]
        public void DirectQueryComplex()
        {
            var collection = ORMUtilities.ExecuteDirectQuery<Users, User>("SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;", 1, 2);
            Assert.AreEqual("SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;", collection.ExecutedQuery);
        }

    }
}