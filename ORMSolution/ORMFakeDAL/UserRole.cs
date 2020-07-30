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

        public UserRole(int userId, int roleId)
        {
            base.FetchEntityByPrimaryKey(userId, roleId);
        }
    }
}
