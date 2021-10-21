using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    public class UserRole : SharpSqlEntity
    {
        [SharpSqlPrimaryKey, SharpSqlForeignKey(typeof(User)), SharpSqlColumn("UserId")]
        public int Column_UserId { get; private set; }

        [SharpSqlPrimaryKey, SharpSqlForeignKey(typeof(Role)), SharpSqlColumn("RoleId")]
        public int Column_RoleId { get; private set; }

        internal UserRole() { }

        public UserRole(int userId, int roleId)
        {
            base.FetchEntityByPrimaryKey(userId, roleId);
        }
    }
}
