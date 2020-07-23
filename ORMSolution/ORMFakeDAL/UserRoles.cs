using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    [ORMTable(typeof(UserRoles), typeof(UserRole), typeof(Users), typeof(Roles))]
    public class UserRoles : ORMCollection<UserRole>
    {
        public UserRoles() { }
    }
}
