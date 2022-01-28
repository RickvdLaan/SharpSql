using SharpSql.Attributes;

namespace SharpSql.NUnit;

[SharpSqlTable(typeof(Roles), typeof(Role))]
public class Roles : SharpSqlCollection<Role>
{
    public Roles() { }
}