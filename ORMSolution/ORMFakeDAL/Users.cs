using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    [ORMTable(typeof(Users), typeof(User))]
    public class Users : ORMCollection<User>
    {
        public Users() { }
    }
}
