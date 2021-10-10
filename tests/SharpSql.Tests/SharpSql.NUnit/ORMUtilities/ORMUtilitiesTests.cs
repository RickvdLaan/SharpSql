using NUnit.Framework;
using SharpSql.Attributes;
using System.Linq;

namespace SharpSql.NUnit
{
    [TestFixture]
    public partial class ORMUtilityTests
    {
        [Test, ORMUnitTest("BasicSelectUsers")]
        [TestCaseSource(nameof(ConvertTo_Data))]
        public void ConvertTo(string username, string password, bool disableChangeTracking, int index)
        {
            var dataTable = ORMUtilities.MemoryCollectionDatabase.Fetch(UnitTestUtilities.GetMemoryTableName());

            var users = ORMUtilities.ConvertTo<Users, User>(dataTable, disableChangeTracking);

            Assert.AreEqual(5, users.Count);

            var user = users[index];

            Assert.AreEqual(-1, user.Id);
            Assert.AreEqual(username, user.Username);
            Assert.AreEqual(password, user.Password);
            Assert.IsNull(user.Organisation);
            Assert.IsNull(user.DateCreated);
            Assert.IsNull(user.DateLastModified);

            Assert.IsTrue(user.Relations.Count == 0);
            Assert.IsTrue(users.All(x => x.ValueAs<User>().Organisation == null));
            Assert.IsTrue(users.All(x => x.DisableChangeTracking == disableChangeTracking));
            Assert.IsFalse(users.All(x => x.IsNew == true));
            Assert.IsFalse(users.All(x => x.IsAutoIncrement == false));
            Assert.IsFalse(users.All(x => x.IsMarkedAsDeleted == true));
            Assert.IsFalse(users.All(x => x.ObjectState == ObjectState.Fetched));

            if (disableChangeTracking)
            {
                Assert.IsTrue(users.All(x => x.IsDirty == true));
                Assert.IsNull(user.OriginalFetchedValue);
                Assert.IsTrue(users.All(x => x.OriginalFetchedValue == null));
            }
            else
            {
                Assert.IsFalse(users.All(x => x.IsDirty == true));
                Assert.IsTrue(user.OriginalFetchedValue.Relations.Count == 0);
                Assert.IsTrue(users.All(x => x.OriginalFetchedValue.ValueAs<User>().Organisation == null));
                Assert.AreEqual(user, user.OriginalFetchedValue);
                Assert.IsFalse(ReferenceEquals(user, user.OriginalFetchedValue));
            }
        }
    }
}
