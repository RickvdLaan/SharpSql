using ORM.Attributes;

namespace ORMFakeDAL
{
    [ORMTable(typeof(UserRole), typeof(Users), typeof(Roles))]
    public class UserRole
    {
    }
}
