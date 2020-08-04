using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    public class UserRole : ORMEntity
    {
        [ORMPrimaryKey, ORMForeignKey(typeof(User)), ORMColumn("UserId")]
        public int User { get; private set; }

        [ORMPrimaryKey, ORMForeignKey(typeof(Role))]
        public int RoleId { get; private set; }

        public UserRole(int userId, int roleId)
        {
            base.FetchEntityByPrimaryKey(userId, roleId);
        }
    }
}
