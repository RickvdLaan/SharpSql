using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    public class UserRole : ORMEntity
    {
        [ORMPrimaryKey, ORMForeignKey(typeof(User)), ORMColumn("UserId")]
        public int Column_UserId { get; private set; }

        [ORMPrimaryKey, ORMForeignKey(typeof(Role)), ORMColumn("RoleId")]
        public int Column_RoleId { get; private set; }

        private UserRole() { }

        public UserRole(int userId, int roleId)
        {
            base.FetchEntityByPrimaryKey(userId, roleId);
        }
    }
}
