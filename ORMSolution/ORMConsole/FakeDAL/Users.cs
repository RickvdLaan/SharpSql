using ORM;
using ORM.Attributes;

namespace ORMConsole
{
    [ORMTable(nameof(Users), typeof(User))]
    public class Users : ORMCollection<User>
    {
        public Users() { }
    }
}
