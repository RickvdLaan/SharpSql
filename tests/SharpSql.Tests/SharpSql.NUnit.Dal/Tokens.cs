using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    [SharpSqlTable(typeof(Tokens), typeof(Token))]
    public class Tokens : SharpSqlCollection<User>
    {
        public Tokens() { }
    }
}
