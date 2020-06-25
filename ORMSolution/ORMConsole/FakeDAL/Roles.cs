using ORM;
using ORM.Attributes;

namespace ORMConsole
{
    [ORMTable(typeof(Roles), typeof(Role))]
    public class Roles : ORMCollection<Role>
    {
        public Roles() { }
    }
}
