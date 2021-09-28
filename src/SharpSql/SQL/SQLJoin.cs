using SharpSql.Attributes;
using System.Reflection;

namespace SharpSql
{
    internal class SQLJoin
    {
        internal bool IsManyToMany { get; set; }

        internal ORMTableAttribute LeftTableAttribute { get; set; }
        internal PropertyInfo LeftPropertyInfo { get; set; }

        internal ORMTableAttribute RightTableAttribute { get; set; }
        internal PropertyInfo[] RightPropertyInfo { get; set; }
    }
}