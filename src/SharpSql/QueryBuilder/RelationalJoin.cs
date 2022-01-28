using SharpSql.Attributes;
using System.Reflection;

namespace SharpSql;

internal struct RelationalJoin
{
    internal bool IsManyToMany { get; set; }

    internal SharpSqlTableAttribute LeftTableAttribute { get; set; }
    internal PropertyInfo LeftPropertyInfo { get; set; }

    internal SharpSqlTableAttribute RightTableAttribute { get; set; }
    internal PropertyInfo[] RightPropertyInfo { get; set; }
}