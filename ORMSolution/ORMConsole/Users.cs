using ORM;
using ORM.Attributes;

namespace ORMConsole
{
    [ORMTable(nameof(Users), typeof(UserEntity))]
    public class Users : ORMCollection
    {
    }
}
