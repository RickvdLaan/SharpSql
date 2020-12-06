using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    public class UserRole : ORMEntity
    {
        [ORMPrimaryKey, ORMForeignKey(typeof(User)), ORMColumn("UserId")]
        public int Column_UserId { get; private set; }

        [ORMPrimaryKey, ORMForeignKey(typeof(Role)), ORMColumn("RoleId")]
        public int Column_RoleId { get; private set; }

        public User User { get; private set; }

        public Role Role { get; private set; }

        public UserRole(int userId, int roleId, bool isLazyLoading = false)
        {
            base.FetchEntityByPrimaryKey(userId, roleId);

            if (!isLazyLoading)
            {
                // Lazy loading isn't supported yet, but is on the todo list.
                User = new User(userId);
                Role = new Role(roleId);
            }
        }
    }
}
