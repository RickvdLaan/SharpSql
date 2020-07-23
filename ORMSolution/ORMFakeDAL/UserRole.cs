using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    public class UserRole : ORMEntity
    {
        [ORMPrimaryKey]
        public int UserId { get; private set; }

        [ORMPrimaryKey]
        public int RoleId { get; private set; }

        public User User { get; private set; }

        public RoleEntity Role { get; private set; }

        public UserRole(int userId, int roleId)
        {
            base.FetchEntityByCombinedPrimaryKey<UserRoles, UserRole>(userId, roleId);

            User = new User(UserId);
            Role = new RoleEntity(RoleId);
        }
    }
}
