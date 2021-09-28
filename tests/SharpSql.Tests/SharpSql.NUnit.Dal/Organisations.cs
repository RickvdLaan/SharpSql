using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    [ORMTable(typeof(Organisations), typeof(Organisation))]
    public class Organisations : ORMCollection<Organisation>
    {
        public Organisations() { }
    }
}
