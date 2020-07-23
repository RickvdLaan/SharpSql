using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    [ORMTable(typeof(Roles), typeof(RoleEntity))]
    public class Roles : ORMCollection<RoleEntity>
    {
        public Roles() { }
    }
}
