using ORM;
using ORM.Attributes;

namespace ORMConsole
{
    [ORMTable(typeof(Users), typeof(User))]
    public class Users : ORMCollection<User>
    {
        public Users() { }
    }
}
