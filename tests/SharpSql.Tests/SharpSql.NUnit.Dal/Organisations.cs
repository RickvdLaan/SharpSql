using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    [SharpSqlTable(typeof(Organisations), typeof(Organisation))]
    public class Organisations : SharpSqlCollection<Organisation>
    {
        public Organisations() { }
    }
}
