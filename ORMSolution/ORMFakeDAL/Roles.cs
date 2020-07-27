using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    [ORMTable(typeof(Roles), typeof(Role))]
    public class Roles : ORMCollection<Role>
    {
        public Roles() { }
    }
}
