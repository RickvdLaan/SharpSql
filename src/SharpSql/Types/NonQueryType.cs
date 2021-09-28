using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SharpSql.Benchmarks")]

namespace SharpSql
{
    internal enum NonQueryType
    {
        Insert,
        Update,
        Delete
    }
}
