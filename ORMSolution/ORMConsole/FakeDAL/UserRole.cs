using ORM.Attributes;

namespace ORMConsole
{
    [ORMTable(typeof(UserRole), typeof(Users), typeof(Roles))]
    public class UserRole
    {
    }
}
