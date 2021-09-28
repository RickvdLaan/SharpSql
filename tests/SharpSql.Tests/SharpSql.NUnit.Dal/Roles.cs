using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    [ORMTable(typeof(Roles), typeof(Role))]
    public class Roles : ORMCollection<Role>
    {
        public Roles() { }
    }
}
